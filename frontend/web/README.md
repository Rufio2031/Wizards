# Wizards web

Vue 3 + TypeScript SPA, built with Vite. All source lives in `src/`.

## Scripts

Run from `frontend/web`.

| Script | What it does |
| --- | --- |
| `npm run dev` | Vite dev server on port 5173, bound to `0.0.0.0` for containers |
| `npm run build` | Type-checks with `vue-tsc`, then builds to `dist/` |

`npm run build` is the gate before anything ships. It catches type errors the dev
server does not.

## Layout of `src/`

```
components/layout   app shell pieces (header, footer, container, wordmark)
composables         useAsyncRequest.ts, the request lifecycle feature
                    composables build on
config              app constants and the single owner of the API base path
features/<name>     one self-contained feature
layouts             page chrome the router selects per route
router              route table, route names
services/http       the HTTP seam
styles              tokens.css then base.css, imported in that order by
                    main.ts. No aggregator file
types               ambient declarations, currently the router's meta fields
views               top-level pages that belong to no feature
```

A feature owns its whole vertical slice and follows one shape:

```
features/events
  api/           the only module that knows this feature's endpoint paths
  components/    components used only by this feature
  composables/   request state and other reusable logic
  routes.ts      route records, lazily importing the feature's views
  types/         the feature's data types
  views/         pages
```

There is no feature barrel. Other features and shared code import the concrete
module they need, such as `@/features/events/routes` or
`@/features/events/types/event`.

## Import convention

- `@/` across module boundaries. A feature importing `@/services/http`, a layout
  importing `@/components/layout/AppHeader.vue`.
- Relative within a feature or within the same folder. `../types/event`,
  `./AppContainer.vue`.

`router/index.ts` imports `@/features/events/routes` directly. Importing routes
through anything broader risks pulling a view into the entry chunk and undoing
the lazy `() => import()` in `routes.ts`.

## Layout selection

Layouts are chosen by nesting, not by route meta. Each layout is a parent route
record whose `component` is the layout and whose `children` are the pages that
wear it. The layout renders `<RouterView />` where the page goes.

- `DefaultLayout` is the standard chrome: header nav, content, footer.
- `FocusedLayout` is stripped chrome for single-task mobile flows reached by QR
  code. It has no registered routes yet, by design, and gets one when the
  registration flow lands.

To add a page, add its record under the layout it belongs to and give it a
`meta.title`, which the router composes into the document title.

## HTTP seam

The API is same-origin under `/api`. nginx serves that in production and the
Vite dev proxy mirrors it in development, both stripping the prefix, so
`/api/events` reaches the API as `/events` in either environment and no feature
code changes between them.

Requests flow through four layers, each with one job.

1. `config/env.ts` is the single owner of the `/api` base path, exported as a
   frozen config.
2. `services/http/httpClient.ts` is the only place `fetch` is called: base path,
   JSON headers, caller-supplied `AbortSignal` support, and RFC 7807 problem
   details turned into an `ApiError`, as network failures are too. Aborts
   propagate unchanged so callers can tell cancelled from failed, and
   `isAbortError` lives here for the callers that need to make that call. It
   exposes `get` and `post` only, since nothing in scope edits or cancels. An
   empty body resolves to `undefined`, which the return type states, so call
   sites have to decide what an empty response means.
3. `features/<name>/api/*.ts` owns that feature's endpoint paths and response
   types, and is where an empty body is turned into that feature's own shape
   (`eventsApi.list` returns `[]`).
4. `composables/useAsyncRequest.ts` owns the request lifecycle: loading, error,
   abort on re-fetch and on scope disposal, and a newest-run-wins guard. Feature
   composables such as `features/events/composables/useEvents.ts` build on it,
   and views consume only composables.

`ApiError.message` can carry server text, so never render it. Show the view's own
copy and send the raw error to `console.error`.

## Environment

| Variable | Default | Notes |
| --- | --- | --- |
| `DEV_API_PROXY_TARGET` | `http://localhost:5208` | Dev server only. Node reads it from `process.env` in `vite.config.ts` at config time, so it never reaches client code and takes no `VITE_` prefix. Compose sets it to the API's compose DNS name. |
