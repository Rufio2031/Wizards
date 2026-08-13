import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /**
     * Page title, composed into the document title by the router. Optional
     * because layout records are matched too and have no title of their own.
     */
    title?: string
  }
}

export {}
