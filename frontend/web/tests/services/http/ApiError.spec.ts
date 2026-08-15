import { describe, expect, it } from 'vitest'

import { ApiError, isApiError } from '@/services/http/ApiError'

describe('ApiError', () => {
  it('carries the status, problem detail and validation messages it was given', () => {
    const error = new ApiError(422, {
      title: 'Unprocessable entity',
      detail: 'The event has already started.',
      errors: { name: ['Name is required.'] },
    })

    expect(error).toBeInstanceOf(Error)
    expect(error.status).toBe(422)
    expect(error.title).toBe('Unprocessable entity')
    expect(error.message).toBe('The event has already started.')
    expect(error.errors).toEqual({ name: ['Name is required.'] })
  })

  it('stands in for a problem that says nothing, rather than leaving fields unset', () => {
    const error = new ApiError(503)

    expect(error.message).toBe('Request failed with status 503.')
    expect(error.errors).toEqual({})
  })
})

describe('isApiError', () => {
  it('recognizes an ApiError and rejects an object that merely looks like one', () => {
    expect(isApiError(new ApiError(404))).toBe(true)
    expect(isApiError({ name: 'ApiError', status: 404, errors: {} })).toBe(false)
    expect(isApiError(null)).toBe(false)
  })
})
