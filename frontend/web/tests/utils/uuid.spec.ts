import { afterEach, describe, expect, it, vi } from 'vitest'

import { createUuid } from '@/utils/uuid'

const UUID_V4 =
  /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/

/** A phone on a plain-HTTP LAN address: no `randomUUID`, `getRandomValues` only. */
function withoutSecureContext(
  getRandomValues: (bytes: Uint8Array<ArrayBuffer>) => Uint8Array<ArrayBuffer>,
) {
  vi.stubGlobal('crypto', { getRandomValues })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('createUuid without crypto.randomUUID', () => {
  it('lays the random bytes out as a v4, stamping the version and variant', () => {
    withoutSecureContext((bytes) => bytes.map((_, index) => index))

    expect(createUuid()).toBe('00010203-0405-4607-8809-0a0b0c0d0e0f')
  })

  it('hands out a different key on every call', () => {
    const realCrypto = globalThis.crypto

    withoutSecureContext((bytes) => realCrypto.getRandomValues(bytes))

    const first = createUuid()
    const second = createUuid()

    expect(first).toMatch(UUID_V4)
    expect(second).toMatch(UUID_V4)
    expect(second).not.toBe(first)
  })
})
