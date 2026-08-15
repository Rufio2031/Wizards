import { describe, expect, it } from 'vitest'

import { ApiError } from '@/services/http/ApiError'
import { toApiFailure } from '@/services/http/validation'

/** What the serializer says about a value it could not read: types, paths and byte offsets. */
const UNREADABLE_START =
  'The JSON value could not be converted to System.DateTimeOffset. Path: $.startDateTime | LineNumber: 0 | BytePositionInLine: 52.'

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
      unreadableFields: [],
    })
  })

  it('keeps every message a key carries, not just the first', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: {
          Name: [
            'The Name field is required.',
            'The field Name must be a string with a minimum length of 1 and a maximum length of 100.',
          ],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {
        Name: [
          'The Name field is required.',
          'The field Name must be a string with a minimum length of 1 and a maximum length of 100.',
        ],
      },
      formMessages: [],
      unreadableFields: [],
    })
  })

  it('names a value it could not read by its path, and keeps the serializer out of it', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: { '$.startDateTime': [UNREADABLE_START] },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {},
      formMessages: [],
      unreadableFields: ['startDateTime'],
    })
  })

  it('carries no internal type name or byte offset out of the parse boundary', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: {
          '$.idempotencyKey': [
            'The JSON value could not be converted to Wizards.Application.DTOs.Requests.CreateRegistrationRequest. Path: $.idempotencyKey | LineNumber: 0 | BytePositionInLine: 52.',
          ],
        },
      }),
    )

    expect(failure?.unreadableFields).toEqual(['idempotencyKey'])
    expect(JSON.stringify(failure)).not.toContain('Wizards.Application')
    expect(JSON.stringify(failure)).not.toContain('BytePositionInLine')
  })

  it('drops the binding artifacts beside an unreadable value, but keeps what the API said about the request', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: {
          '': ['Registration is closed.'],
          request: ['The request field is required.'],
          Name: ['The Name field is required.'],
          '$.startDateTime': [UNREADABLE_START],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {},
      formMessages: ['Registration is closed.'],
      unreadableFields: ['startDateTime'],
    })
  })

  it('names every unreadable value when more than one could not be read', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          '$.startDateTime': [UNREADABLE_START],
          '$.gameType.selections.rounds': [
            'The JSON value could not be converted to System.Int32. Path: $.gameType.selections.rounds | LineNumber: 0 | BytePositionInLine: 91.',
          ],
        },
      }),
    )

    expect(failure?.unreadableFields).toEqual(
      expect.arrayContaining(['startDateTime', 'gameType.selections.rounds']),
    )
    expect(failure?.unreadableFields).toHaveLength(2)
  })

  it('reports an unexplained failure as empty messages, keeping raw error text out', () => {
    const serverFault = toApiFailure(
      new ApiError(500, {
        title: 'Internal Server Error',
        detail: 'NullReferenceException at WizardService.cs:42',
      }),
    )

    expect(serverFault).toEqual({
      fieldErrors: {},
      formMessages: [],
      unreadableFields: [],
    })
    expect(toApiFailure(new Error('Something went wrong'))).toEqual({
      fieldErrors: {},
      formMessages: [],
      unreadableFields: [],
    })
  })

  it('reports no failure when the last call succeeded', () => {
    expect(toApiFailure(null)).toBeNull()
  })

  // Known rough edge, not a desired outcome: a body missing entirely is reported
  // as a "request" field with no JSON path beside it, so nothing marks it as a
  // binding artifact and it reaches the banner named after the action parameter.
  // Unreachable from either view, both of which always send a JSON object.
  it('known rough edge: a body missing entirely is still blamed on a "request" field', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: { request: ['The request field is required.'] },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: { request: ['The request field is required.'] },
      formMessages: [],
      unreadableFields: [],
    })
  })
})
