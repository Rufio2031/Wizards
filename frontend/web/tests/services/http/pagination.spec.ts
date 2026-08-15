import { describe, expect, it } from 'vitest'

import { emptyPage } from '@/services/http/pagination'

describe('emptyPage', () => {
  it('describes a first page with nothing on it, at the size the caller asked for', () => {
    expect(emptyPage(20)).toEqual({
      items: [],
      pagination: { skip: 0, take: 20, totalCount: 0 },
    })
  })
})
