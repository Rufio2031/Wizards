<script setup lang="ts">
import AppContainer from '@/components/layout/AppContainer.vue'
import AppFooter from '@/components/layout/AppFooter.vue'
import AppHeader from '@/components/layout/AppHeader.vue'
</script>

<template>
  <div class="default-layout">
    <a class="default-layout__skip" href="#main">Skip to content</a>

    <AppHeader />

    <main id="main" class="default-layout__main" tabindex="-1">
      <AppContainer>
        <RouterView />
      </AppContainer>
    </main>

    <AppFooter />
  </div>
</template>

<style scoped>
.default-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  min-height: 100dvh;
}

/* Off-canvas rather than display:none so screen readers still reach it. */
.default-layout__skip {
  position: absolute;
  left: -9999px;
  top: 0;
}

.default-layout__skip:focus {
  left: 8px;
  top: 8px;
  z-index: 1;
  padding: 8px 16px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  color: var(--color-text-strong);
  background: var(--color-surface);
  text-decoration: none;
}

.default-layout__main {
  flex: 1;
  padding-block: 24px;
}

/* Scoped to non-keyboard focus so the skip link and route changes still show
   the global focus ring when they move focus here. */
.default-layout__main:focus:not(:focus-visible) {
  outline: none;
}
</style>
