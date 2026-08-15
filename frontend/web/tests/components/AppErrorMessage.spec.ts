import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import AppErrorMessage from '@/components/AppErrorMessage.vue'

describe('AppErrorMessage', () => {
  it('renders nothing at all while there is no message', () => {
    const absent = mount(AppErrorMessage)
    const empty = mount(AppErrorMessage, { props: { message: '' } })

    expect(absent.find('[role="alert"]').exists()).toBe(false)
    expect(absent.text()).toBe('')
    expect(empty.find('[role="alert"]').exists()).toBe(false)
    expect(empty.text()).toBe('')
  })

  it('announces the message as an alert', () => {
    const wrapper = mount(AppErrorMessage, {
      props: { message: 'The event could not be saved.' },
    })

    expect(wrapper.get('[role="alert"]').text()).toBe(
      'The event could not be saved.',
    )
  })
})
