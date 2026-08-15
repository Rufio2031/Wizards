<script setup lang="ts" generic="T">
// The loading, missing, failed, and loaded states of a single fetched resource.
import AppAction from '@/components/AppAction.vue'
import AppErrorMessage from '@/components/AppErrorMessage.vue'

defineOptions({ inheritAttrs: false })

defineProps<{
  /** The resource itself; the default slot renders only once it is present. */
  data: T

  /** Takes precedence over every other state. */
  loading: boolean

  failed: boolean

  loadingText: string

  errorText: string

  /** Present only when the resource was not found, which outranks `failed`. */
  notFound?: { title: string; text: string } | null
}>()

defineEmits<{
  retry: []
}>()

defineSlots<{
  default(props: { data: NonNullable<T> }): unknown
}>()
</script>

<template>
  <p v-if="loading">{{ loadingText }}</p>

  <template v-else-if="notFound">
    <h1 class="app-async-state__title">{{ notFound.title }}</h1>

    <p class="app-async-state__message">{{ notFound.text }}</p>
  </template>

  <template v-else-if="failed">
    <AppErrorMessage :message="errorText" />

    <AppAction class="app-async-state__retry" @click="$emit('retry')">Try again</AppAction>
  </template>

  <slot v-else-if="data" :data="data" />
</template>

<style scoped>
.app-async-state__title {
  margin: 0;
}

.app-async-state__message {
  margin-top: 16px;
}

.app-async-state__retry {
  margin-top: 16px;
}
</style>
