import { describe, expect, it } from 'vitest'
import { ref } from 'vue'

import { useFormFailure } from '@/composables/useFormFailure'
import type { ApiFailure } from '@/services/http/validation'

const FALLBACK = 'The event could not be saved.'

/** Said under the control that holds a value the API could not read. */
const UNREADABLE_VALUE = 'We could not read this value. Please check it and try again.'

/** Said once in the banner when no control on the form holds the unreadable value. */
const UNREADABLE_REQUEST =
  'We could not read some of what you sent. Please check your entries and try again.'

function failed(parts: Partial<ApiFailure>): ApiFailure {
  return { fieldErrors: {}, formMessages: [], unreadableFields: [], ...parts }
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

  it('puts an unreadable value under the control that holds it, matched however it is cased', () => {
    const failure = failed({ unreadableFields: ['startDateTime'] })

    const { fieldError, formError } = useFormFailure(failure, FALLBACK, [
      'Name',
      'StartDateTime',
    ])

    expect(fieldError('StartDateTime')).toBe(UNREADABLE_VALUE)
    expect(fieldError('Name')).toBeUndefined()
    expect(formError.value).toBe('')
  })

  it('says one generic sentence when the form holds no control for the unreadable value', () => {
    const failure = failed({ unreadableFields: ['idempotencyKey'] })

    const { formError } = useFormFailure(failure, FALLBACK, ['Name', 'StartDateTime'])

    expect(formError.value).toBe(UNREADABLE_REQUEST)
  })

  it('says that sentence once however many values could not be read', () => {
    const failure = failed({
      unreadableFields: ['idempotencyKey', 'attendeeCount'],
    })

    const { formError } = useFormFailure(failure, FALLBACK, ['Name'])

    expect(formError.value).toBe(UNREADABLE_REQUEST)
  })

  it('speaks under the control it can and in the banner for the value it cannot place', () => {
    const failure = failed({
      unreadableFields: ['startDateTime', 'idempotencyKey'],
    })

    const { fieldError, formError } = useFormFailure(failure, FALLBACK, [
      'StartDateTime',
    ])

    expect(fieldError('StartDateTime')).toBe(UNREADABLE_VALUE)
    expect(formError.value).toBe(UNREADABLE_REQUEST)
  })

  it('shows every message in the banner when the form declares no fields', () => {
    const failure = failed({
      fieldErrors: { Name: ['Name is required.'] },
      unreadableFields: ['startDateTime'],
    })

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe(`Name: Name is required. ${UNREADABLE_REQUEST}`)
  })

  it('says the whole-request messages alongside the ones no field claims', () => {
    const failure = failed({
      formMessages: ['The event has already started.'],
      unreadableFields: ['idempotencyKey'],
    })

    const { formError } = useFormFailure(failure, FALLBACK, ['StartDateTime'])

    expect(formError.value).toBe(
      `The event has already started. ${UNREADABLE_REQUEST}`,
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
})
