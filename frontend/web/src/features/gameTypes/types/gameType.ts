/** How a setting's value is read, and so which input it is presented as. */
export type SettingType = 'int' | 'bool' | 'enum'

// The API omits nulls, so the optional members are absent, never null.
export interface GameTypeSetting {
  /** Submitted back under this key, and never shown to an organizer. */
  key: string
  label: string
  description?: string
  type: SettingType

  /** Bounds on an `int` setting; absent when it is unbounded, or not an `int`. */
  minValue?: number
  maxValue?: number

  /** Always a value this setting accepts, so it is safe to prefill. */
  defaultValue: string

  /** The values an `enum` setting allows. Empty for every other type. */
  options: string[]
}

/** A game and the settings an event played with it can choose. */
export interface GameTypeTemplate {
  gameTypeId: string
  name: string
  settings: GameTypeSetting[]
}
