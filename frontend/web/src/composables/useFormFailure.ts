import { computed, toValue, type MaybeRefOrGetter } from 'vue'

import type { ApiFailure } from '@/services/http/validation'

/** Stands in for the serializer's words when the control that holds the value is on screen. */
const UNREADABLE_VALUE = 'We could not read this value. Please check it and try again.'

/** Said once when nothing on the form holds the value that could not be read. */
const UNREADABLE_REQUEST =
  'We could not read some of what you sent. Please check your entries and try again.'

/** Names a property the way a user reads it, since the banner is the only place it is named. */
function toLabel(property: string): string {
  const leaf = property.split('.').at(-1) ?? property
  const words = leaf.replace(/([a-z0-9])([A-Z])/g, '$1 $2')

  return words.charAt(0).toUpperCase() + words.slice(1)
}

/**
 * Turns a failed write into the copy a form renders.
 * Handles the two kinds of failure the API reports: field errors, which are intended shown under the
 * field they belong to, and form messages, which are shown in a banner.
 *
 * @param failure The last failure, as `useAsyncRequest` reports it.
 * @param fallbackMessage The view's own copy, shown when the API failed without
 * explaining itself. Raw server text is never shown, so this is what stands in.
 * @param fields The keys this form has a control for, so a message blamed on
 * anything else can be shown rather than dropped. Pass a getter when the set of
 * controls changes. Declaring none sends every message to the banner, since a
 * form that claims nothing is assumed to render nothing.
 * @returns The form's error copy, read per control and as banner text.
 */
export function useFormFailure(
  failure: MaybeRefOrGetter<ApiFailure | null>,
  fallbackMessage: string,
  fields: MaybeRefOrGetter<readonly string[]> = [],
) {
  // A JSON path names a property as the body carried it, in camelCase, while a
  // validated field is named as the DTO declares it, so the two meet only on casing.
  const controlsByName = computed(
    () => new Map(toValue(fields).map((field) => [field.toLowerCase(), field])),
  )

  function hasControlFor(path: string): boolean {
    return controlsByName.value.has(path.toLowerCase())
  }

  /** Everything a control puts under itself, which is every message the API blamed on it. */
  function messagesFor(property: string): string[] {
    const current = toValue(failure)

    if (!current) {
      return []
    }

    if (current.unreadableFields.some((path) => path.toLowerCase() === property.toLowerCase())) {
      return [UNREADABLE_VALUE]
    }

    return current.fieldErrors[property] ?? []
  }

  function fieldError(property: string): string | undefined {
    const messages = messagesFor(property)

    if (import.meta.env.DEV && messages.length > 0 && !toValue(fields).includes(property)) {
      console.warn(
        `useFormFailure: "${property}" is rendered but not declared, so its message also shows in the banner.`,
      )
    }

    return messages.length > 0 ? messages.join(' ') : undefined
  }

  // Anything a control does not already say. A field renders every message blamed
  // on it, so what reaches the banner is what no control on this form holds:
  // whole keys the form has no control for, and values the API could not read at
  // all. Filtering by message rather than by key keeps a message that a control
  // leaves unsaid from vanishing between the two.
  const unrenderedMessages = computed(() => {
    const current = toValue(failure)

    if (!current) {
      return []
    }

    const declared = new Set(toValue(fields))

    const unclaimed = Object.entries(current.fieldErrors).flatMap(([property, messages]) => {
      const shown = new Set(declared.has(property) ? messagesFor(property) : [])

      return messages
        .filter((message) => !shown.has(message))
        .map((message) => `${toLabel(property)}: ${message}`)
    })

    const unreadable = current.unreadableFields.some((path) => !hasControlFor(path))

    return unreadable ? [...unclaimed, UNREADABLE_REQUEST] : unclaimed
  })

  const formError = computed(() => {
    const current = toValue(failure)

    if (!current) {
      return ''
    }

    const messages = [...current.formMessages, ...unrenderedMessages.value]

    if (messages.length > 0) {
      return messages.join(' ')
    }

    // Every remaining message is already rendered under the field it belongs to,
    // so a banner would only repeat it.
    const isFieldBlamed =
      Object.keys(current.fieldErrors).length > 0 || current.unreadableFields.length > 0

    return isFieldBlamed ? '' : fallbackMessage
  })

  return { fieldError, formError }
}
