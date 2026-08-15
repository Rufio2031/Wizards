import { flushPromises } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref, type EffectScope } from 'vue'

import { gameTypesApi } from '@/features/gameTypes/api/gameTypesApi'
import { useGameType } from '@/features/gameTypes/composables/useGameType'
import type { GameTypeTemplate } from '@/features/gameTypes/types/gameType'

vi.mock('@/features/gameTypes/api/gameTypesApi', () => ({
  gameTypesApi: {
    getById: vi.fn(),
    list: vi.fn(),
  },
}))

const getById = vi.mocked(gameTypesApi.getById)

const chess: GameTypeTemplate = {
  gameTypeId: 'chess',
  name: 'Chess',
  settings: [
    {
      key: 'timeControl',
      label: 'Time control',
      type: 'enum',
      defaultValue: 'blitz',
      options: ['blitz', 'rapid'],
    },
  ],
}

const scopes: EffectScope[] = []

function create(...args: Parameters<typeof useGameType>) {
  const scope = effectScope()

  scopes.push(scope)

  return scope.run(() => useGameType(...args))!
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  for (const scope of scopes.splice(0)) {
    scope.stop()
  }

  vi.restoreAllMocks()
  getById.mockReset()
})

describe('useGameType', () => {
  it('reads nothing while the caller does not know the identifier, then reads it once it does', async () => {
    getById.mockResolvedValue(chess)

    const gameTypeId = ref<string | undefined>(undefined)
    const { gameType, isLoading, error } = create(gameTypeId)

    await flushPromises()

    expect(getById).not.toHaveBeenCalled()
    expect(gameType.value).toBeNull()
    expect(isLoading.value).toBe(false)
    expect(error.value).toBeNull()

    gameTypeId.value = 'chess'
    await flushPromises()

    expect(gameType.value).toEqual(chess)
  })
})
