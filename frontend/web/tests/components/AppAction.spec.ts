import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'

import AppAction from '@/components/AppAction.vue'

const blank = { render: () => null }

function testRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: blank },
      { path: '/events/:id', component: blank },
    ],
  })
}

describe('AppAction', () => {
  it('navigates as a link when given a destination', async () => {
    const router = testRouter()
    await router.push('/')
    await router.isReady()

    const wrapper = mount(AppAction, {
      props: { to: '/events/42' },
      slots: { default: 'View event' },
      global: { plugins: [router] },
    })

    const link = wrapper.get('a')

    expect(link.attributes('href')).toBe('/events/42')
    expect(link.text()).toBe('View event')
  })

  it('acts as a button that runs the handler it was given when it has no destination', async () => {
    const onClick = vi.fn()

    const wrapper = mount(AppAction, {
      attrs: { onClick },
      slots: { default: 'Try again' },
    })

    expect(wrapper.find('a').exists()).toBe(false)
    await wrapper.get('button').trigger('click')

    expect(onClick).toHaveBeenCalledTimes(1)
  })

  it('leaves a form alone unless it is the submit control', () => {
    const acts = mount(AppAction)
    const submits = mount(AppAction, { props: { type: 'submit' } })

    expect(acts.get('button').attributes('type')).toBe('button')
    expect(submits.get('button').attributes('type')).toBe('submit')
  })
})
