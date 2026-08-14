<script setup lang="ts">
import { APP_NAME } from '@/config/app'

import AppContainer from './AppContainer.vue'

const year = new Date().getFullYear()
</script>

<template>
  <footer class="app-footer">
    <AppContainer>
      <p class="app-footer__text">{{ APP_NAME }} &middot; {{ year }}</p>
    </AppContainer>
  </footer>
</template>

<style scoped>
.app-footer {
  border-top: 1px solid var(--color-border);
}

.app-footer__text {
  padding-block: 16px;
  font-size: 0.875rem;
}
</style>
