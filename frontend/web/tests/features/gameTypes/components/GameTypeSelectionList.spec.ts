import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import GameTypeSelectionList from '@/features/gameTypes/components/GameTypeSelectionList.vue'
import type { GameTypeSetting } from '@/features/gameTypes/types/gameType'

function settingOf(overrides: Partial<GameTypeSetting>): GameTypeSetting {
  return {
    key: 'rounds',
    label: 'Rounds',
    type: 'int',
    defaultValue: '3',
    options: [],
    ...overrides,
  }
}

const SETTINGS: GameTypeSetting[] = [
  settingOf({ key: 'rounds', label: 'Rounds' }),
  settingOf({ key: 'format', label: 'Format', type: 'enum', options: ['Draft'] }),
  settingOf({ key: 'ranked', label: 'Ranked play', type: 'bool' }),
]

function rowsOf(selections: Record<string, string>) {
  const wrapper = mount(GameTypeSelectionList, {
    props: { settings: SETTINGS, selections },
  })

  const labels = wrapper.findAll('dt').map((label) => label.text())
  const values = wrapper.findAll('dd').map((value) => value.text())

  return labels.map((label, index) => [label, values[index]])
}

describe('GameTypeSelectionList', () => {
  it('reads in the order the game type declares its settings, not the order the values arrived', () => {
    const rows = rowsOf({ ranked: 'true', rounds: '5', format: 'Draft' })

    expect(rows.map(([label]) => label)).toEqual([
      'Rounds',
      'Format',
      'Ranked play',
    ])
  })

  it('titles each value with the human label, and reads a bool as a word', () => {
    expect(rowsOf({ rounds: '5', ranked: 'true' })).toEqual([
      ['Rounds', '5'],
      ['Ranked play', 'Yes'],
    ])

    expect(rowsOf({ ranked: 'false' })).toEqual([['Ranked play', 'No']])
  })

  it('still shows a value whose setting the game type has dropped, by key, after the known ones', () => {
    const rows = rowsOf({ retiredHouseRule: 'Chaos', ranked: 'true', rounds: '5' })

    expect(rows).toEqual([
      ['Rounds', '5'],
      ['Ranked play', 'Yes'],
      ['retiredHouseRule', 'Chaos'],
    ])
  })
})
