/**
 * Shape of a single tabletop card game event.
 *
 * Mirrors what the events API is expected to return, so swapping the hardcoded
 * list for a real fetch should not change any component that consumes it.
 */
export interface GameEvent {
  /** Stable identifier, used as the list key. */
  id: string

  /** Display name of the event. */
  name: string

  /** Start date as an ISO `YYYY-MM-DD` string. */
  date: string

  /** Venue or store hosting the event. */
  location: string

  /** Total number of seats the venue can seat. */
  capacity: number

  /**
   * Seats already claimed. Expected to stay within `capacity`, but consumers
   * should not assume it: this will come from an API.
   */
  registered: number
}
