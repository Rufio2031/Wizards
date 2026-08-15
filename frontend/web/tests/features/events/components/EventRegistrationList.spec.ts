import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import EventRegistrationList from '@/features/events/components/EventRegistrationList.vue'
import { eventsApi } from '@/features/events/api/eventsApi'

vi.mock('@/features/events/api/eventsApi', () => ({
  eventsApi: { listRegistrations: vi.fn() },
}))

const listRegistrations = vi.mocked(eventsApi.listRegistrations)

const REGISTRATION_LIMIT = 8

// Any "n of 8" the heading might claim before the roster is actually known.
const COUNT_CLAIM = /\d+\s*of\s*8/

function mountList() {
  return mount(EventRegistrationList, {
    props: { eventId: 'evt-1', registrationLimit: REGISTRATION_LIMIT },
  })
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.restoreAllMocks()
  listRegistrations.mockReset()
})

describe('EventRegistrationList', () => {
  it('claims no registered count while the roster is still loading', () => {
    listRegistrations.mockReturnValue(new Promise(() => {}))

    const wrapper = mountList()

    expect(wrapper.text()).toContain('Loading registrations')
    expect(wrapper.text()).not.toMatch(COUNT_CLAIM)
  })

  it('claims no registered count when the roster could not be loaded', async () => {
    listRegistrations.mockRejectedValue(new Error('gateway is asleep'))

    const wrapper = mountList()

    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'could not load the registrations',
    )
    expect(wrapper.text()).not.toMatch(COUNT_CLAIM)
    expect(wrapper.find('ol').exists()).toBe(false)
  })

  it('reads as nobody registered, counted against the limit, when the roster is empty', async () => {
    listRegistrations.mockResolvedValue([])

    const wrapper = mountList()

    await flushPromises()

    expect(wrapper.text()).toContain('Nobody has registered yet.')
    expect(wrapper.text()).toContain('0 of 8')
    expect(wrapper.find('ol').exists()).toBe(false)
  })

  it('lists every registered player in the order returned, counted against the limit', async () => {
    listRegistrations.mockResolvedValue([
      { name: 'Merlin' },
      { name: 'Morgana' },
      { name: 'Nicolas' },
    ])

    const wrapper = mountList()

    await flushPromises()

    expect(wrapper.findAll('li').map((item) => item.text())).toEqual([
      'Merlin',
      'Morgana',
      'Nicolas',
    ])
    expect(wrapper.text()).toContain('3 of 8')
    expect(wrapper.text()).not.toContain('Nobody has registered yet.')
  })
})
