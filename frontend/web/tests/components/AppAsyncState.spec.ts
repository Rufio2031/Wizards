import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import AppAsyncState from '@/components/AppAsyncState.vue'

const LOADING_TEXT = 'Loading the event.'
const ERROR_TEXT = 'The event could not be loaded.'

const NOT_FOUND = {
  title: 'Event not found',
  text: 'It may have been cancelled.',
}

const loaded = '<template #default="state">Loaded {{ state.data.name }}</template>'

function mountState(state: {
  data?: { name: string } | null
  loading?: boolean
  failed?: boolean
  notFound?: { title: string; text: string } | null
}) {
  return mount(AppAsyncState, {
    props: {
      data: state.data ?? null,
      loading: state.loading ?? false,
      failed: state.failed ?? false,
      notFound: state.notFound ?? null,
      loadingText: LOADING_TEXT,
      errorText: ERROR_TEXT,
    },
    slots: { default: loaded },
  })
}

describe('AppAsyncState', () => {
  it('shows only the loading copy while loading, whatever else arrived', () => {
    const wrapper = mountState({
      loading: true,
      failed: true,
      notFound: NOT_FOUND,
      data: { name: 'Wizard Night' },
    })

    expect(wrapper.text()).toContain(LOADING_TEXT)
    expect(wrapper.text()).not.toContain(NOT_FOUND.title)
    expect(wrapper.text()).not.toContain(ERROR_TEXT)
    expect(wrapper.text()).not.toContain('Loaded')
  })

  it('reports a missing resource in its own words rather than as a failure', () => {
    const wrapper = mountState({ failed: true, notFound: NOT_FOUND })

    expect(wrapper.text()).toContain(NOT_FOUND.title)
    expect(wrapper.text()).toContain(NOT_FOUND.text)
    expect(wrapper.text()).not.toContain(ERROR_TEXT)
    expect(wrapper.text()).not.toContain('Loaded')
  })

  it('offers no retry for a resource that is missing rather than unreachable', () => {
    const wrapper = mountState({ notFound: NOT_FOUND })

    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('announces a failure and asks again when the retry is taken', async () => {
    const wrapper = mountState({ failed: true })

    expect(wrapper.get('[role="alert"]').text()).toBe(ERROR_TEXT)
    expect(wrapper.text()).not.toContain('Loaded')

    await wrapper.get('button').trigger('click')

    expect(wrapper.emitted('retry')).toHaveLength(1)
  })

  it('hands the resource to the default slot once every other state has passed', () => {
    const wrapper = mountState({ data: { name: 'Wizard Night' } })

    expect(wrapper.text()).toContain('Loaded Wizard Night')
  })
})
