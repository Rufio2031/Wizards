<script setup lang="ts">
import { computed } from 'vue'

import AppAsyncState from '@/components/AppAsyncState.vue'
import AppBackLink from '@/components/AppBackLink.vue'
import AppBadge from '@/components/AppBadge.vue'
import GameTypeSelectionList from '@/features/gameTypes/components/GameTypeSelectionList.vue'
import { useGameType } from '@/features/gameTypes/composables/useGameType'
import { RouteNames } from '@/router/routeNames'
import { formatSchedule } from '@/utils/dateTime'

import EventQrCode from '../components/EventQrCode.vue'
import EventRegistrationList from '../components/EventRegistrationList.vue'
import { useEvent } from '../composables/useEvent'
import { EVENT_COPY } from '../copy'

const props = defineProps<{
  eventId: string
}>()

const { event, isLoading, error, dataNotFound, load } = useEvent(() => props.eventId)

const { gameType, isLoading: isLoadingGameType } = useGameType(
  () => event.value?.gameType.gameTypeId,
)

const gameTypeSettings = computed(() => gameType.value?.settings ?? [])

const hasSelections = computed(
  () => Object.keys(event.value?.selections ?? {}).length > 0,
)
</script>

<template>
  <section class="event-detail">
    <AppBackLink class="event-detail__back" :to="{ name: RouteNames.events }">
      Back to events
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
      <h1 class="event-detail__title">{{ event.name }}</h1>

      <p class="event-detail__meta">
        {{ formatSchedule(event.startDateTime, event.endDateTime) }}
      </p>

      <p class="event-detail__meta">Up to {{ event.registrationLimit }} players</p>

      <p v-if="event.description" class="event-detail__description">
        {{ event.description }}
      </p>

      <AppBadge class="event-detail__game-type">
        {{ event.gameType.name }}
      </AppBadge>

      <template v-if="hasSelections && !isLoadingGameType">
        <h2 class="event-detail__settings-title">Settings</h2>

        <GameTypeSelectionList :settings="gameTypeSettings" :selections="event.selections" />
      </template>

      <h2 class="event-detail__registration-heading">Registration</h2>

      <EventQrCode class="event-detail__qr-code" :event-id="event.eventId" />

      <EventRegistrationList
        class="event-detail__registrations"
        :event-id="event.eventId"
        :registration-limit="event.registrationLimit"
      />
    </AppAsyncState>
  </section>
</template>

<style scoped>
.event-detail {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.event-detail__back {
  margin-bottom: 16px;
}

.event-detail__title {
  margin: 0;
}

.event-detail__meta {
  margin-top: 4px;
  font-size: 0.875rem;
}

.event-detail__description {
  margin-top: 24px;
  line-height: 1.6;
  color: var(--color-text-strong);
}

.event-detail__game-type {
  margin-top: 24px;
}

.event-detail__settings-title {
  margin: 24px 0 12px;
  font-size: 1.125rem;
}

.event-detail__registration-heading {
  margin: 40px 0 0;
}

.event-detail__qr-code {
  margin-top: 16px;
}

.event-detail__registrations {
  margin-top: 32px;
}
</style>
