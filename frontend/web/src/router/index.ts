/**
 * Application routes. History mode is safe here because nginx falls unknown
 * paths through to index.html.
 */
import { createRouter, createWebHistory } from 'vue-router'

import HomeView from '../views/HomeView.vue'
import EventsView from '../views/EventsView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    /** Document title for the route, so tabs and history entries are distinct. */
    title: string
  }
}

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView, meta: { title: 'Wizards' } },
    {
      path: '/events',
      name: 'events',
      component: EventsView,
      meta: { title: 'Events · Wizards' },
    },
  ],
})

router.afterEach((to) => {
  document.title = to.meta.title
})
