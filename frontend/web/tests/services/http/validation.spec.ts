import { describe, expect, it } from 'vitest'

import { ApiError } from '@/services/http/ApiError'
import { toApiFailure } from '@/services/http/validation'

/** What the serializer says about a value it could not read: types, paths and byte offsets. */
const UNREADABLE_START =
  'The JSON value could not be converted to System.DateTimeOffset. Path: $.startDateTime | LineNumber: 0 | BytePositionInLine: 52.'

/** Said under the field that holds a value the API could not read. */
const UNREADABLE_VALUE = 'We could not read this value. Please check it and try again.'

/** Said about the request as a whole when the body could not be read at all. */
const UNREADABLE_REQUEST =
  'We could not read some of what you sent. Please check your entries and try again.'

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
        name: [
          'The Name field is required.',
          'The field Name must be a string with a minimum length of 1 and a maximum length of 100.',
        ],
      },
      formMessages: [],
    })
  })

  it('names a field the way the app reads it, whatever casing the API used', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: { StartDateTime: ['Start must be in the future.'] },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: { startDateTime: ['Start must be in the future.'] },
      formMessages: [],
    })
  })

  it('names every DTO segment of a nested field the way the app reads it', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          'GameType.GameTypeId': ['Choose a game type.'],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {
        'gameType.gameTypeId': ['Choose a game type.'],
      },
      formMessages: [],
    })
  })

  it('keeps a setting key past the selections path exactly as the API wrote it', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          'GameType.Selections.Rounds': ['Rounds must be at most 5.'],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {
        'gameType.selections.Rounds': ['Rounds must be at most 5.'],
      },
      formMessages: [],
    })
  })

  it('leaves a field the API already named the way the app reads it alone', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          registrationLimit: ['Must be between 1 and 30.'],
          'gameType.gameTypeId': ['Choose a game type.'],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {
        registrationLimit: ['Must be between 1 and 30.'],
        'gameType.gameTypeId': ['Choose a game type.'],
      },
      formMessages: [],
    })
  })

  it('keeps both messages when the API named one field two ways', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          Name: ['The Name field is required.'],
          name: ['That name is already taken.'],
        },
      }),
    )

    expect(Object.keys(failure?.fieldErrors ?? {})).toEqual(['name'])
    expect(failure?.fieldErrors.name).toEqual(
      expect.arrayContaining([
        'The Name field is required.',
        'That name is already taken.',
      ]),
    )
    expect(failure?.fieldErrors.name).toHaveLength(2)
  })

  it('blames a value it could not read on the field that holds it, and keeps the serializer out of it', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: { '$.startDateTime': [UNREADABLE_START] },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: { startDateTime: [UNREADABLE_VALUE] },
      formMessages: [],
    })
  })

  it('blames a nested value it could not read on the field the app reads it under', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        errors: {
          '$.GameType.GameTypeId': [
            'The JSON value could not be converted to System.Guid. Path: $.GameType.GameTypeId | LineNumber: 0 | BytePositionInLine: 33.',
          ],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: { 'gameType.gameTypeId': [UNREADABLE_VALUE] },
      formMessages: [],
    })
  })

  it('blames the request as a whole when the body could not be read at all', () => {
    const failure = toApiFailure(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: {
          $: [
            'The JSON value could not be converted to Wizards.Application.DTOs.Requests.CreateEventRequest. Path: $ | LineNumber: 0 | BytePositionInLine: 0.',
          ],
        },
      }),
    )

    expect(failure).toEqual({
      fieldErrors: {},
      formMessages: [UNREADABLE_REQUEST],
    })
    expect(JSON.stringify(failure)).not.toContain('Wizards.Application')
    expect(JSON.stringify(failure)).not.toContain('BytePositionInLine')
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

    expect(failure?.fieldErrors).toEqual({ idempotencyKey: [UNREADABLE_VALUE] })
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
      fieldErrors: { startDateTime: [UNREADABLE_VALUE] },
      formMessages: ['Registration is closed.'],
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

    expect(failure).toEqual({
      fieldErrors: {
        startDateTime: [UNREADABLE_VALUE],
        'gameType.selections.rounds': [UNREADABLE_VALUE],
      },
      formMessages: [],
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
    })
  })
})
