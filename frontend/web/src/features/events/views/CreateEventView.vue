<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import AppAction from '@/components/AppAction.vue'
import AppAsyncState from '@/components/AppAsyncState.vue'
import AppErrorMessage from '@/components/AppErrorMessage.vue'
import AppField from '@/components/AppField.vue'
import { useFormFailure } from '@/composables/useFormFailure'
import GameTypeSettingField from '@/features/gameTypes/components/GameTypeSettingField.vue'
import { useGameTypes } from '@/features/gameTypes/composables/useGameTypes'
import type { GameTypeTemplate } from '@/features/gameTypes/types/gameType'
import { RouteNames } from '@/router/routeNames'
import { toDateTimeLocalValue, toUtcInstant } from '@/utils/dateTime'

import { useCreateEvent } from '../composables/useCreateEvent'
import { REGISTRATION_LIMIT } from '../types/event'

const UNEXPECTED_FAILURE = 'We could not schedule the event just now. Please try again.'
const UNREADABLE_SCHEDULE = 'Please enter a start and end date and time.'

const router = useRouter()

const { gameTypes, isLoading: isLoadingGameTypes, error: gameTypesError, load } = useGameTypes()
const { isSaving, failure, create, clearFailure } = useCreateEvent()

const name = ref('')
const description = ref('')
const location = ref('')
const startDateTime = ref('')
const endDateTime = ref('')
const registrationLimit = ref(REGISTRATION_LIMIT.max)
const selectedGameTypeId = ref('')

/** Keyed by setting key, and only ever holding the currently selected game's settings. */
const selections = ref<Record<string, string>>({})

const selectedGameType = computed<GameTypeTemplate | undefined>(() =>
  gameTypes.value.find((gameType) => gameType.gameTypeId === selectedGameTypeId.value),
)

/** The API blames a rejected setting on the field its value arrived in. */
const SETTING_FIELD_PREFIX = 'gameType.selections.'

const renderedFields = computed(() => [
  'Name',
  'Description',
  'Location',
  'StartDateTime',
  'EndDateTime',
  'RegistrationLimit',
  'gameType.gameTypeId',
  ...(selectedGameType.value?.settings ?? []).map(
    (setting) => `${SETTING_FIELD_PREFIX}${setting.key}`,
  ),
])

const { fieldError, formError } = useFormFailure(failure, UNEXPECTED_FAILURE, renderedFields)

const submitError = ref('')

const bannerError = computed(() => formError.value || submitError.value)

const earliestStart = toDateTimeLocalValue(new Date())

function settingError(key: string): string | undefined {
  return fieldError(`${SETTING_FIELD_PREFIX}${key}`)
}

// Reported errors describe the details as they were submitted, so the first
// correction retires them rather than leaving them under fields being fixed.
watch(
  [
    name,
    description,
    location,
    startDateTime,
    endDateTime,
    registrationLimit,
    selectedGameTypeId,
    selections,
  ],
  () => {
    clearFailure()
    submitError.value = ''
  },
  { deep: true },
)

// Switching games replaces the settings entirely, so values from the previous
// game cannot be submitted against the new one. Watches the id rather than the
// game type, since a reload rebuilds that object without the choice changing.
watch(selectedGameTypeId, () => {
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

async function submit() {
  submitError.value = ''

  const startsAt = toUtcInstant(startDateTime.value)
  const endsAt = toUtcInstant(endDateTime.value)

  if (!startsAt || !endsAt) {
    submitError.value = UNREADABLE_SCHEDULE

    return
  }

  try {
    const created = await create({
      name: name.value,
      description: description.value || undefined,
      location: location.value,
      startDateTime: startsAt,
      endDateTime: endsAt,
      registrationLimit: registrationLimit.value,
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
  } catch {
    submitError.value = UNEXPECTED_FAILURE
  }
}

load()
</script>

<template>
  <section class="create-event">
    <h1 class="create-event__title">Schedule an event</h1>

    <AppAsyncState
      v-slot="{ data: gameTypes }"
      :data="gameTypes"
      :loading="isLoadingGameTypes"
      :failed="!!gameTypesError"
      loading-text="Loading games…"
      error-text="We could not load the games just now. Please try again."
      @retry="load"
    >
      <form class="create-event__form" @submit.prevent="submit">
        <AppErrorMessage :message="bannerError" />

        <AppField v-slot="{ id, describedBy, invalid }" label="Name" :error="fieldError('Name')">
          <input
            :id="id"
            v-model="name"
            :aria-describedby="describedBy"
            :aria-invalid="invalid"
            required
          />
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
          label="Location"
          hint="Stated on the event's calendar invite."
          :error="fieldError('Location')"
        >
          <input
            :id="id"
            v-model="location"
            :aria-describedby="describedBy"
            :aria-invalid="invalid"
            required
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
            :min="earliestStart"
            :aria-describedby="describedBy"
            :aria-invalid="invalid"
            required
          />
        </AppField>

        <AppField
          v-slot="{ id, describedBy, invalid }"
          label="Ends"
          :error="fieldError('EndDateTime')"
        >
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
          label="Player limit"
          :hint="`Between ${REGISTRATION_LIMIT.min} and ${REGISTRATION_LIMIT.max} players.`"
          :error="fieldError('RegistrationLimit')"
        >
          <input
            :id="id"
            v-model.number="registrationLimit"
            class="create-event__limit"
            type="number"
            inputmode="numeric"
            :min="REGISTRATION_LIMIT.min"
            :max="REGISTRATION_LIMIT.max"
            step="1"
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
            <option
              v-for="gameType in gameTypes"
              :key="gameType.gameTypeId"
              :value="gameType.gameTypeId"
            >
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
          <AppAction type="submit" primary :disabled="isSaving">
            {{ isSaving ? 'Scheduling…' : 'Schedule event' }}
          </AppAction>

          <AppAction :to="{ name: RouteNames.events }">Cancel</AppAction>
        </div>
      </form>
    </AppAsyncState>
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

.create-event__limit {
  width: 5.5rem;
  text-align: right;
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

.create-event__actions {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
