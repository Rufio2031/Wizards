import { httpClient, type RequestOptions } from '@/services/http/httpClient'

import type { GameTypeTemplate } from '../types/gameType'

const GAME_TYPES_PATH = '/gametypes'

/** The only place game type route paths are known. */
export const gameTypesApi = {
  async list(options?: RequestOptions): Promise<GameTypeTemplate[]> {
    const gameTypes = await httpClient.get<GameTypeTemplate[]>(
      GAME_TYPES_PATH,
      options,
    )

    if (!gameTypes) {
      throw new Error('The game types endpoint returned no body.')
    }

    return gameTypes
  },
}
