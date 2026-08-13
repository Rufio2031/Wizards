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

function toDate(value?: string): Date | null {
  if (!value) {
    return null
  }

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime()) ? null : parsed
}

/**
 * Formats a date/time range for display. If the start and end are on the same day,
 * the end is formatted as just a time. Otherwise, both are formatted as full date/times.
 */
export function formatDateTimeRange(
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
