import { isApiError } from './ApiError'

/**
 * A failed call's messages, split by whether the API blamed a particular field.
 *
 * Only messages the API attributed to a key are carried. `ApiError.message`
 * never appears here, so a caller can render these while still keeping raw
 * server text out of the page.
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
    for (const [key, messages] of Object.entries(error.errors)) {
      if (key === '') {
        formMessages.push(...messages)
      } else {
        fieldErrors[key] = messages
      }
    }
  }

  return { fieldErrors, formMessages }
}
