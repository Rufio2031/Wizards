<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import AppAction from '@/components/AppAction.vue'
import AppField from '@/components/AppField.vue'
import { useFormFailure } from '@/composables/useFormFailure'
import GameTypeSettingField from '@/features/gameTypes/components/GameTypeSettingField.vue'
import { useGameTypes } from '@/features/gameTypes/composables/useGameTypes'
import type { GameTypeTemplate } from '@/features/gameTypes/types/gameType'
import { RouteNames } from '@/router/routeNames'

import { useCreateEvent } from '../composables/useCreateEvent'

const UNEXPECTED_FAILURE = 'We could not schedule the event just now. Please try again.'

const router = useRouter()

const { gameTypes, isLoading: isLoadingGameTypes, error: gameTypesError, load } = useGameTypes()
const { isSaving, failure, create, clearFailure } = useCreateEvent()

const name = ref('')
const description = ref('')
const startDateTime = ref('')
const endDateTime = ref('')
const selectedGameTypeId = ref('')

/** Keyed by setting key, and only ever holding the currently selected game's settings. */
const selections = ref<Record<string, string>>({})

const selectedGameType = computed<GameTypeTemplate | undefined>(() =>
  gameTypes.value.find((gameType) => gameType.gameTypeId === selectedGameTypeId.value),
)

const { fieldError, formError } = useFormFailure(failure, UNEXPECTED_FAILURE)

/** The API blames a rejected setting on the field its value arrived in. */
const SETTING_FIELD_PREFIX = 'gameType.selections.'

function settingError(key: string): string | undefined {
  return fieldError(`${SETTING_FIELD_PREFIX}${key}`)
}

// Reported errors describe the details as they were submitted, so the first
// correction retires them rather than leaving them under fields being fixed.
watch(
  [name, description, startDateTime, endDateTime, selectedGameTypeId, selections],
  () => clearFailure(),
  { deep: true },
)

// Switching games replaces the settings entirely, so values from the previous
// game cannot be submitted against the new one.
watch(selectedGameType, () => {
  selections.value = Object.fromEntries(
    (selectedGameType.value?.settings ?? []).map((setting) => [
      setting.key,
      setting.defaultValue,
    ]),
  )
})

watch(gameTypes, (loaded) => {
  if (!selectedGameTypeId.value && loaded.length > 0) {
    selectedGameTypeId.value = loaded[0].gameTypeId
  }
})

/**
 * Converts a `datetime-local` value to the UTC instant it denotes.
 *
 * The control yields wall-clock time in the browser's zone and no offset, so
 * the zone has to be applied rather than assumed away: appending `Z` would
 * relabel 18:00 local as 18:00 UTC and shift the event by the offset.
 */
function toUtcInstant(localValue: string): string {
  return new Date(localValue).toISOString()
}

async function submit() {
  const created = await create({
    name: name.value,
    description: description.value || undefined,
    startDateTime: toUtcInstant(startDateTime.value),
    endDateTime: toUtcInstant(endDateTime.value),
    gameType: {
      gameTypeId: selectedGameTypeId.value,
      selections: selections.value,
    },
  })

  if (!created) {
    return
  }

  await router.push({
    name: RouteNames.eventDetail,
    params: { eventId: created.eventId },
  })
}

load()
</script>

<template>
  <section class="create-event">
    <h1 class="create-event__title">Schedule an event</h1>

    <p v-if="isLoadingGameTypes">Loading games…</p>

    <template v-else-if="gameTypesError">
      <p>We could not load the games just now. Please try again.</p>

      <AppAction class="create-event__retry" @click="load">Try again</AppAction>
    </template>

    <form v-else class="create-event__form" @submit.prevent="submit">
      <p v-if="formError" class="create-event__error" role="alert">{{ formError }}</p>

      <AppField v-slot="{ id, describedBy, invalid }" label="Name" :error="fieldError('Name')">
        <input :id="id" v-model="name" :aria-describedby="describedBy" :aria-invalid="invalid" required />
      </AppField>

      <AppField
        v-slot="{ id, describedBy, invalid }"
        label="Description"
        :error="fieldError('Description')"
      >
        <textarea
          :id="id"
          v-model="description"
          rows="3"
          :aria-describedby="describedBy"
          :aria-invalid="invalid"
        />
      </AppField>

      <AppField
        v-slot="{ id, describedBy, invalid }"
        label="Starts"
        :error="fieldError('StartDateTime')"
      >
        <input
          :id="id"
          v-model="startDateTime"
          type="datetime-local"
          :aria-describedby="describedBy"
          :aria-invalid="invalid"
          required
        />
      </AppField>

      <AppField v-slot="{ id, describedBy, invalid }" label="Ends" :error="fieldError('EndDateTime')">
        <input
          :id="id"
          v-model="endDateTime"
          type="datetime-local"
          :min="startDateTime || undefined"
          :aria-describedby="describedBy"
          :aria-invalid="invalid"
          required
        />
      </AppField>

      <AppField
        v-slot="{ id, describedBy, invalid }"
        label="Game"
        :error="fieldError('gameType.gameTypeId')"
      >
        <select
          :id="id"
          v-model="selectedGameTypeId"
          :aria-describedby="describedBy"
          :aria-invalid="invalid"
        >
          <option v-for="gameType in gameTypes" :key="gameType.gameTypeId" :value="gameType.gameTypeId">
            {{ gameType.name }}
          </option>
        </select>
      </AppField>

      <fieldset v-if="selectedGameType?.settings.length" class="create-event__settings">
        <legend class="create-event__legend">{{ selectedGameType.name }} settings</legend>

        <GameTypeSettingField
          v-for="setting in selectedGameType.settings"
          :key="setting.key"
          v-model="selections[setting.key]"
          :setting="setting"
          :error="settingError(setting.key)"
        />
      </fieldset>

      <div class="create-event__actions">
        <button class="create-event__submit" type="submit" :disabled="isSaving">
          {{ isSaving ? 'Scheduling…' : 'Schedule event' }}
        </button>

        <AppAction :to="{ name: RouteNames.events }">Cancel</AppAction>
      </div>
    </form>
  </section>
</template>

<style scoped>
.create-event__title {
  margin-top: 0;
}

.create-event__form {
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 480px;
}

.create-event__settings {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin: 0;
  padding: 16px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
}

.create-event__legend {
  padding: 0 8px;
  font-weight: 600;
  color: var(--color-text-strong);
}

.create-event__error {
  margin: 0;
  padding: 12px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  color: var(--color-accent);
  background: var(--color-accent-soft);
}

.create-event__actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.create-event__submit {
  padding: 8px 16px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  font: inherit;
  color: var(--color-bg);
  background: var(--color-accent);
  cursor: pointer;
}

.create-event__submit:disabled {
  cursor: progress;
  opacity: 0.7;
}

.create-event__retry {
  margin-top: 16px;
}
</style>
