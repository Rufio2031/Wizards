import { describe, expect, it } from 'vitest'

import { ApiError } from '@/services/http/ApiError'
import { toApiFailure } from '@/services/http/validation'

describe('toApiFailure', () => {
  it('splits a failure that blames both a field and the whole request', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          '': ['Registration is closed.', 'The event is already full.'],
          email: ['Email is not valid.'],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: { email: ['Email is not valid.'] },
      formMessages: ['Registration is closed.', 'The event is already full.'],
    })
  })

  it('reports an unexplained failure as empty messages, keeping raw error text out', () => {
    const serverFault = toApiFailure(
      new ApiError(500, {
        title: 'Internal Server Error',
        detail: 'NullReferenceException at WizardService.cs:42',
      }),
    )

    expect(serverFault).toEqual({ fieldErrors: {}, formMessages: [] })
    expect(toApiFailure(new Error('Something went wrong'))).toEqual({
      fieldErrors: {},
      formMessages: [],
    })
  })

  it('reports no failure when the last call succeeded', () => {
    expect(toApiFailure(null)).toBeNull()
  })
})
