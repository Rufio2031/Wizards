/**
 * Shape of a single tabletop card game event as the API returns it.
 *
 * The API has no events controller yet, so no response DTO exists to mirror.
 * This is the provisional contract and must be reconciled with the real DTO
 * once it lands, the backend being the source of truth.
 */
export interface GameEvent {
  /** Stable identifier, used as the list key. */
  id: string

  /** Display name of the event. */
  name: string

  /** Start of the event as an ISO 8601 date or date-time string. */
  date: string

  /** Venue or store hosting the event. */
  location: string

  /** Total number of seats the venue can seat. */
  capacity: number

  /**
   * Seats already claimed. Expected to stay within `capacity`, but consumers
   * should not assume it: this comes from an API.
   */
  registered: number
}
