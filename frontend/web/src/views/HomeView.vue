<script setup lang="ts">
import { RouteNames } from '@/router/routeNames'
import AppAction from '@/components/AppAction.vue'
</script>

<template>
  <section class="home">
    <h1 class="home__title">Wizards</h1>

    <p class="home__lede">
      Wizards is where tabletop card game events live. Organizers post an event,
      players find it and claim a seat, and everyone sees the same seat count.
    </p>

    <AppAction class="home__cta" :to="{ name: RouteNames.events }">
      Browse events
    </AppAction>
  </section>
</template>

<style scoped>
.home {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 16px;
}

/* The flex gap already spaces the heading; the base h1 margin would double it. */
.home__title {
  margin: 0;
}

.home__lede {
  font-size: 1.125rem;
  color: var(--color-text-strong);
}

.home__cta {
  margin-top: 8px;
}
</style>
