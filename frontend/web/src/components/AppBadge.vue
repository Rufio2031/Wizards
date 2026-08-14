<template>
  <div class="app-badge">
    <slot />
  </div>
</template>

<style scoped>
.app-badge {
  width: fit-content;
  padding: 4px 12px;
  border-radius: 999px;
  font-size: 0.8125rem;
  line-height: 1.45;
  color: var(--color-accent);
  background: var(--color-accent-soft);
}
</style>
