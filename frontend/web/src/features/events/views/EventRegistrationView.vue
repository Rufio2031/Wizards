<script setup lang="ts">
import { ref, watch } from 'vue'

import AppAction from '@/components/AppAction.vue'
import AppField from '@/components/AppField.vue'
import { useFormFailure } from '@/composables/useFormFailure'

import { useEventRegistration } from '../composables/useEventRegistration'

const UNEXPECTED_FAILURE = 'We could not register you just now. Please try again.'

const props = defineProps<{
  eventId: string
}>()

const { isRegistered, isSaving, failure, register, clearFailure } =
  useEventRegistration(() => props.eventId)

const { fieldError, formError } = useFormFailure(failure, UNEXPECTED_FAILURE)

const name = ref('')

watch(name, () => clearFailure())

function submit() {
  void register({ name: name.value })
}
</script>

<template>
  <section class="event-registration">
    <template v-if="isRegistered">
      <h1 class="event-registration__title">You are registered</h1>

      <p class="event-registration__message">
        We have your name down for this event. Show this screen at the table.
      </p>
    </template>

    <template v-else>
      <h1 class="event-registration__title">Register</h1>

      <form class="event-registration__form" @submit.prevent="submit">
        <p v-if="formError" class="event-registration__error" role="alert">
          {{ formError }}
        </p>

        <AppField v-slot="{ id, describedBy, invalid }" label="Name" :error="fieldError('Name')">
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
    </template>
  </section>
</template>

<style scoped>
.event-registration__title {
  margin-top: 0;
}

.event-registration__message {
  margin-top: 16px;
  line-height: 1.6;
  color: var(--color-text-strong);
}

.event-registration__form {
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 480px;
}

.event-registration__error {
  margin: 0;
  padding: 12px;
  border: 1px solid var(--color-accent-border);
  border-radius: 6px;
  color: var(--color-accent);
  background: var(--color-accent-soft);
}

</style>
