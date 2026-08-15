import { computed, shallowRef, toValue, type MaybeRefOrGetter } from 'vue'

import type { ApiFailure } from '@/services/http/validation'

/** Names a property the way a user reads it, since the banner is the only place it is named. */
function toLabel(property: string): string {
  const leaf = property.split('.').at(-1) ?? property
  const words = leaf.replace(/([a-z0-9])([A-Z])/g, '$1 $2')

  return words.charAt(0).toUpperCase() + words.slice(1)
}

/**
 * Turns a failed write into the copy a form renders.
 * Field errors are read under the field they belong to, and everything else is read as banner text.
 *
 * @param failure The last failure, as `useAsyncRequest` reports it.
 * @param fallbackMessage The view's own copy, shown when the API failed without
 * explaining itself. Raw server text is never shown, so this is what stands in.
 * @param fields The keys this form has a control for, so a message blamed on
 * anything else reaches the banner rather than being dropped. Pass a getter when
 * the set of controls changes. Declaring none sends every message to the banner.
 * @returns The form's error copy, read per control and as banner text, with the
 * means to retire what an edit can plausibly fix.
 */
export function useFormFailure(
  failure: MaybeRefOrGetter<ApiFailure | null>,
  fallbackMessage: string,
  fields: MaybeRefOrGetter<readonly string[]> = [],
) {
  const retired = shallowRef<ApiFailure | null>(null)

  const isRetired = computed(() => {
    const current = toValue(failure)

    return current !== null && current === retired.value
  })

  function messagesFor(property: string): string[] {
    const current = toValue(failure)

    if (!current || isRetired.value) {
      return []
    }

    return current.fieldErrors[property] ?? []
  }

  function fieldError(property: string): string | undefined {
    const messages = messagesFor(property)

    return messages.length > 0 ? messages.join(' ') : undefined
  }

  // Messages blamed on a field this form has no control for, which would otherwise
  // go unread. Labelled, since the banner is the only place that field is named.
  const unrenderedMessages = computed(() => {
    const current = toValue(failure)

    if (!current || isRetired.value) {
      return []
    }

    const declared = new Set(toValue(fields))

    return Object.entries(current.fieldErrors)
      .filter(([property]) => !declared.has(property))
      .flatMap(([property, messages]) =>
        messages.map((message) => `${toLabel(property)}: ${message}`),
      )
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
    const isFieldBlamed = Object.keys(current.fieldErrors).length > 0

    return isFieldBlamed ? '' : fallbackMessage
  })

  /**
   * Retires what the API blamed on a field, wherever it is rendered, and leaves
   * the banner's own messages standing. An edit can plausibly fix a rejected
   * value, but nothing typed into this form changes what the request as a whole
   * was refused for.
   */
  function clearFieldErrors() {
    retired.value = toValue(failure)
  }

  return { fieldError, formError, clearFieldErrors }
}
