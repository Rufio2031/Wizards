# Wizards

This is an ASP.NET Core API application backing a Vue 3 web app built with Vite. The API is designed with the Clean Architecture design pattern and is supported by SQLite for data.

## Running Locally

Requires Docker with Compose v2 (Docker Desktop, or Docker Engine with the Compose plugin on Linux).

### Docker

From the repo root:

```bash
docker compose up --build
```

- Frontend: http://localhost:5209
- API: http://localhost:5208

The SQLite database is created, migrated, and seeded on API startup. It is not persisted outside the container, so every start gives a freshly seeded database.

<details>
<summary><strong>Without Docker</strong></summary>

Requires the .NET 10 SDK and Node.js 24.

Run each command in its own terminal from the repo root.

API:

```bash
dotnet run --project backend/api/WizardsApi/Wizards.Api --launch-profile http
```

Frontend:

```bash
cd frontend/web
npm ci
npm run dev
```

- Frontend: http://localhost:5173
- API: http://localhost:5208
- OpenAPI document: http://localhost:5208/openapi/v1.json

The Vite dev server proxies `/api` to http://localhost:5208 by default, so no environment variables are required. SQLite writes to `backend/api/WizardsApi/Wizards.Api/wizards.db`. Delete that file to reseed.

</details>

## Design Write-Up

### How did you determine and enforce how many people can attend an event? Where does capacity live, and what happens under concurrent registrations for the last seat?

An Event can carry a different capacity depending on venue size or available personnel. So there needed to be a way for an Event to decide its capacity separate from a global limit or from other Events. Therefore, an Event has a configurable registration limit, which is hard capped in the domain rules at 30; the provided max value. This max registration limit is stored directly on the Event record itself, as it is directly tied to that entity.

Actual user event registration is stored in a one-to-many table, linking a user name to the Event that they signed up for. When a user submits registration for an Event, we can check the current count of registrations and see where that stacks up against the Event's registration limit, which we do in the business logic. However, this doesn't prevent multiple threads or users from competing for that last spot at the same millisecond.

Where things must get in line is at the database layer. SQLite serializes writers, so two registrations cannot commit at the same instant. One is written before the other, and the second sees the first. Because of that we can have final assurance on the Event's registration at the time of writing to the database. The EventRegistration table contains a trigger that fires before each insert, and in that trigger it runs a quick query against the Events table to fetch the specific Event's registration limit. Even if the second request passed application layer validation, if it lost the race to the database, this trigger would compare the now-full registration list against the registration limit and find that the user is too late. The registration is full for that Event. The request fails and the user is notified the event is full.

Each registration carries a client-supplied idempotency key, unique per event. A retried or double-submitted request reuses its key and returns the original registration rather than consuming a second seat.

### How does your template system work, and what would adding a 4th game (or a non-card game) require?

The GameType templates are essentially viewed as a catalog of game types that are, for the sake of this project, considered immutable. Each game type stores the name of the game type, and the available settings specific to that game type.

On Event creation, the user can select a GameType from this "catalog". When selecting a specific game type, the user is shown the specific settings that the game type has configured, allowing the user to select their desired values for their game type. For example, Magic exposes a format (Standard, Modern, Pioneer, Commander, Draft), a deck size bounded between 40 and 250, and a minimum player count to start. Yu-Gi-Oh! exposes the same three keys with its own formats and bounds, while Catan exposes a largely different set of keys and bounds.

Configuration of the GameType settings uses an Entity-Attribute-Value design, allowing the game type to store the key, or name of the setting, the data type, and value settings which define how the game type setting option is presented to the user as a configurable field on the Event. This allows multiple game types to be configured without interfering with the data of others, while still allowing validation and shaping the game type the way the Event creator would like. Each Event stores the game type setting selection in a separate table. At that point, the selections, which were validated and guided around the GameType Settings bounds for that game type, are bound to the Event which are displayed to the end user.

Adding a 4th game would require adding a new record to the GameType table. It would only need a collection of settings to go with it. Each setting consists of:
```
{
    "key": string,
    "label": string,
    "description": string?,
    "type": "int" | "bool" | "enum",
    "minValue": int?,
    "maxValue": int?,
    "defaultValue": string,
    "options": string[]       // i.e. ["Standard", "Modern", "Pioneer", "Commander", "Draft"]
}
```

Once in the catalog, it's ready for an Event to select it and customize the event's game type selection.

Catan is already that 4th game. It ships seeded alongside the three card games, and it is not a card game at all, so it carries no deck size and exposes its own keys and bounds instead. Adding it required no change to Event, EventsService, the controllers, the DTOs, or the schema. Outside of tests, the only place a game is named anywhere in the backend is the seed data.

### What did you deliberately cut or fake to stay in the timebox, and what would you build next?

A few things I cut out:
- Current registration count on event cards.
- Faked a bit of caching for the GameType catalog. The endpoints are using HTTP response caching.
- Cleanup and performance pass
    - Additional attention to frontend component architecture.
    - Request/Response performance and query optimization.


What I would do next:
From a technical perspective, the thing that I would consider building next would be logging and observability. Something to be able to visualize requests and errors. I'd be interested in seeing the call traces, hit counts on the endpoints, the response times of those requests, and better visibility on error messages. This would help us identify common request flows or poor performing requests to inform us if we need to give some endpoints or queries some more attention.

After that:
- Caching / Rate Limiting
    - GameTypes seems like a response that could be cached heavily, assuming it doesn't change too often.
- Actual location coordinates on Event.
- User or Profile model
    - Associate a user as an Event owner.
    - User can see their Registered Events, or Owned Events.
- Edit Events
- Additional testing
    - e2e spec test library
    - Integration tests

## AI Usage Note

I used Claude Code in a multi-agent orchestration setup, delegating tasks to domain-specific agents, with a workflow to carry a task end to end. It got me up and running with project initialization and file scaffolding. It assisted with database migrations and the data seeder that runs at project start, and played a big part in building the unit test suite and, to a fault, the code documentation. I used Claude Cowork briefly as a sounding board on the GameType template design.

One example of something I had to fix was that the domain model validations were throwing generic `ArgumentException`s, which bubbled up as 500s. This disguised bad request input as a server error. A user hitting a real validation rule, such as the event registration being full, was being told that the server was broken rather than explain what they did wrong. Domain rules now throw a `DomainException` carrying the broken rule's message and the key it applies to, which the API maps to a 400 with that message intact.

A few others worth noting:
- `GetEvents` fetched every event at once with no consideration for pagination or chunked queries.
- It made a required request field nullable so it could tell whether the client sent it, which advertises the field as optional when it was not.