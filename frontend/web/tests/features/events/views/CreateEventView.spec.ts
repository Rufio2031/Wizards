import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, type Component } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'

import { eventsApi } from '@/features/events/api/eventsApi'
import CreateEventView from '@/features/events/views/CreateEventView.vue'
import type { GameEvent } from '@/features/events/types/event'
import { gameTypesApi } from '@/features/gameTypes/api/gameTypesApi'
import type { GameTypeTemplate } from '@/features/gameTypes/types/gameType'
import { RouteNames } from '@/router/routeNames'
import { ApiError } from '@/services/http/ApiError'

vi.mock('@/features/events/api/eventsApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/events/api/eventsApi')>()

  return { eventsApi: { ...actual.eventsApi, create: vi.fn() } }
})

vi.mock('@/features/gameTypes/api/gameTypesApi', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('@/features/gameTypes/api/gameTypesApi')>()

  return { gameTypesApi: { ...actual.gameTypesApi, list: vi.fn() } }
})

const arcana: GameTypeTemplate = {
  gameTypeId: 'gt-arcana',
  name: 'Arcana',
  settings: [
    {
      key: 'rounds',
      label: 'Rounds',
      type: 'int',
      minValue: 1,
      maxValue: 9,
      defaultValue: '3',
      options: [],
    },
  ],
}

const runes: GameTypeTemplate = {
  gameTypeId: 'gt-runes',
  name: 'Runes',
  settings: [
    {
      key: 'deck',
      label: 'Deck',
      type: 'enum',
      defaultValue: 'expert',
      options: ['starter', 'expert'],
    },
  ],
}

const created: GameEvent = {
  eventId: 'evt-1',
  name: 'Summoning 101',
  location: 'The Tower',
  startDateTime: '2026-03-14T13:30:00.000Z',
  endDateTime: '2026-03-14T16:30:00.000Z',
  registrationLimit: 30,
  gameType: { gameTypeId: 'gt-arcana', name: 'Arcana' },
  selections: { rounds: '3' },
}

const blank = { render: () => null }

function testRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/events', name: RouteNames.events, component: blank },
      {
        path: '/events/:eventId',
        name: RouteNames.eventDetail,
        component: blank,
      },
    ],
  })
}

// The real AppAsyncState only offers its retry once a load has failed, so the
// games list cannot be reloaded from the rendered form. This stands in for it,
// rendering the form and offering the same retry at any time.
const ReloadableAsyncState = defineComponent({
  inheritAttrs: false,
  props: { data: { type: Array, required: true } },
  emits: ['retry'],
  setup(props, { slots, emit }) {
    return () =>
      h('div', [
        h(
          'button',
          {
            type: 'button',
            'data-test': 'reload',
            onClick: () => emit('retry'),
          },
          'Reload',
        ),
        slots.default?.({ data: props.data }),
      ])
  },
})

async function mountView(stubs?: Record<string, Component>) {
  const router = testRouter()

  await router.push('/events')
  await router.isReady()

  const wrapper = mount(CreateEventView, {
    global: { plugins: [router], stubs },
  })

  await flushPromises()

  return { wrapper, router }
}

/** The control a label names, whichever input the field chose to render. */
function control(wrapper: VueWrapper, label: string) {
  const found = wrapper.findAll('label').find((it) => it.text() === label)

  if (!found) {
    throw new Error(`No field labelled "${label}" is rendered.`)
  }

  return wrapper.get(`#${found.attributes('for')}`)
}

/** What a screen reader announces with the control, hints and errors alike. */
function descriptionsOf(wrapper: VueWrapper, label: string): string[] {
  const describedBy = control(wrapper, label).attributes('aria-describedby')

  return (describedBy ?? '')
    .split(' ')
    .filter(Boolean)
    .map((id) => wrapper.get(`#${id}`).text())
}

async function fillDetails(wrapper: VueWrapper) {
  await control(wrapper, 'Name').setValue('Summoning 101')
  await control(wrapper, 'Location').setValue('The Tower')
  await control(wrapper, 'Starts').setValue('2026-03-14T09:30')
  await control(wrapper, 'Ends').setValue('2026-03-14T12:30')
}

async function submit(wrapper: VueWrapper) {
  await wrapper.get('form').trigger('submit')
  await flushPromises()
}

function submittedRequest() {
  const [request] = vi.mocked(eventsApi.create).mock.calls[0]

  return request
}

