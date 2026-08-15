import { describe, expect, it } from 'vitest'

import { useFormFailure } from '@/composables/useFormFailure'
import type { ApiFailure } from '@/services/http/validation'

const FALLBACK = 'The event could not be saved.'

describe('useFormFailure', () => {
  it('gives a field the first message the API blamed on it, and nothing for the rest', () => {
    const failure: ApiFailure = {
      fieldErrors: {
        name: ['Name is required.', 'Name must be shorter than 80 characters.'],
      },
      formMessages: [],
    }

    const { fieldError } = useFormFailure(failure, FALLBACK)

    expect(fieldError('name')).toBe('Name is required.')
    expect(fieldError('capacity')).toBeUndefined()
  })

  it('joins the messages about the request as a whole into the banner', () => {
    const failure: ApiFailure = {
      fieldErrors: {},
      formMessages: ['The event has already started.', 'Try a later date.'],
    }

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe(
      'The event has already started. Try a later date.',
    )
  })

  it('shows no banner when every message already belongs under a field', () => {
    const failure: ApiFailure = {
      fieldErrors: { name: ['Name is required.'] },
      formMessages: [],
    }

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe('')
  })

  it("falls back to the view's own copy when the failure explained nothing", () => {
    const failure: ApiFailure = { fieldErrors: {}, formMessages: [] }

    const { formError } = useFormFailure(failure, FALLBACK)

    expect(formError.value).toBe('The event could not be saved.')
  })
})
