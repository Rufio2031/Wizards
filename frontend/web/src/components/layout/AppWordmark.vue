<script setup lang="ts">
import { APP_NAME } from '@/config/app'
import { RouteNames } from '@/router/routeNames'

withDefaults(
  defineProps<{
    /**
     * Renders the wordmark as a link home. Off for single-task flows that
     * deliberately offer nowhere to navigate.
     */
    linkToHome?: boolean
  }>(),
  { linkToHome: true },
)
</script>

<template>
  <RouterLink
    v-if="linkToHome"
    class="app-wordmark"
    :to="{ name: RouteNames.home }"
  >
    {{ APP_NAME }}
  </RouterLink>

  <p v-else class="app-wordmark">{{ APP_NAME }}</p>
</template>

<style scoped>
.app-wordmark {
  font-family: var(--font-heading);
  font-size: 1.125rem;
  font-weight: 500;
  color: var(--color-text-strong);
  text-decoration: none;
}
</style>
