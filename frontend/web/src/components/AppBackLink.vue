<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

defineProps<{
  to: RouteLocationRaw
}>()
</script>

<template>
  <RouterLink class="app-back-link" :to="to">
    <span class="app-back-link__icon" aria-hidden="true">‹</span>
    <slot />
  </RouterLink>
</template>

<style scoped>
.app-back-link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 0.8125rem;
  color: var(--color-text);
  text-decoration: none;
}

.app-back-link:hover {
  color: var(--color-text-strong);
  text-decoration: underline;
}

.app-back-link__icon {
  font-size: 1rem;
  line-height: 1;
}
</style>
