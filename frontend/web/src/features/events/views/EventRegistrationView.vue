<script setup lang="ts">
import { ref, watch } from 'vue'

import AppAction from '@/components/AppAction.vue'
import AppAsyncState from '@/components/AppAsyncState.vue'
import AppBackLink from '@/components/AppBackLink.vue'
import AppErrorMessage from '@/components/AppErrorMessage.vue'
import AppField from '@/components/AppField.vue'
import { useFormFailure } from '@/composables/useFormFailure'
import { RouteNames } from '@/router/routeNames'

import EventCard from '../components/EventCard.vue'
import { useEvent } from '../composables/useEvent'
import { useEventRegistration } from '../composables/useEventRegistration'
import { EVENT_COPY } from '../copy'

const UNEXPECTED_FAILURE = 'We could not register you just now. Please try again.'

const props = defineProps<{
  eventId: string
}>()

const { event, isLoading, error, dataNotFound, load } = useEvent(() => props.eventId)

const { registration, isRegistered, isSaving, failure, register } = useEventRegistration(
  () => props.eventId,
)

const { fieldError, formError, clearFieldErrors } = useFormFailure(
  failure,
  UNEXPECTED_FAILURE,
  ['name'],
)

const name = ref('')

watch(name, () => clearFieldErrors())

function submit() {
  void register({ name: name.value })
}
</script>

<template>
  <section class="event-registration">
    <AppBackLink
      class="event-registration__back"
      :to="{ name: RouteNames.eventDetail, params: { eventId } }"
    >
      Back to event
    </AppBackLink>

    <AppAsyncState
      v-slot="{ data: event }"
      :data="event"
      :loading="isLoading"
      :failed="!!error"
      :not-found="dataNotFound ? EVENT_COPY.notFound : null"
      :loading-text="EVENT_COPY.loading"
      :error-text="EVENT_COPY.error"
      @retry="load"
    >
      <h1 class="event-registration__title">
        {{ isRegistered ? 'You are registered' : 'Register' }}
      </h1>

      <EventCard class="event-registration__event" :event="event" />

      <p v-if="registration" class="event-registration__message">
        We have you down for this event as
        <strong>{{ registration.name }}</strong
        >. Show this screen at the table.
      </p>

      <form v-else class="event-registration__form" @submit.prevent="submit">
        <AppErrorMessage :message="formError" />

        <AppField v-slot="{ id, describedBy, invalid }" label="Name" :error="fieldError('name')">
          <input
            :id="id"
            v-model="name"
            autocomplete="name"
            :aria-describedby="describedBy"
            :aria-invalid="invalid"
            required
          />
        </AppField>

        <AppAction type="submit" primary :disabled="isSaving">
          {{ isSaving ? 'Registering…' : 'Register' }}
        </AppAction>
      </form>
    </AppAsyncState>
  </section>
</template>

<style scoped>
.event-registration__back {
  margin-bottom: 16px;
}

.event-registration__title {
  margin-top: 0;
}

.event-registration__message {
  margin-top: 16px;
  line-height: 1.6;
  color: var(--color-text-strong);
}

.event-registration__event {
  margin-top: 24px;
  max-width: 480px;
}

.event-registration__form {
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 480px;
  margin-top: 32px;
}
</style>
