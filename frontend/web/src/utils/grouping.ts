export interface Group<TKey extends PropertyKey, TItem> {
  key: TKey
  items: TItem[]
}

/** Groups are ordered by where their key first appears; input is never sorted. */
export function groupBy<TItem, TKey extends PropertyKey>(
  items: readonly TItem[],
  selectKey: (item: TItem) => TKey,
): Group<TKey, TItem>[] {
  const groups = new Map<TKey, Group<TKey, TItem>>()

  for (const item of items) {
    const key = selectKey(item)
    const existing = groups.get(key)

    if (existing) {
      existing.items.push(item)
    } else {
      groups.set(key, { key, items: [item] })
    }
  }

  return [...groups.values()]
}
