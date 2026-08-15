function toHex(bytes: Uint8Array): string {
  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('')
}

/**
 * Creates a random UUID v4.
 *
 * `crypto.randomUUID` only exists in a secure context, which a phone opening a
 * plain-HTTP QR link to a LAN address is not. `crypto.getRandomValues` carries
 * no such gate and is just as strong, so it fills in there.
 */
export function createUuid(): string {
  if (typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }

  const bytes = crypto.getRandomValues(new Uint8Array(16))

  bytes[6] = (bytes[6] & 0x0f) | 0x40
  bytes[8] = (bytes[8] & 0x3f) | 0x80

  const hex = toHex(bytes)

  return [
    hex.slice(0, 8),
    hex.slice(8, 12),
    hex.slice(12, 16),
    hex.slice(16, 20),
    hex.slice(20),
  ].join('-')
}
