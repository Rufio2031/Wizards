<template>
  <div class="app-container">
    <slot />
  </div>
</template>

<style scoped>
/* Vertical rhythm belongs to whatever wraps the container, so it only ever
   constrains the inline axis. */
.app-container {
  max-width: 900px;
  margin: 0 auto;
  padding-inline: 24px;
}
</style>
