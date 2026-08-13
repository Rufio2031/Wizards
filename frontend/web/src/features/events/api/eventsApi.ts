/**
 * Hardcoded event list standing in for the events API, which does not exist yet.
 * Shaped like the response that endpoint is expected to return.
 */
import type { GameEvent } from '../types/event'

export const events: GameEvent[] = [
  {
    id: 'winter-standard-open',
    name: 'Winter Standard Open',
    date: '2026-09-05',
    location: 'Ravenloft Games, Columbus OH',
    capacity: 64,
    registered: 41,
  },
  {
    id: 'draft-night-fall-set',
    name: 'Draft Night: Fall Set',
    date: '2026-09-12',
    location: 'The Dragon Hoard, Dublin OH',
    capacity: 32,
    registered: 32,
  },
  {
    id: 'commander-social',
    name: 'Commander Social',
    date: '2026-09-19',
    location: 'Gateway Cardshop, Westerville OH',
    capacity: 48,
    registered: 12,
  },
  {
    id: 'regional-qualifier',
    name: 'Regional Qualifier',
    date: '2026-10-03',
    location: 'Convention Center Hall B, Columbus OH',
    capacity: 256,
    registered: 187,
  },
]
