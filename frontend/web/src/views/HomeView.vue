<script setup lang="ts">
import { RouteNames } from '@/router/routeNames'
</script>

<template>
  <section class="home">
    <h1 class="home__title">Wizards</h1>

    <p class="home__lede">
      Wizards is where tabletop card game events live. Organizers post an event,
      players find it and claim a seat, and everyone sees the same seat count.
    </p>

    <p>
      Nothing is registerable yet. This is the first skeleton, and the events
      API is not live, so the list comes up empty.
    </p>

    <RouterLink class="home__cta" :to="{ name: RouteNames.events }">
      Browse events
    </RouterLink>
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
  padding: 8px 16px;
  border-radius: 6px;
  color: var(--color-accent);
  background: var(--color-accent-soft);
  border: 1px solid var(--color-accent-border);
  text-decoration: none;
}

.home__cta:hover {
  box-shadow: var(--shadow-sm);
}
</style>
