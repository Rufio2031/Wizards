<script setup lang="ts">
// Read-only presentation of the settings settled for one event.
import { computed } from 'vue'

import type { GameTypeSetting } from '../types/gameType'

const props = defineProps<{
  /** The settings the game type exposes, which name and order the values. */
  settings: GameTypeSetting[]

  /** The values settled for the event, keyed by the setting's key. */
  selections: Record<string, string>
}>()

const BOOLEAN_LABELS: Record<string, string> = { true: 'Yes', false: 'No' }

function present(value: string, setting?: GameTypeSetting): string {
  if (setting?.type !== 'bool') {
    return value
  }

  return BOOLEAN_LABELS[value] ?? value
}

// A setting the game type has since dropped still has a value on the event, so
// it is shown under its key, after every setting the game type still exposes.
const RETIRED_ORDER = Number.MAX_SAFE_INTEGER

const rows = computed(() => {
  const byKey = new Map(props.settings.map((setting) => [setting.key, setting]))

  return Object.entries(props.selections)
    .map(([key, value]) => {
      const setting = byKey.get(key)

      return {
        key,
        label: setting?.label ?? key,
        value: present(value, setting),
        order: setting ? props.settings.indexOf(setting) : RETIRED_ORDER,
      }
    })
    .sort((left, right) => left.order - right.order)
})
</script>

<template>
  <dl class="selection-list">
    <div v-for="row in rows" :key="row.key" class="selection-list__row">
      <dt class="selection-list__label">{{ row.label }}</dt>

      <dd class="selection-list__value">{{ row.value }}</dd>
    </div>
  </dl>
</template>

<style scoped>
.selection-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 0;
}

.selection-list__row {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 8px;
}

.selection-list__label {
  font-size: 0.875rem;
  color: var(--color-status-muted);
}

.selection-list__value {
  margin: 0;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-strong);
}
</style>
