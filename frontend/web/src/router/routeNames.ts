/** Every navigable route name. Navigate by name so paths stay changeable. */
export const RouteNames = {
  home: 'home',
  events: 'events',
} as const

export type RouteName = (typeof RouteNames)[keyof typeof RouteNames]
