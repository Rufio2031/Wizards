<script setup lang="ts">
import { RouteNames } from '@/router/routeNames'

import AppContainer from './AppContainer.vue'
import AppWordmark from './AppWordmark.vue'
</script>

<template>
  <header class="app-header">
    <AppContainer>
      <div class="app-header__inner">
        <AppWordmark />

        <nav class="app-header__nav">
          <RouterLink class="app-header__link" :to="{ name: RouteNames.home }">
            Home
          </RouterLink>
          <RouterLink class="app-header__link" :to="{ name: RouteNames.events }">
            Events
          </RouterLink>
        </nav>
      </div>
    </AppContainer>
  </header>
</template>

<style scoped>
.app-header {
  border-bottom: 1px solid var(--color-border);
}

.app-header__inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-block: 16px;
}

.app-header__nav {
  display: flex;
  gap: 8px;
}

.app-header__link {
  padding: 4px 12px;
  border-radius: 6px;
  font-size: 0.875rem;
  color: var(--color-text);
  text-decoration: none;
}

.app-header__link:hover {
  color: var(--color-text-strong);
  background: var(--color-surface-subtle);
}

.app-header__link.router-link-exact-active {
  color: var(--color-accent);
  background: var(--color-accent-soft);
}
</style>
