import type { RouteRecordRaw } from 'vue-router'

import { RouteNames } from '@/router/routeNames'

/** Children of the default layout. Paths are relative to the layout's `/`. */
export const eventsRoutes: RouteRecordRaw[] = [
  {
    path: 'events',
    name: RouteNames.events,
    component: () => import('./views/EventsView.vue'),
    meta: { title: 'Events' },
  },
  {
    path: 'events/new',
    name: RouteNames.eventCreate,
    component: () => import('./views/CreateEventView.vue'),
    meta: { title: 'Schedule an event' },
  },
  {
    path: 'events/:eventId',
    name: RouteNames.eventDetail,
    component: () => import('./views/EventDetailView.vue'),
    props: true,
    meta: { title: 'Event' },
  },
]
