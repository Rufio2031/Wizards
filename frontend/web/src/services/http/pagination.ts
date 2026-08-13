export interface PaginationMeta {
  skip: number
  take: number
  totalCount: number
}

export interface Page<TItem> {
  items: TItem[]
  pagination: PaginationMeta
}

export function emptyPage<TItem>(take: number): Page<TItem> {
  return { items: [], pagination: { skip: 0, take, totalCount: 0 } }
}
