import { mount } from '@vue/test-utils'
import { QrcodeSvg } from 'qrcode.vue'
import { describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { h } from 'vue'

import EventQrCode from '@/features/events/components/EventQrCode.vue'
import { eventsRoutes } from '@/features/events/routes'

// The real route table, mounted under the layout path it lives under in the app,
// so the encoded path is the one a scanner would actually land on.
function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        component: { render: () => h('div') },
        children: eventsRoutes,
      },
    ],
  })
}

describe('EventQrCode', () => {
  it('encodes an absolute URL to the event registration page, and shows the same URL', async () => {
    const router = createTestRouter()

    await router.push('/')
    await router.isReady()

    const wrapper = mount(EventQrCode, {
      props: { eventId: 'evt-1' },
      global: { plugins: [router] },
    })

    const encoded = wrapper.getComponent(QrcodeSvg).props('value')

    expect(encoded).toBe(`${window.location.origin}/events/evt-1/register`)
    expect(encoded).toMatch(/^https?:\/\/[^/]+\/events\/evt-1\/register$/)
    expect(wrapper.text()).toContain(encoded)
  })
})
