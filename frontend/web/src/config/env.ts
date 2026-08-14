interface AppConfig {
  /**
   * Same-origin path every API request is prefixed with. nginx in production
   * and the Vite dev proxy both strip it before forwarding to the API.
   */
  readonly apiBasePath: string
}

/**
 * The single owner of the API base path, kept as the one module that would
 * read `import.meta.env` if any value needed to vary by environment.
 */
export const env: AppConfig = Object.freeze({
  apiBasePath: '/api',
})
