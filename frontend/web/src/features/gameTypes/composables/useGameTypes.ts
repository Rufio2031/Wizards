import { useAsyncRequest } from '@/composables/useAsyncRequest'

import { gameTypesApi } from '../api/gameTypesApi'
import type { GameTypeTemplate } from '../types/gameType'

export function useGameTypes() {
  const {
    data: gameTypes,
    isLoading,
    error,
    run: load,
  } = useAsyncRequest<GameTypeTemplate[]>((options) => gameTypesApi.list(options), {
    initialValue: [],
    failureMessage: 'Loading game types failed.',
  })

  return { gameTypes, isLoading, error, load }
}
