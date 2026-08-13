<script setup lang="ts">
// Minimal chrome for single-task flows reached by QR code on mobile: no
// organizer navigation, nothing to wander off into mid-registration.
import AppContainer from '@/components/layout/AppContainer.vue'
import AppWordmark from '@/components/layout/AppWordmark.vue'
</script>

<template>
  <div class="focused-layout">
    <header class="focused-layout__header">
      <AppContainer>
        <AppWordmark :link-to-home="false" />
      </AppContainer>
    </header>

    <main id="main" class="focused-layout__main" tabindex="-1">
      <AppContainer>
        <RouterView />
      </AppContainer>
    </main>
  </div>
</template>

<style scoped>
.focused-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  /* Mobile browser chrome eats part of 100vh, which would push the footer of a
     registration flow under the URL bar. */
  min-height: 100dvh;
}

.focused-layout__header {
  padding-block: 16px;
  border-bottom: 1px solid var(--color-border);
}

.focused-layout__main {
  flex: 1;
  padding-block: 24px;
}

.focused-layout__main:focus:not(:focus-visible) {
  outline: none;
}
</style>
