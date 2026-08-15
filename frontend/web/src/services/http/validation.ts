import { isApiError } from './ApiError'

/**
 * A failed call's messages, split by whether the API blamed a particular field.
 *
 * Only messages the API wrote for a person are carried: `ApiError.message` never
 * appears, and the serializer's own words are reduced to the path they were
 * about, so a caller can render everything here without putting the API's
 * internals in front of a user.
 */
export interface ApiFailure {
  /** Messages keyed by the field each is about, as the API named it. */
  fieldErrors: Record<string, string[]>

  /**
   * Messages about the request as a whole. Empty when every message named a
   * field, or when the failure carried no messages at all, in which case the
   * caller supplies its own copy.
   */
  formMessages: string[]

  /**
   * Values the API could not read at all, named by their path in the body that
   * was sent, so a caller can say so in its own words. The serializer's message
   * is dropped: it describes types and byte offsets, never the value typed.
   */
  unreadableFields: string[]
}

/** The API names a value it could not parse by its JSON path, as `$.startDateTime`. */
const JSON_PATH_ROOT = '$'

function isJsonPath(key: string): boolean {
  return key === JSON_PATH_ROOT || key.startsWith(`${JSON_PATH_ROOT}.`)
}

function toBodyPath(key: string): string {
  return key.slice(JSON_PATH_ROOT.length + 1)
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
  const unreadableFields: string[] = []

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
      } else if (isJsonPath(key)) {
        unreadableFields.push(toBodyPath(key))
      } else if (!failedToDeserialize) {
        fieldErrors[key] = messages
      }
    }
  }

  return { fieldErrors, formMessages, unreadableFields }
}
