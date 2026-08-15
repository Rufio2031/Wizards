<script setup lang="ts">
// The one call to action, rendered as a link when it navigates and a button when it acts.
import type { RouteLocationRaw } from 'vue-router'

withDefaults(
  defineProps<{
    to?: RouteLocationRaw

    /** Ignored when `to` renders a link. A form's submit control needs `submit`. */
    type?: 'button' | 'submit'

    /** Styles as a solid button. */
    primary?: boolean
  }>(),
  { to: undefined, type: 'button', primary: false },
)
</script>

<template>
  <RouterLink
    v-if="to"
    class="app-action"
    :class="{ 'app-action--primary': primary }"
    :to="to"
  >
    <slot />
  </RouterLink>

  <!-- `disabled` is not a prop: it falls through to the button on its own. -->
  <button
    v-else
    class="app-action"
    :class="{ 'app-action--primary': primary }"
    :type="type"
  >
    <slot />
  </button>
</template>

<style scoped>
.app-action {
  display: inline-block;
  padding: 8px 16px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  font: inherit;
  color: var(--color-accent);
  background: var(--color-accent-soft);
  text-decoration: none;
  cursor: pointer;
}

.app-action:hover {
  box-shadow: var(--shadow-sm);
}

.app-action--primary {
  color: var(--color-bg);
  background: var(--color-accent);
}

.app-action:disabled {
  cursor: progress;
  opacity: 0.7;
}
</style>
