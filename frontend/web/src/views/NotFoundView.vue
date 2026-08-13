<script setup lang="ts">
import { RouteNames } from '@/router/routeNames'
</script>

<template>
  <section class="not-found">
    <h1 class="not-found__title">Page not found</h1>

    <p>That page does not exist, or it moved.</p>

    <RouterLink class="not-found__link" :to="{ name: RouteNames.home }">
      Back to home
    </RouterLink>
  </section>
</template>

<style scoped>
.not-found {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 16px;
}

.not-found__title {
  margin: 0;
}

.not-found__link {
  margin-top: 8px;
  padding: 8px 16px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  color: var(--color-accent);
  background: var(--color-accent-soft);
  text-decoration: none;
}

.not-found__link:hover {
  box-shadow: var(--shadow-sm);
}
</style>
