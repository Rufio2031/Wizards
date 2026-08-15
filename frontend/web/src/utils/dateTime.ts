/** A calendar day in the browser's time zone: a sortable key and its heading. */
export interface LocalDay {
  key: string
  label: string
}

const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
})

const timeFormatter = new Intl.DateTimeFormat(undefined, {
  hour: 'numeric',
  minute: '2-digit',
})

const dayHeadingFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
  year: 'numeric',
})

function toDate(value?: string): Date | null {
  if (!value) {
    return null
  }

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime()) ? null : parsed
}

const UNKNOWN_SCHEDULE_LABEL = 'Date to be announced'
const UNKNOWN_DAY_KEY = 'unknown-day'

/**
 * Formats a date/time range for display. If the start and end are on the same day,
 * the end is formatted as just a time. Otherwise, both are formatted as full date/times.
 */
function formatDateTimeRange(
  startValue?: string,
  endValue?: string,
): string | null {
  const start = toDate(startValue)

  if (!start) {
    return null
  }

  const startLabel = dateTimeFormatter.format(start)
  const end = toDate(endValue)

  if (!end) {
    return startLabel
  }

  const endsSameDay = end.toDateString() === start.toDateString()

  const endLabel = endsSameDay
    ? timeFormatter.format(end)
    : dateTimeFormatter.format(end)

  return `${startLabel} to ${endLabel}`
}

export function formatSchedule(startValue?: string, endValue?: string): string {
  return formatDateTimeRange(startValue, endValue) ?? UNKNOWN_SCHEDULE_LABEL
}

function padded(value: number): string {
  return String(value).padStart(2, '0')
}

function toLocalDayKey(date: Date): string {
  return `${date.getFullYear()}-${padded(date.getMonth() + 1)}-${padded(date.getDate())}`
}

export function toLocalDay(value?: string): LocalDay {
  const date = toDate(value)

  if (!date) {
    return { key: UNKNOWN_DAY_KEY, label: UNKNOWN_SCHEDULE_LABEL }
  }

  return {
    key: toLocalDayKey(date),
    label: dayHeadingFormatter.format(date),
  }
}

/**
 * Formats an instant as the local `YYYY-MM-DDTHH:mm` a `datetime-local` input
 * reads, which an ISO instant is not.
 */
export function toDateTimeLocalValue(date: Date): string {
  return `${toLocalDayKey(date)}T${padded(date.getHours())}:${padded(date.getMinutes())}`
}

/** Parses a `datetime-local` value as an ISO instant, or `null` when unreadable. */
export function toUtcInstant(localValue: string): string | null {
  return toDate(localValue)?.toISOString() ?? null
}
