/** RFC 7807 problem detail body, as returned by the API on failures. */
export interface ProblemDetails {
  type?: string
  title?: string
  detail?: string
  status?: number
}

/** A failed API call, with whatever problem detail the API supplied. */
export class ApiError extends Error {
  readonly status: number
  readonly type?: string
  readonly title?: string
  readonly detail?: string

  constructor(
    status: number,
    problem: ProblemDetails = {},
    options?: ErrorOptions,
  ) {
    super(
      problem.detail ?? problem.title ?? `Request failed with status ${status}.`,
      options,
    )

    this.name = 'ApiError'
    this.status = status
    this.type = problem.type
    this.title = problem.title
    this.detail = problem.detail
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}
