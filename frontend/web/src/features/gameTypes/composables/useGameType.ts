import { toValue, watch, type MaybeRefOrGetter } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { gameTypesApi } from '../api/gameTypesApi'
import type { GameTypeTemplate } from '../types/gameType'

/**
 * Reads one game type and the settings it exposes, reloading when the
 * identifier changes.
 *
 * @param gameTypeId The game type to read, or `undefined` while the caller does
 * not know it yet, which reads nothing.
 */
export function useGameType(gameTypeId: MaybeRefOrGetter<string | undefined>) {
  const {
    data: gameType,
    isLoading,
    error,
    run,
  } = useAsyncRequest<GameTypeTemplate | null, string>(
    (options, id) => gameTypesApi.getById(id, options),
    {
      initialValue: null,
      failureMessage: 'Loading the game type failed.',
    },
  )

  function load() {
    const id = toValue(gameTypeId)

    return id ? run(id) : Promise.resolve(null)
  }

  watch(() => toValue(gameTypeId), () => void load(), { immediate: true })

  return { gameType, isLoading, error, load }
}
