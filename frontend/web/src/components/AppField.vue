<script setup lang="ts">
// A labelled form control, with the hint and error text the control is described by.
import { computed, useId } from 'vue'

const props = defineProps<{
  label: string

  /** Rendered under the control, and announced with it. */
  hint?: string

  /** Rendered under the hint, announced with the control, and marks it invalid. */
  error?: string

  /**
   * Renders the label beside the control rather than above it, for a checkbox
   * whose label reads as its caption.
   */
  inline?: boolean
}>()

const id = useId()
const hintId = computed(() => `${id}-hint`)
const errorId = computed(() => `${id}-error`)

// A control is described by whichever of the two are present, in reading order.
const describedBy = computed(
  () =>
    [props.hint ? hintId.value : null, props.error ? errorId.value : null]
      .filter(Boolean)
      .join(' ') || undefined,
)

// Left undefined rather than false, so an unmarked control carries no attribute.
const invalid = computed<true | undefined>(() => (props.error ? true : undefined))
</script>

<template>
  <div class="app-field" :class="{ 'app-field--inline': inline }">
    <label class="app-field__label" :for="id">{{ label }}</label>

    <slot :id="id" :described-by="describedBy" :invalid="invalid" />

    <p v-if="hint" :id="hintId" class="app-field__hint">{{ hint }}</p>

    <p v-if="error" :id="errorId" class="app-field__error">{{ error }}</p>
  </div>
</template>

<style scoped>
.app-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.app-field__label {
  font-weight: 600;
  color: var(--color-text-strong);
}

/* The label follows the control, so the checkbox leads and the text reads as its
   caption. Hint and error still fall below both. */
.app-field--inline {
  display: grid;
  grid-template-columns: auto 1fr;
  align-items: center;
  column-gap: 8px;
}

.app-field--inline .app-field__label {
  order: 2;
}

.app-field--inline .app-field__hint,
.app-field--inline .app-field__error {
  grid-column: 1 / -1;
}

.app-field__hint {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-status-muted);
}

.app-field__error {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-danger);
}
</style>
