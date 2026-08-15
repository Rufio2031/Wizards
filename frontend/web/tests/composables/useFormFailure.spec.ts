import { describe, expect, it } from 'vitest'
import { ref } from 'vue'

import { useFormFailure } from '@/composables/useFormFailure'
import type { ApiFailure } from '@/services/http/validation'

const FALLBACK = 'The event could not be saved.'

/** Said under the control that holds a value the API could not read. */
const UNREADABLE_VALUE = 'We could not read this value. Please check it and try again.'

function failed(parts: Partial<ApiFailure>): ApiFailure {
  return { fieldErrors: {}, formMessages: [], ...parts }
}

describe('useFormFailure', () => {
  it('gives a field every message the API blamed on it, not only the first', () => {
    const failure = failed({
      fieldErrors: {
        Name: [
          'The Name field is required.',
          'The field Name must be a string with a minimum length of 1 and a maximum length of 100.',
        ],
      },
    })

    const { fieldError, formError } = useFormFailure(failure, FALLBACK, ['Name'])

    expect(fieldError('Name')).toBe(
      'The Name field is required. The field Name must be a string with a minimum length of 1 and a maximum length of 100.',
    )
    expect(formError.value).toBe('')
  })

  it('gives nothing for a field the API did not blame', () => {
    const failure = failed({ fieldErrors: { name: ['Name is required.'] } })

    const { fieldError } = useFormFailure(failure, FALLBACK, ['name', 'capacity'])

    expect(fieldError('name')).toBe('Name is required.')
    expect(fieldError('capacity')).toBeUndefined()
  })

  it('joins the messages about the request as a whole into the banner', () => {
    const failure = failed({
      formMessages: ['The event has already started.', 'Try a later date.'],
    })

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe('The event has already started. Try a later date.')
  })

  it('shows no banner when every message already belongs under a field', () => {
    const failure = failed({ fieldErrors: { name: ['Name is required.'] } })

    const { formError } = useFormFailure(failure, FALLBACK, ['name'])

    expect(formError.value).toBe('')
  })

  it('names the property a banner line is about', () => {
    const failure = failed({
      fieldErrors: { 'gameType.selections.rounds': ['Rounds must be at most 5.'] },
    })

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe('Rounds: Rounds must be at most 5.')
  })

  it('reads a run-together property name as words', () => {
    const failure = failed({
      fieldErrors: { registrationLimit: ['Must be between 1 and 30.'] },
    })

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe('Registration Limit: Must be between 1 and 30.')
  })

  it('stops speaking for a field once the form renders a control for it', () => {
    const failure = failed({
      fieldErrors: { 'gameType.selections.rounds': ['Rounds must be at most 5.'] },
    })
    const fields = ref<string[]>([])

    const { formError } = useFormFailure(failure, FALLBACK, fields)

    expect(formError.value).toBe('Rounds: Rounds must be at most 5.')

    fields.value = ['gameType.selections.rounds']

    expect(formError.value).toBe('')
  })

  it('puts an unreadable value under the control that holds it', () => {
    const failure = failed({ fieldErrors: { startDateTime: [UNREADABLE_VALUE] } })

    const { fieldError, formError } = useFormFailure(failure, FALLBACK, [
      'name',
      'startDateTime',
    ])

    expect(fieldError('startDateTime')).toBe(UNREADABLE_VALUE)
    expect(fieldError('name')).toBeUndefined()
    expect(formError.value).toBe('')
  })

  it('names the value in the banner when the form holds no control for it', () => {
    const failure = failed({ fieldErrors: { idempotencyKey: [UNREADABLE_VALUE] } })

    const { formError } = useFormFailure(failure, FALLBACK, ['name', 'startDateTime'])

    expect(formError.value).toBe(`Idempotency Key: ${UNREADABLE_VALUE}`)
  })

  it('names each unreadable value the form holds no control for', () => {
    const failure = failed({
      fieldErrors: {
        idempotencyKey: [UNREADABLE_VALUE],
        attendeeCount: [UNREADABLE_VALUE],
      },
    })

    const { formError } = useFormFailure(failure, FALLBACK, ['name'])

    expect(formError.value).toBe(
      `Idempotency Key: ${UNREADABLE_VALUE} Attendee Count: ${UNREADABLE_VALUE}`,
    )
  })

  it('speaks under the control it can and in the banner for the value it cannot place', () => {
    const failure = failed({
      fieldErrors: {
        startDateTime: [UNREADABLE_VALUE],
        idempotencyKey: [UNREADABLE_VALUE],
      },
    })

    const { fieldError, formError } = useFormFailure(failure, FALLBACK, [
      'startDateTime',
    ])

    expect(fieldError('startDateTime')).toBe(UNREADABLE_VALUE)
    expect(formError.value).toBe(`Idempotency Key: ${UNREADABLE_VALUE}`)
  })

  it('shows every message in the banner when the form declares no fields', () => {
    const failure = failed({
      fieldErrors: {
        name: ['Name is required.'],
        startDateTime: [UNREADABLE_VALUE],
      },
    })

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe(
      `Name: Name is required. Start Date Time: ${UNREADABLE_VALUE}`,
    )
  })

  it('says the whole-request messages alongside the ones no field claims', () => {
    const failure = failed({
      formMessages: ['The event has already started.'],
      fieldErrors: { idempotencyKey: [UNREADABLE_VALUE] },
    })

    const { formError } = useFormFailure(failure, FALLBACK, ['startDateTime'])

    expect(formError.value).toBe(
      `The event has already started. Idempotency Key: ${UNREADABLE_VALUE}`,
    )
  })

  it("falls back to the view's own copy when the failure explained nothing", () => {
    const { formError } = useFormFailure(failed({}), FALLBACK)

    expect(formError.value).toBe('The event could not be saved.')
  })

  it('reports no banner while the last call still stands', () => {
    const { formError } = useFormFailure(null, FALLBACK, ['Name'])

    expect(formError.value).toBe('')
  })

  it('stays silent once an edit retires a failure that only blamed fields', () => {
    const failure = failed({ fieldErrors: { name: ['Name is required.'] } })

    const { fieldError, formError, clearFieldErrors } = useFormFailure(failure, FALLBACK, [
      'name',
    ])

    clearFieldErrors()

    expect(fieldError('name')).toBeUndefined()
    expect(formError.value).toBe('')
  })

  it('stays silent once an edit retires a value the API could not read', () => {
    const failure = failed({
      fieldErrors: {
        startDateTime: [UNREADABLE_VALUE],
        idempotencyKey: [UNREADABLE_VALUE],
      },
    })

    const { fieldError, formError, clearFieldErrors } = useFormFailure(failure, FALLBACK, [
      'startDateTime',
    ])

    expect(formError.value).toBe(`Idempotency Key: ${UNREADABLE_VALUE}`)

    clearFieldErrors()

    expect(fieldError('startDateTime')).toBeUndefined()
    expect(formError.value).toBe('')
  })

  it('keeps saying what the request was refused for after an edit clears the fields', () => {
    const failure = failed({
      formMessages: ['This event is full.'],
      fieldErrors: { name: ['Name is required.'] },
    })

    const { fieldError, formError, clearFieldErrors } = useFormFailure(failure, FALLBACK, [
      'name',
    ])

    expect(fieldError('name')).toBe('Name is required.')

    clearFieldErrors()

    expect(fieldError('name')).toBeUndefined()
    expect(formError.value).toBe('This event is full.')
  })

  it('speaks for the next failure without being told a new attempt began', () => {
    const failure = ref<ApiFailure | null>(
      failed({ fieldErrors: { name: ['Name is required.'] } }),
    )

    const { fieldError, formError, clearFieldErrors } = useFormFailure(failure, FALLBACK, [
      'name',
    ])

    clearFieldErrors()

    expect(fieldError('name')).toBeUndefined()

    failure.value = failed({
      formMessages: ['This event is full.'],
      fieldErrors: { name: ['That name is already taken.'] },
    })

    expect(fieldError('name')).toBe('That name is already taken.')
    expect(formError.value).toBe('This event is full.')
  })

  it('speaks for a failure that arrives after an edit made while nothing had failed', () => {
    const failure = ref<ApiFailure | null>(null)

    const { fieldError, formError, clearFieldErrors } = useFormFailure(failure, FALLBACK, [
      'name',
    ])

    clearFieldErrors()

    failure.value = failed({
      formMessages: ['This event is full.'],
      fieldErrors: { name: ['Name is required.'] },
    })

    expect(fieldError('name')).toBe('Name is required.')
    expect(formError.value).toBe('This event is full.')
  })
})
