<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { QrcodeSvg } from 'qrcode.vue'

import { RouteNames } from '@/router/routeNames'

/** Rendered edge length in px, quiet zone included. */
const QR_SIZE_PX = 208

const props = defineProps<{
  eventId: string
}>()

const router = useRouter()

const registrationRoute = computed(() => ({
  name: RouteNames.eventRegistration,
  params: { eventId: props.eventId },
}))

// Resolved through the router so the encoded path follows the route table. A
// scanned code leaves the app, so it carries the origin the RouterLink omits.
const registrationUrl = computed(
  () => window.location.origin + router.resolve(registrationRoute.value).href,
)
</script>

<template>
  <div class="event-qr-code">
    <RouterLink class="event-qr-code__link" :to="registrationRoute">
      <QrcodeSvg
        class="event-qr-code__image"
        :value="registrationUrl"
        :size="QR_SIZE_PX"
        :margin="4"
        level="M"
        aria-hidden="true"
      />

      <span class="event-qr-code__label">Open the registration page</span>
    </RouterLink>

    <!-- The fallback for any phone whose camera will not cooperate: type it. -->
    <p class="event-qr-code__url">{{ registrationUrl }}</p>
  </div>
</template>

<style scoped>
.event-qr-code {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  max-width: 100%;
}

.event-qr-code__link {
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 12px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-surface);
  color: var(--color-accent);
  font-size: 0.875rem;
  text-decoration: none;
}

.event-qr-code__link:hover {
  box-shadow: var(--shadow-sm);
}

.event-qr-code__link:hover .event-qr-code__label {
  text-decoration: underline;
}

.event-qr-code__image {
  display: block;
  /* The white ground belongs to the code, so it takes the same radius as the card. */
  border-radius: 4px;
  max-width: 100%;
  height: auto;
}

.event-qr-code__url {
  max-width: 100%;
  font-family: var(--font-mono);
  font-size: 0.75rem;
  /* A URL is one unbreakable token, so it would otherwise widen the page. */
  overflow-wrap: anywhere;
}
</style>
