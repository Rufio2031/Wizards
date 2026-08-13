/**
 * Application routes. History mode is safe here because nginx falls unknown
 * paths through to index.html.
 */
import { createRouter, createWebHistory } from 'vue-router'

import { APP_NAME } from '@/config/app'
import { eventsRoutes } from '@/features/events/routes'
import DefaultLayout from '@/layouts/DefaultLayout.vue'
import HomeView from '@/views/HomeView.vue'

import { RouteNames } from './routeNames'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: DefaultLayout,
      children: [
        {
          path: '',
          name: RouteNames.home,
          component: HomeView,
          meta: { title: 'Home' },
        },
        ...eventsRoutes,
        {
          path: ':pathMatch(.*)*',
          component: () => import('@/views/NotFoundView.vue'),
          meta: { title: 'Page not found' },
        },
      ],
    },
  ],
  scrollBehavior(to, _from, savedPosition) {
    if (to.hash) {
      return { el: to.hash }
    }

    return savedPosition ?? { top: 0 }
  },
})

router.afterEach((to, from) => {
  document.title = to.meta.title ? `${to.meta.title} · ${APP_NAME}` : APP_NAME

  // A client-side navigation leaves focus where it was, so assistive tech never
  // hears that the page changed. The first render is the browser's to own.
  if (from.matched.length === 0) {
    return
  }

  requestAnimationFrame(() => {
    document.getElementById('main')?.focus({ preventScroll: true })
  })
})
