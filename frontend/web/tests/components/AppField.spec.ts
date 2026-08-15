import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { h } from 'vue'

import AppField from '@/components/AppField.vue'

// The consumer's half of the contract: whatever the field offers has to land on the
// control as the corresponding ARIA attribute.
const control = `
  <template #default="field">
    <input
      :id="field.id"
      :aria-describedby="field.describedBy"
      :aria-invalid="field.invalid"
    />
  </template>
`

function mountField(props: { label: string; hint?: string; error?: string }) {
  return mount(AppField, { props, slots: { default: control } })
}

/**
 * The text of every element the control is described by, in the order a screen
 * reader would announce them, failing if any named id is not on the page.
 */
function describedTexts(wrapper: ReturnType<typeof mountField>) {
  const describedBy = wrapper.get('input').attributes('aria-describedby')
  if (!describedBy) return []

  return describedBy.split(' ').map((id) => {
    const described = wrapper.find(`[id="${id}"]`)

    expect(
      described.exists(),
      `aria-describedby names "${id}", which nothing renders`,
    ).toBe(true)

    return described.text()
  })
}

describe('AppField', () => {
  it('labels the control the slot rendered', () => {
    const wrapper = mountField({ label: 'Event name' })
    const input = wrapper.get('input')

    expect(input.attributes('id')).toBeTruthy()
    expect(wrapper.get('label').text()).toBe('Event name')
    expect(wrapper.get('label').attributes('for')).toBe(input.attributes('id'))
  })

  it('describes the control by its hint alone', () => {
    const wrapper = mountField({
      label: 'Capacity',
      hint: 'Leave blank for no limit.',
    })

    expect(describedTexts(wrapper)).toEqual(['Leave blank for no limit.'])
  })

  it('describes the control by its error alone', () => {
    const wrapper = mountField({ label: 'Name', error: 'Name is required.' })

    expect(describedTexts(wrapper)).toEqual(['Name is required.'])
  })

  it('describes the control by its hint then its error', () => {
    const wrapper = mountField({
      label: 'Capacity',
      hint: 'Leave blank for no limit.',
      error: 'Capacity must be a whole number.',
    })

    expect(describedTexts(wrapper)).toEqual([
      'Leave blank for no limit.',
      'Capacity must be a whole number.',
    ])
  })

  it('leaves a control with neither hint nor error undescribed', () => {
    const wrapper = mountField({ label: 'Name' })

    expect(wrapper.get('input').attributes('aria-describedby')).toBeUndefined()
  })

  it('marks the control invalid only while it has an error', () => {
    const valid = mountField({ label: 'Name' })
    const invalid = mountField({ label: 'Name', error: 'Name is required.' })

    // Not `false`: an unmarked control has to carry no aria-invalid at all.
    expect(valid.get('input').attributes('aria-invalid')).toBeUndefined()
    expect(invalid.get('input').attributes('aria-invalid')).toBe('true')
  })

  it('gives each field on the page its own id', () => {
    const twoFields = () =>
      h('form', [
        h(AppField, { label: 'Name' }, { default: identify }),
        h(AppField, { label: 'Email' }, { default: identify }),
      ])

    const wrapper = mount(twoFields)
    const [name, email] = wrapper.findAll('input')

    expect(name.attributes('id')).toBeTruthy()
    expect(name.attributes('id')).not.toBe(email.attributes('id'))
  })
})

function identify(field: { id: string }) {
  return h('input', { id: field.id })
}
