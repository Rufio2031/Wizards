<script setup lang="ts">
// The app name, linking home.
import { APP_NAME } from '@/config/app'
import { RouteNames } from '@/router/routeNames'
</script>

<template>
  <RouterLink class="app-wordmark" :to="{ name: RouteNames.home }">
    {{ APP_NAME }}
  </RouterLink>
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
