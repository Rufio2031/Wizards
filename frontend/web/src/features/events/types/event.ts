export interface GameType {
  gameTypeId: string
  name: string
}

// The API omits nulls, so `description` is absent, never null.
export interface GameEvent {
  eventId: string
  name: string
  description?: string
  startDateTime: string
  endDateTime: string
  gameType: GameType

  /**
   * The settings settled for this event, keyed by the game type setting's key.
   * Carries every setting the game type exposed when the event was created,
   * including the ones left at their default.
   */
  selections: Record<string, string>
}

/** The details a new event is created from. */
export interface CreateEventRequest {
  name: string
  description?: string
  startDateTime: string
  endDateTime: string
  gameType: {
    gameTypeId: string
    selections: Record<string, string>
  }
}
