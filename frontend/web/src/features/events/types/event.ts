export interface GameType {
  gameTypeId: string
  name: string
}

// The API omits nulls, so `description` and `endDateTime` are absent, never null.
export interface GameEvent {
  eventId: string
  name: string
  description?: string
  startDateTime: string
  endDateTime?: string
  gameType: GameType
}
