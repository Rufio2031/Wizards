<script setup lang="ts">
import { computed } from 'vue'

import AppField from '@/components/AppField.vue'

import type { GameTypeSetting } from '../types/gameType'

const props = defineProps<{
  setting: GameTypeSetting
  modelValue: string
  error?: string
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const isBounded = computed(
  () =>
    props.setting.type === 'int' &&
    props.setting.minValue !== undefined &&
    props.setting.maxValue !== undefined,
)

// A setting whose bounds meet allows exactly one value, so there is nothing to
// slide and nothing to type.
const isFixed = computed(
  () => isBounded.value && props.setting.minValue === props.setting.maxValue,
)

// A slider needs both ends to place the handle, so a setting bounded on one side
// or neither falls back to the number input on its own.
const hasSlider = computed(() => isBounded.value && !isFixed.value)

const isChecked = computed(() => props.modelValue === 'true')

function onInput(event: Event) {
  const target = event.target as HTMLInputElement | HTMLSelectElement

  emit('update:modelValue', target.value)
}

function onToggle(event: Event) {
  emit('update:modelValue', (event.target as HTMLInputElement).checked ? 'true' : 'false')
}

// Runs on blur rather than on every keystroke, so typing 1 toward 100 is not
// snapped up to the minimum as soon as the first digit lands. An empty or
// unparseable box is left alone for the server to reject.
function onNumberChange(event: Event) {
  const entered = (event.target as HTMLInputElement).value.trim()

  if (entered === '') {
    return
  }

  const parsed = Number(entered)

  if (!Number.isFinite(parsed)) {
    return
  }

  const { minValue, maxValue } = props.setting
  let clamped = parsed

  if (minValue !== undefined) {
    clamped = Math.max(clamped, minValue)
  }

  if (maxValue !== undefined) {
    clamped = Math.min(clamped, maxValue)
  }

  if (String(clamped) !== entered) {
    emit('update:modelValue', String(clamped))
  }
}
</script>

<template>
  <AppField
    v-slot="{ id, describedBy, invalid }"
    :label="setting.label"
    :hint="setting.description"
    :error="error"
    :inline="setting.type === 'bool'"
  >
    <input
      v-if="setting.type === 'bool'"
      :id="id"
      type="checkbox"
      :checked="isChecked"
      :aria-describedby="describedBy"
      @change="onToggle"
    />

    <select
      v-else-if="setting.type === 'enum'"
      :id="id"
      :value="modelValue"
      :aria-describedby="describedBy"
      :aria-invalid="invalid"
      @change="onInput"
    >
      <option v-for="option in setting.options" :key="option" :value="option">
        {{ option }}
      </option>
    </select>

    <div v-else-if="hasSlider" class="setting-field__range">
      <span class="setting-field__bound">{{ setting.minValue }}</span>

      <input
        class="setting-field__slider"
        type="range"
        :value="modelValue"
        :min="setting.minValue"
        :max="setting.maxValue"
        :aria-label="setting.label"
        :aria-describedby="describedBy"
        @input="onInput"
      />

      <span class="setting-field__bound">{{ setting.maxValue }}</span>

      <input
        :id="id"
        class="setting-field__number"
        type="number"
        inputmode="numeric"
        :value="modelValue"
        :min="setting.minValue"
        :max="setting.maxValue"
        :aria-describedby="describedBy"
        :aria-invalid="invalid"
        @input="onInput"
        @change="onNumberChange"
      />
    </div>

    <input
      v-else
      :id="id"
      type="number"
      inputmode="numeric"
      :value="modelValue"
      :min="setting.minValue"
      :max="setting.maxValue"
      :readonly="isFixed"
      :aria-describedby="describedBy"
      :aria-invalid="invalid"
      @input="onInput"
      @change="onNumberChange"
    />
  </AppField>
</template>

<style scoped>
.setting-field__range {
  display: flex;
  align-items: center;
  gap: 8px;
}

.setting-field__slider {
  flex: 1;
  min-width: 0;
  accent-color: var(--color-accent);
}

.setting-field__bound {
  font-size: 0.875rem;
  font-variant-numeric: tabular-nums;
  color: var(--color-status-muted);
}

.setting-field__number {
  width: 5.5rem;
  text-align: right;
}
</style>
