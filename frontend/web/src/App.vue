<script setup lang="ts">
// Root shell: the shared header above whatever the current route renders.
import AppHeader from './components/AppHeader.vue'
</script>

<template>
  <AppHeader />

  <main class="app-main">
    <RouterView />
  </main>
</template>

<style scoped>
.app-main {
  max-width: 900px;
  margin: 0 auto;
  padding: 24px;
}
</style>
