export interface GameType {
  gameTypeId: string
  name: string
}

// The API omits nulls, so `description` is absent, never null.
export interface GameEvent {
  eventId: string
  name: string
  description?: string
  location: string
  startDateTime: string
  endDateTime: string
  registrationLimit: number
  gameType: GameType

  /**
   * The settings settled for this event, keyed by the game type setting's key.
   */
  selections: Record<string, string>
}

export interface CreateRegistrationRequest {
  name: string
}

export interface Registration {
  name: string
}

export const REGISTRATION_LIMIT = { min: 1, max: 30 } as const

export interface CreateEventRequest {
  name: string
  description?: string
  location: string
  startDateTime: string
  endDateTime: string
  registrationLimit: number
  gameType: {
    gameTypeId: string
    selections: Record<string, string>
  }
}
