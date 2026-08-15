import { flushPromises } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, type EffectScope } from 'vue'

import { gameTypesApi } from '@/features/gameTypes/api/gameTypesApi'
import { useGameTypes } from '@/features/gameTypes/composables/useGameTypes'
import type { GameTypeTemplate } from '@/features/gameTypes/types/gameType'

vi.mock('@/features/gameTypes/api/gameTypesApi', () => ({
  gameTypesApi: {
    getById: vi.fn(),
    list: vi.fn(),
  },
}))

const list = vi.mocked(gameTypesApi.list)

const chess: GameTypeTemplate = {
  gameTypeId: 'chess',
  name: 'Chess',
  settings: [],
}

const scopes: EffectScope[] = []

function create() {
  const scope = effectScope()

  scopes.push(scope)

  return scope.run(() => useGameTypes())!
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  for (const scope of scopes.splice(0)) {
    scope.stop()
  }

  vi.restoreAllMocks()
  list.mockReset()
})

describe('useGameTypes', () => {
  it('offers an empty catalog until the caller loads it, then every game type the catalog holds', async () => {
    list.mockResolvedValue([chess])

    const { gameTypes, load } = create()

    expect(gameTypes.value).toEqual([])

    await load()
    await flushPromises()

    expect(gameTypes.value).toEqual([
      { gameTypeId: 'chess', name: 'Chess', settings: [] },
    ])
  })
})
