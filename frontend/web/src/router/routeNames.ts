/** Every navigable route name. Navigate by name so paths stay changeable. */
export const RouteNames = {
  home: 'home',
  events: 'events',
  eventCreate: 'event-create',
  eventDetail: 'event-detail',
} as const

export type RouteName = (typeof RouteNames)[keyof typeof RouteNames]
