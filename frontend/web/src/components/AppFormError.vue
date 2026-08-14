<script setup lang="ts">
// The banner a form shows for a failure that belongs to no single field.
defineProps<{
  /** The banner copy. Nothing renders while it is empty. */
  message?: string
}>()
</script>

<template>
  <p v-if="message" class="app-form-error" role="alert">{{ message }}</p>
</template>

<style scoped>
.app-form-error {
  margin: 0;
  padding: 12px;
  border: 1px solid var(--color-danger-border);
  border-radius: 6px;
  color: var(--color-danger);
  background: var(--color-danger-soft);
}
</style>
