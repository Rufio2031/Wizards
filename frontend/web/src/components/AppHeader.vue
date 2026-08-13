<script setup lang="ts">
// Site header: wordmark plus the top-level navigation shared by every page.
</script>

<template>
  <header class="app-header">
    <div class="app-header__inner">
      <RouterLink class="app-header__brand" to="/">Wizards</RouterLink>

      <nav class="app-header__nav">
        <RouterLink class="app-header__link" to="/">Home</RouterLink>
        <RouterLink class="app-header__link" to="/events">Events</RouterLink>
      </nav>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  border-bottom: 1px solid var(--color-border);
}

/* Matches .app-main so the nav lines up with page content on wide viewports. */
.app-header__inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  max-width: 900px;
  margin: 0 auto;
  padding: 16px 24px;
}

.app-header__brand {
  font-family: var(--font-heading);
  font-size: 1.125rem;
  font-weight: 500;
  color: var(--color-text-strong);
  text-decoration: none;
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
