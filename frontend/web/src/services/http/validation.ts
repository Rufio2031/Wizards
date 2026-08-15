import { isApiError } from './ApiError'

/**
 * A failed call's messages, split by whether the API blamed a particular field.
 *
 * Only messages written for a person are carried: `ApiError.message` never
 * appears, and the serializer's own words are replaced with copy a user can act
 * on, so a caller can render everything here without putting the API's internals
 * in front of a user.
 */
export interface ApiFailure {
  /**
   * Messages keyed by the field each is about, in camelCase whatever the API used,
   * except for a dictionary key the API chose, which is left as it was written.
   */
  fieldErrors: Record<string, string[]>

  /**
   * Messages about the request as a whole. Empty when every message named a
   * field, or when the failure carried no messages at all, in which case the
   * caller supplies its own copy.
   */
  formMessages: string[]
}

/** Stands in for the serializer's message, which describes types and byte offsets, never the value typed. */
const UNREADABLE_VALUE = 'We could not read this value. Please check it and try again.'

/** Said instead when the body as a whole could not be read, so no one field is at fault. */
const UNREADABLE_REQUEST =
  'We could not read some of what you sent. Please check your entries and try again.'

/** The API names a value it could not parse by its JSON path, as `$.startDateTime`. */
const JSON_PATH_ROOT = '$'

function isJsonPath(key: string): boolean {
  return key === JSON_PATH_ROOT || key.startsWith(`${JSON_PATH_ROOT}.`)
}

function toBodyPath(key: string): string {
  return key.slice(JSON_PATH_ROOT.length + 1)
}

/**
 * Paths the API fills with data rather than with more properties. What follows one
 * of these is a dictionary key someone authored, which the API matches without
 * regard to case and hands back as it was written.
 */
const DATA_PATH_PREFIXES = ['gameType.selections.']

function dataPathPrefixOf(key: string): string | undefined {
  const lowered = key.toLowerCase()

  return DATA_PATH_PREFIXES.find((prefix) => lowered.startsWith(prefix.toLowerCase()))
}

/**
 * Lowercases the first letter of every dotted segment, so a field named as the DTO
 * declares it and the same field named by its JSON path land on one key.
 *
 * Stops at a path the API fills with data, leaving the key past it exactly as the
 * API said it. Recasing a setting key would move its message off the control that
 * renders that setting and into the banner, where it reads as being about a field
 * the form never named.
 */
function toFieldKey(key: string): string {
  const dataPath = dataPathPrefixOf(key)

  if (dataPath) {
    return dataPath + key.slice(dataPath.length)
  }

  return key
    .split('.')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.')
}

function blame(fieldErrors: Record<string, string[]>, key: string, messages: string[]): void {
  const field = toFieldKey(key)

  fieldErrors[field] = [...(fieldErrors[field] ?? []), ...messages]
}

/**
 * Splits a caught failure into its per-field and whole-request messages.
 *
 * @param error The failure to read, or `null` when the last call succeeded.
 * @returns The split messages, or `null` when there was no failure. A failure
 * that carried no validation messages returns empty collections rather than
 * `null`, so a caller can tell "failed but unexplained" from "did not fail".
 */
export function toApiFailure(error: Error | null): ApiFailure | null {
  if (!error) {
    return null
  }

  const fieldErrors: Record<string, string[]> = {}
  const formMessages: string[] = []

  if (isApiError(error)) {
    const entries = Object.entries(error.errors)

    // A body that failed to deserialize never reached validation, so once any key
    // names a JSON path, the keys beside it are the binding artifacts that came
    // with it, named after the action's own parameter rather than after anything
    // the user typed.
    const failedToDeserialize = entries.some(([key]) => isJsonPath(key))

    for (const [key, messages] of entries) {
      if (key === '') {
        formMessages.push(...messages)

        continue
      }

      if (isJsonPath(key)) {
        const path = toBodyPath(key)

        // `$` alone names the whole body, leaving no field to blame it on.
        if (path === '') {
          formMessages.push(UNREADABLE_REQUEST)
        } else {
          blame(fieldErrors, path, [UNREADABLE_VALUE])
        }

        continue
      }

      if (!failedToDeserialize) {
        blame(fieldErrors, key, messages)
      }
    }
  }

  return { fieldErrors, formMessages }
}
