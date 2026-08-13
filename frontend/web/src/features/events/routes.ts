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
]
