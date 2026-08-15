import { computed, shallowRef } from 'vue'

import { useAsyncRequest } from '@/composables/useAsyncRequest'
import type { RequestOptions } from '@/services/http/httpClient'
import { emptyPage, type Page } from '@/services/http/pagination'

export interface UsePagedRequestOptions<TQuery> {
  /**
   * Resolved once per walk rather than per page: a query re-read between pages
   * can move the window out from under the offset, dropping a row or repeating
   * one already loaded.
   */
  pinQuery: () => TQuery

  /** Logged with the raw failure, which never reaches a template. */
  failureMessage: string
}

/**
 * Walks a paged endpoint by offset, accumulating the pages it has loaded.
 *
 * @param fetchPage Requests one page with an abort signal and the pinned query at an offset.
 * @param options The query to pin for each walk and the message logged on failure.
 * @returns The accumulated items, the last page's metadata, and the state and actions the list renders from.
 */
export function usePagedRequest<TItem, TQuery extends { take: number }>(
  fetchPage: (
    options: RequestOptions,
    params: TQuery & { skip: number },
  ) => Promise<Page<TItem>>,
  { pinQuery, failureMessage }: UsePagedRequestOptions<TQuery>,
) {
  const items = shallowRef<TItem[]>([])
  const isFirstPage = shallowRef(true)

  let query = pinQuery()

  const {
    data,
    isLoading: isFetching,
    error,
    run,
  } = useAsyncRequest<Page<TItem>, TQuery & { skip: number }>(fetchPage, {
    initialValue: emptyPage<TItem>(query.take),
    failureMessage,
  })

  const pagination = computed(() => data.value.pagination)
  const hasMore = computed(
    () => items.value.length < pagination.value.totalCount,
  )

  const isLoading = computed(() => isFetching.value && isFirstPage.value)
  const isLoadingMore = computed(() => isFetching.value && !isFirstPage.value)
  const loadFailed = computed(() => !!error.value && isFirstPage.value)
  const loadMoreFailed = computed(() => !!error.value && !isFirstPage.value)

  async function fetchFrom(skip: number): Promise<void> {
    isFirstPage.value = skip === 0

    const page = await run({ ...query, skip })

    if (!page) {
      return
    }

    items.value = skip === 0 ? page.items : [...items.value, ...page.items]
  }

  /** Starts the walk over at the first page, against a freshly pinned query. */
  function load(): Promise<void> {
    query = pinQuery()

    return fetchFrom(0)
  }

  /** Appends the page that follows what is already loaded. */
  function loadMore(): Promise<void> {
    if (isFetching.value || !hasMore.value) {
      return Promise.resolve()
    }

    return fetchFrom(items.value.length)
  }

  return {
    items,
    pagination,
    hasMore,
    isLoading,
    isLoadingMore,
    error,
    loadFailed,
    loadMoreFailed,
    load,
    loadMore,
  }
}