beforeEach(() => {
  vi.resetAllMocks()
  vi.stubEnv('TZ', 'America/New_York')
  vi.mocked(gameTypesApi.list).mockResolvedValue([arcana, runes])
  vi.mocked(eventsApi.create).mockResolvedValue(created)
})

afterEach(() => {
  vi.unstubAllEnvs()
  vi.restoreAllMocks()
})

describe('CreateEventView', () => {
  it('selects the first game once the games load', async () => {
    const { wrapper } = await mountView()

    expect((control(wrapper, 'Game').element as HTMLSelectElement).value).toBe(
      'gt-arcana',
    )
  })

  it('prefills a chosen game with that game defaults', async () => {
    const { wrapper } = await mountView()

    await control(wrapper, 'Game').setValue('gt-runes')

    expect((control(wrapper, 'Deck').element as HTMLSelectElement).value).toBe(
      'expert',
    )
  })

  it('leaves the previous game settings out once the game changes', async () => {
    const { wrapper } = await mountView()

    await control(wrapper, 'Rounds').setValue('7')
    await control(wrapper, 'Game').setValue('gt-runes')
    await fillDetails(wrapper)
    await submit(wrapper)

    expect(submittedRequest().gameType).toEqual({
      gameTypeId: 'gt-runes',
      selections: { deck: 'expert' },
    })
  })

  it('keeps the chosen game and its entered settings when the games list reloads', async () => {
    vi.mocked(gameTypesApi.list)
      .mockResolvedValueOnce([arcana, runes])
      .mockResolvedValueOnce([{ ...arcana }, { ...runes, name: 'Runes II' }])

    const { wrapper } = await mountView({ AppAsyncState: ReloadableAsyncState })

    await control(wrapper, 'Game').setValue('gt-runes')
    await control(wrapper, 'Deck').setValue('starter')

    await wrapper.get('[data-test="reload"]').trigger('click')
    await flushPromises()

    // The renamed game proves the reloaded list is the one now rendered.
    expect(control(wrapper, 'Game').text()).toContain('Runes II')
    expect((control(wrapper, 'Game').element as HTMLSelectElement).value).toBe(
      'gt-runes',
    )
    expect((control(wrapper, 'Deck').element as HTMLSelectElement).value).toBe(
      'starter',
    )
  })

  it('sends the entered local times as UTC instants', async () => {
    const { wrapper } = await mountView()

    await fillDetails(wrapper)
    await submit(wrapper)

    // 9:30am on March 14 2026 in New York, which is 4 hours behind UTC.
    expect(submittedRequest().startDateTime).toBe('2026-03-14T13:30:00.000Z')
    expect(submittedRequest().endDateTime).toBe('2026-03-14T16:30:00.000Z')
  })

  it('sends no description when none was entered', async () => {
    const { wrapper } = await mountView()

    await fillDetails(wrapper)
    await submit(wrapper)

    expect(submittedRequest().description).toBeUndefined()
  })

  it('opens the new event once it is created', async () => {
    const { wrapper, router } = await mountView()

    await fillDetails(wrapper)
    await submit(wrapper)

    expect(router.currentRoute.value.path).toBe('/events/evt-1')
  })

  it('reports a schedule it cannot read instead of submitting', async () => {
    const { wrapper, router } = await mountView()

    await control(wrapper, 'Name').setValue('Summoning 101')
    await control(wrapper, 'Location').setValue('The Tower')
    await control(wrapper, 'Starts').setValue('the ides of March')
    await control(wrapper, 'Ends').setValue('2026-03-14T12:30')
    await submit(wrapper)

    expect(wrapper.get('[role="alert"]').text()).toBe(
      'Please enter a start and end date and time.',
    )
    expect(router.currentRoute.value.path).toBe('/events')
  })

  it('shows a rejected setting against that setting rather than in the banner', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.mocked(eventsApi.create).mockRejectedValue(
      new ApiError(400, {
        title: 'One or more validation errors occurred.',
        errors: { 'gameType.selections.rounds': ['Rounds must be at most 5.'] },
      }),
    )

    const { wrapper } = await mountView()

    await control(wrapper, 'Rounds').setValue('7')
    await fillDetails(wrapper)
    await submit(wrapper)

    expect(descriptionsOf(wrapper, 'Rounds')).toContain(
      'Rounds must be at most 5.',
    )
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })
})
