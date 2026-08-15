import { describe, expect, it } from 'vitest'

import { groupBy } from '@/utils/grouping'

interface Attendee {
  name: string
  city: string
}

const attendees: Attendee[] = [
  { name: 'Merlin', city: 'Boston' },
  { name: 'Circe', city: 'Austin' },
  { name: 'Gandalf', city: 'Boston' },
  { name: 'Morgana', city: 'Cairo' },
  { name: 'Baba Yaga', city: 'Austin' },
]

describe('groupBy', () => {
  it('returns no groups for an empty list', () => {
    expect(groupBy([], (item: string) => item)).toEqual([])
  })

  it('orders groups by where their key first appears, keeping items in the order given', () => {
    const groups = groupBy(attendees, (attendee) => attendee.city)

    expect(groups.map((group) => group.key)).toEqual([
      'Boston',
      'Austin',
      'Cairo',
    ])
    expect(groups[0].items.map((attendee) => attendee.name)).toEqual([
      'Merlin',
      'Gandalf',
    ])
    expect(groups[1].items.map((attendee) => attendee.name)).toEqual([
      'Circe',
      'Baba Yaga',
    ])
  })

  it('groups by numeric keys without coercing them to strings', () => {
    expect(groupBy([1, 2, 3, 4], (value) => value % 2)).toEqual([
      { key: 1, items: [1, 3] },
      { key: 0, items: [2, 4] },
    ])
  })

  it('leaves the input list untouched', () => {
    const input = ['pear', 'apple', 'plum']

    groupBy(input, (fruit) => fruit.slice(0, 1))

    expect(input).toEqual(['pear', 'apple', 'plum'])
  })
})
