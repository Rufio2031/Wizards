import { mount, type DOMWrapper } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import GameTypeSettingField from '@/features/gameTypes/components/GameTypeSettingField.vue'
import type { GameTypeSetting } from '@/features/gameTypes/types/gameType'

function settingOf(overrides: Partial<GameTypeSetting> = {}): GameTypeSetting {
  return {
    key: 'rounds',
    label: 'Rounds',
    type: 'int',
    defaultValue: '3',
    options: [],
    ...overrides,
  }
}

function mountField(setting: GameTypeSetting, modelValue: string) {
  return mount(GameTypeSettingField, { props: { setting, modelValue } })
}

/**
 * Types into the box and commits it, as leaving the field does. The value is
 * written straight to the element so the commit is asserted on its own, without
 * the keystroke's echo in the emissions.
 */
async function commit(box: Omit<DOMWrapper<Element>, 'exists'>, entered: string) {
  ;(box.element as HTMLInputElement).value = entered

  await box.trigger('change')
}

function lastEmit(wrapper: ReturnType<typeof mountField>) {
  return wrapper.emitted('update:modelValue')?.at(-1)
}

const BOUNDED = settingOf({ minValue: 1, maxValue: 10 })

describe('GameTypeSettingField', () => {
  it('presents a bool as a toggle following the value, and emits the new state as a string', async () => {
    const wrapper = mountField(
      settingOf({ key: 'ranked', label: 'Ranked play', type: 'bool' }),
      'false',
    )

    const toggle = wrapper.get('input[type="checkbox"]')

    expect((toggle.element as HTMLInputElement).checked).toBe(false)

    await toggle.setValue(true)

    expect(lastEmit(wrapper)).toEqual(['true'])

    await wrapper.setProps({ modelValue: 'true' })

    expect((toggle.element as HTMLInputElement).checked).toBe(true)

    await toggle.setValue(false)

    expect(lastEmit(wrapper)).toEqual(['false'])
  })

  it('presents an enum as exactly the options declared, and emits the one chosen', async () => {
    const wrapper = mountField(
      settingOf({
        key: 'format',
        label: 'Format',
        type: 'enum',
        options: ['Standard', 'Draft', 'Sealed'],
        defaultValue: 'Standard',
      }),
      'Draft',
    )

    const select = wrapper.get('select')

    expect(select.findAll('option').map((option) => option.text())).toEqual([
      'Standard',
      'Draft',
      'Sealed',
    ])
    expect((select.element as HTMLSelectElement).value).toBe('Draft')

    await select.setValue('Sealed')

    expect(lastEmit(wrapper)).toEqual(['Sealed'])
  })

  it('keeps an enum value the game no longer offers visible, selected, unavailable, and flagged', () => {
    const wrapper = mountField(
      settingOf({
        key: 'format',
        label: 'Format',
        type: 'enum',
        options: ['Standard', 'Draft'],
        defaultValue: 'Standard',
      }),
      'Commander',
    )

    const select = wrapper.get('select')
    const options = select.findAll('option')

    expect(options.map((option) => option.text())).toEqual([
      'Commander (unavailable)',
      'Standard',
      'Draft',
    ])
    expect((select.element as HTMLSelectElement).value).toBe('Commander')
    expect((options[0].element as HTMLOptionElement).disabled).toBe(true)
    expect(wrapper.text()).toContain('no longer offered')
  })

  it('presents an int bounded at both ends as a slider and a number entry reading the same value', () => {
    const wrapper = mountField(BOUNDED, '4')

    const slider = wrapper.get('input[type="range"]')
    const number = wrapper.get('input[type="number"]')

    expect((slider.element as HTMLInputElement).value).toBe('4')
    expect((number.element as HTMLInputElement).value).toBe('4')
    expect(slider.attributes('min')).toBe('1')
    expect(slider.attributes('max')).toBe('10')
  })

  it('presents an int bounded on one side or neither as a number entry alone', () => {
    const oneSided = mountField(settingOf({ minValue: 1 }), '4')
    const unbounded = mountField(settingOf(), '4')

    for (const wrapper of [oneSided, unbounded]) {
      expect(wrapper.find('input[type="range"]').exists()).toBe(false)
      expect(wrapper.find('input[type="number"]').exists()).toBe(true)
    }
  })

  it('presents an int whose bounds meet as a single value that cannot be changed', () => {
    const wrapper = mountField(settingOf({ minValue: 4, maxValue: 4 }), '4')

    const number = wrapper.get('input[type="number"]')

    expect(wrapper.find('input[type="range"]').exists()).toBe(false)
    expect((number.element as HTMLInputElement).value).toBe('4')
    expect((number.element as HTMLInputElement).readOnly).toBe(true)
  })

  it('settles a committed number outside the bounds onto the bound it passed', async () => {
    const above = mountField(BOUNDED, '4')

    await commit(above.get('input[type="number"]'), '99')

    expect(lastEmit(above)).toEqual(['10'])

    const below = mountField(BOUNDED, '4')

    await commit(below.get('input[type="number"]'), '-4')

    expect(lastEmit(below)).toEqual(['1'])
  })

  it('settles a committed decimal onto a whole number', async () => {
    const wrapper = mountField(BOUNDED, '4')

    await commit(wrapper.get('input[type="number"]'), '7.5')

    expect(lastEmit(wrapper)).toEqual(['8'])
  })

  it('settles a committed decimal that rounds past a bound onto that bound', async () => {
    const wrapper = mountField(BOUNDED, '4')

    await commit(wrapper.get('input[type="number"]'), '10.6')

    expect(lastEmit(wrapper)).toEqual(['10'])
  })

  it('leaves a committed number inside the bounds exactly as typed', async () => {
    const wrapper = mountField(BOUNDED, '4')
    const number = wrapper.get('input[type="number"]')

    await commit(number, '7')

    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
    expect((number.element as HTMLInputElement).value).toBe('7')
  })

  it('leaves an empty or unreadable entry alone rather than substituting a bound', async () => {
    const emptied = mountField(BOUNDED, '4')

    await commit(emptied.get('input[type="number"]'), '')

    expect(emptied.emitted('update:modelValue')).toBeUndefined()

    // A number input hands over an empty string for anything it cannot read, so
    // this is what "soon" typed into the box actually commits as.
    const gibberish = mountField(BOUNDED, '4')
    const box = gibberish.get('input[type="number"]')

    await commit(box, 'soon')

    expect(gibberish.emitted('update:modelValue')).toBeUndefined()
    expect((box.element as HTMLInputElement).value).not.toBe('1')
  })
})
