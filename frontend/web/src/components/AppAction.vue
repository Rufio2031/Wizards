<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

defineProps<{
  to?: RouteLocationRaw
}>()
</script>

<template>
  <RouterLink v-if="to" class="app-action" :to="to">
    <slot />
  </RouterLink>

  <button v-else class="app-action" type="button">
    <slot />
  </button>
</template>

<style scoped>
.app-action {
  display: inline-block;
  padding: 8px 16px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  font: inherit;
  color: var(--color-accent);
  background: var(--color-accent-soft);
  text-decoration: none;
  cursor: pointer;
}

.app-action:hover {
  box-shadow: var(--shadow-sm);
}
</style>
