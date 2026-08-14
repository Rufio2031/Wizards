<script setup lang="ts">
import AppAction from '@/components/AppAction.vue'
import { RouteNames } from '@/router/routeNames'
</script>

<template>
  <section class="not-found">
    <h1 class="not-found__title">Page not found</h1>

    <p>That page does not exist, or it moved.</p>

    <AppAction class="not-found__link" :to="{ name: RouteNames.home }">
      Back to home
    </AppAction>
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
}
</style>
