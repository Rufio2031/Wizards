import { computed, toValue, type MaybeRefOrGetter } from 'vue'

import type { ApiFailure } from '@/services/http/validation'

/**
 * Turns a failed write into the copy a form renders.
 * Handles the two kinds of failure the API reports: field errors, which are intended shown under the
 * field they belong to, and form messages, which are shown in a banner.
 *
 * @param failure The last failure, as `useAsyncRequest` reports it.
 * @param fallbackMessage The view's own copy, shown when the API failed without
 * explaining itself. Raw server text is never shown, so this is what stands in.
 * @returns `fieldError`, the first message the API blamed on a named property,
 * and `formError`, the banner copy, which is empty when there is nothing to say.
 */
export function useFormFailure(
  failure: MaybeRefOrGetter<ApiFailure | null>,
  fallbackMessage: string,
) {
  function fieldError(property: string): string | undefined {
    return toValue(failure)?.fieldErrors[property]?.[0]
  }

  // A failure the API attributed entirely to fields needs no banner, since every
  // message is already rendered under the field it belongs to.
  const formError = computed(() => {
    const current = toValue(failure)

    if (!current) {
      return ''
    }

    if (current.formMessages.length > 0) {
      return current.formMessages.join(' ')
    }

    return Object.keys(current.fieldErrors).length > 0 ? '' : fallbackMessage
  })

  return { fieldError, formError }
}
