<script setup lang="ts">
// The banner for any error the user must notice, whether a rejected submission or a failed fetch.
defineProps<{
  /** The banner copy. Nothing renders while it is empty. */
  message?: string
}>()
</script>

<template>
  <p v-if="message" class="app-error-message" role="alert">{{ message }}</p>
</template>

<style scoped>
.app-error-message {
  margin: 0;
  padding: 12px;
  border: 1px solid var(--color-danger-border);
  border-radius: 6px;
  color: var(--color-danger);
  background: var(--color-danger-soft);
}
</style>
