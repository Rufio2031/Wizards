import { describe, expect, it } from 'vitest'

import {
  formatSchedule,
  toDateTimeLocalValue,
  toLocalDay,
  toUtcInstant,
} from '@/utils/dateTime'

const UNKNOWN = 'Date to be announced'

// Local-time literals (no zone suffix) so a run's time zone cannot shift the
// calendar day being asserted.
const MARCH_14_MORNING = '2026-03-14T09:30:00'
const MARCH_14_EVENING = '2026-03-14T17:45:00'
const MARCH_15_MORNING = '2026-03-15T09:30:00'

function endSegmentOf(label: string): string {
  const parts = label.split(' to ')

  expect(parts).toHaveLength(2)

  return parts[1]
}

describe('formatSchedule', () => {
  it('announces an unknown date when the start is missing or unparsable', () => {
    expect(formatSchedule()).toBe(UNKNOWN)
    expect(formatSchedule('not a date')).toBe(UNKNOWN)
  })

  it('renders the start alone when there is no usable end', () => {
    const label = formatSchedule(MARCH_14_MORNING, 'not a date')

    expect(label).toContain('2026')
    expect(label).toContain('14')
    expect(label).toContain(':30')
    expect(label).not.toContain(' to ')
  })

  it('renders a same-day end as a time only, without repeating the date', () => {
    const end = endSegmentOf(formatSchedule(MARCH_14_MORNING, MARCH_14_EVENING))

    expect(end).toContain(':45')
    expect(end).not.toContain('2026')
    expect(end).not.toContain('14')
  })

  it('renders an end on a later day as a full date and time', () => {
    const end = endSegmentOf(formatSchedule(MARCH_14_MORNING, MARCH_15_MORNING))

    expect(end).toContain('2026')
    expect(end).toContain('15')
    expect(end).toContain(':30')
  })
})

describe('toLocalDay', () => {
  it('keys a day as a sortable year-month-day, padded, and labels it', () => {
    expect(toLocalDay(MARCH_14_MORNING).key).toBe('2026-03-14')
    expect(toLocalDay('2026-01-05T00:00:00').key).toBe('2026-01-05')

    const label = toLocalDay(MARCH_14_MORNING).label

    expect(label).toContain('2026')
    expect(label).toContain('14')
  })

  it('keys the last instant of a day to that day, not the next', () => {
    expect(toLocalDay('2026-12-31T23:59:59').key).toBe('2026-12-31')
  })

  it('falls back to a shared unknown key when the value is missing or unparsable', () => {
    expect(toLocalDay()).toEqual({ key: 'unknown-day', label: UNKNOWN })
    expect(toLocalDay('not a date')).toEqual({
      key: 'unknown-day',
      label: UNKNOWN,
    })
  })
})

describe('toUtcInstant', () => {
  it('returns the UTC instant the local value names', () => {
    expect(toUtcInstant('2026-03-14T09:30:00-05:00')).toBe(
      '2026-03-14T14:30:00.000Z',
    )

    // A zoneless value is the browser's wall clock, so the instant it names must
    // read back as that same wall clock wherever the suite runs.
    const instant = toUtcInstant(MARCH_14_MORNING)

    expect(instant).not.toBeNull()

    const readBack = new Date(instant!)

    expect(readBack.getHours()).toBe(9)
    expect(readBack.getMinutes()).toBe(30)
    expect(readBack.getDate()).toBe(14)
  })

  it('returns null rather than throwing when the value is unreadable', () => {
    expect(toUtcInstant('not a date')).toBeNull()
    expect(toUtcInstant('')).toBeNull()
    expect(toUtcInstant('2026-02-30T99:99')).toBeNull()
  })
})

describe('toDateTimeLocalValue', () => {
  it('emits the local date and time a datetime-local input reads, zero padded', () => {
    expect(toDateTimeLocalValue(new Date(2026, 0, 5, 7, 4))).toBe(
      '2026-01-05T07:04',
    )
    expect(toDateTimeLocalValue(new Date(2026, 11, 31, 23, 59))).toBe(
      '2026-12-31T23:59',
    )
  })

  it('is not an ISO instant: no seconds, and no zone marker to shift the day', () => {
    const midnight = new Date(2026, 2, 14, 0, 0)

    expect(toDateTimeLocalValue(midnight)).toBe('2026-03-14T00:00')
  })
})
