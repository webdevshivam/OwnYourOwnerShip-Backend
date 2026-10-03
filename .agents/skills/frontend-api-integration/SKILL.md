---
name: frontend-api-integration
description: Comprehensive skill for analyzing, documenting, and integrating backend APIs for the OwnMyOwnership frontend, ensuring clear contracts, TypeScript models, Angular integration, state mapping, and robust error handling.
---

# Frontend API Integration Skill

## Purpose

This skill defines how the agent should analyze, document, and integrate backend APIs for the OwnMyOwnership frontend.

The agent must provide enough information for a frontend developer to consume an API correctly without repeatedly inspecting backend code.

The skill should cover:

- API purpose
- Endpoint
- HTTP method
- Authentication
- Request parameters
- Request body
- Response structure
- Error responses
- Loading states
- Empty states
- Pagination
- Validation
- Concurrency considerations
- Frontend service design
- TypeScript models
- State management
- Caching
- Retry behaviour
- Error handling
- UI implications
- Best practices
- Potential API design problems

---

# Core Principle

> The frontend should consume a clear API contract, not depend on backend implementation details.

The agent must distinguish between:

```text
API Contract
```

and:

```text
Backend Implementation
```

Frontend code should depend primarily on the API contract.

---

# When to Use This Skill

Use this skill when:

- A new backend API is created.
- An existing API changes.
- A frontend feature needs backend integration.
- The user asks how to consume an API.
- The user asks for API documentation.
- The user asks what data the frontend needs.
- The frontend and backend contracts need to be reviewed.
- An API has pagination, filtering, sorting, searching, uploading, or real-time behaviour.
- Authentication or authorization affects the frontend.
- The frontend needs to handle concurrent updates.

---

# Step 1 — Understand the Feature

Before documenting an API, identify:

- Feature name
- User flow
- Required UI
- Required data
- User actions
- Backend operations
- Authentication requirements
- Authorization requirements
- Loading states
- Empty states
- Error states

Do not document APIs in isolation when the feature context is available.

---

# Step 2 — Identify Required APIs

Determine whether the frontend needs:

```text
GET
POST
PUT
PATCH
DELETE
```

Do not automatically create separate endpoints for every UI action.

Prefer APIs that represent meaningful business operations.

For example:

```text
PATCH /tasks/{id}
```

may be appropriate for updating task information.

But a meaningful business operation may justify:

```text
POST /tasks/{id}/complete
```

when completing a task has business rules beyond simply changing a status field.

---

# API Documentation Format

Every API should be documented using this structure.

## API Name

Human-readable name.

## Purpose

What the API does and why the frontend needs it.

## Endpoint

```text
METHOD /api/...
```

## Authentication

Specify:

```text
Required / Not required
```

If required, explain how the frontend authenticates.

## Authorization

Explain which users can call the API.

Do not rely only on frontend route guards for authorization.

---

# Request Documentation

Document:

### Path Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| id | string | Yes | Resource identifier |

### Query Parameters

| Parameter | Type | Required | Default | Description |
|---|---|---|---|---|
| page | number | No | 1 | Page number |
| limit | number | No | 20 | Number of records |

### Request Body

Provide a TypeScript representation:

```ts
export interface CreateTaskRequest {
  title: string;
  description?: string;
  priority: TaskPriority;
  dueDate?: string;
}
```

Document:

- Required fields
- Optional fields
- Allowed values
- Maximum lengths
- Validation rules
- Date formats
- Nullable fields

---

# Response Documentation

Always provide the expected response.

Example:

```ts
export interface TaskResponse {
  id: string;
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueDate: string | null;
  status: TaskStatus;
  createdAt: string;
  updatedAt: string;
}
```

Explain important fields.

Do not expose unnecessary backend/database implementation details to the frontend.

---

# Response Envelope

Identify whether the API uses:

```ts
{
  data: ...
}
```

or:

```ts
{
  success: true,
  data: ...
}
```

or another format.

Use the project's established convention consistently.

Do not introduce a new response format for one endpoint without a reason.

---

# Error Contract

Document expected errors.

Example:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Validation Error
429 Too Many Requests
500 Internal Server Error
```

For each important error explain:

- Meaning
- Expected frontend behaviour
- Whether the user should see a message
- Whether retry is appropriate

Example:

```ts
export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, string[]>;
}
```

The frontend should not depend on parsing arbitrary backend error strings when stable error codes can be provided.

---

# Frontend State Mapping

For every API, document the UI states.

At minimum:

```text
Initial
Loading
Success
Empty
Validation Error
Unauthorized
Forbidden
Not Found
Conflict
Server Error
Network Error
```

Example:

```text
Loading
   ↓
Success → Display data
   ↓
Empty   → Display empty state

Failure
   ├── 401 → Authentication handling
   ├── 403 → Permission message
   ├── 404 → Not found
   ├── 409 → Conflict handling
   └── 500 → Generic error + retry
```

---

# Angular Integration

For OwnMyOwnership Angular applications:

Prefer:

```text
Component
   ↓
Facade / Feature Service
   ↓
API Service
   ↓
HttpClient
   ↓
Backend
```

Avoid putting HTTP calls directly inside large components.

Example:

```ts
@Injectable({
  providedIn: 'root',
})
export class TaskApiService {
  constructor(private readonly http: HttpClient) {}

  getTasks(params: GetTasksParams) {
    return this.http.get<TaskListResponse>(
      '/api/tasks',
      { params }
    );
  }
}
```

Keep API communication separate from UI-specific logic.

---

# TypeScript Models

Define request and response types.

Prefer explicit types:

```ts
interface CreateTaskRequest {
  title: string;
  priority: TaskPriority;
}
```

Avoid:

```ts
any
```

unless there is a documented reason.

Do not blindly reuse backend/database entity models as frontend models.

Backend entity:

```text
Database concern
```

Frontend model:

```text
UI/API contract concern
```

They may look similar, but they do not have to be identical.

---

# API Service Responsibilities

API services should primarily handle:

- HTTP calls
- Request construction
- Response typing
- API-specific transformations where appropriate

Avoid putting large business workflows inside the API service.

Do not turn the API service into a "god service".

---

# Feature Service / Facade Responsibilities

Feature-level services may handle:

- UI state
- Combining multiple API calls
- Business flow orchestration
- Loading state
- Error state
- Feature-specific transformations
- Caching where justified

Keep this separate from raw HTTP communication.

---

# HTTP Interceptors

Use interceptors for cross-cutting concerns such as:

- Authentication headers
- Token refresh
- Global request configuration
- Correlation/request IDs
- Common error handling where appropriate

Do not put feature-specific behaviour into global interceptors.

Avoid interceptors that silently modify business data.

---

# Authentication

Document:

- How authentication is sent
- Access token handling
- Refresh token handling
- Expiration behaviour
- Logout behaviour
- 401 handling

Never store sensitive authentication information in an unsafe location merely for convenience.

Follow the application's established authentication architecture.

---

# Authorization

The frontend may hide UI elements based on permissions, but:

> Frontend authorization is for user experience, not security.

The backend must enforce authorization.

The frontend should correctly handle:

```text
403 Forbidden
```

even when the UI already hides the action.

---

# Pagination

When an API supports pagination, document:

```text
Pagination type
Page size
Maximum page size
Cursor
Next cursor
Previous cursor
Total count
```

For cursor pagination:

```ts
interface CursorPage<T> {
  items: T[];
  nextCursor: string | null;
  hasNext: boolean;
}
```

Do not assume every API uses page-number pagination.

Choose the pagination approach based on the backend contract.

---

# Filtering and Sorting

Document:

```text
filter parameters
search parameters
sort field
sort direction
```

Example:

```text
GET /api/tasks
?status=ACTIVE
&priority=HIGH
&sortBy=dueDate
&sortOrder=asc
```

The frontend should not implement server-side filtering locally unless there is a specific reason.

---

# Dates and Time

Document the timezone and format.

Prefer a clear contract such as:

```text
2026-09-30T18:30:00Z
```

For timestamps:

- Treat backend UTC timestamps as authoritative.
- Convert to local display time in the UI.
- Do not make business decisions using the browser's current clock when the server must be authoritative.

Pay special attention to:

- Due dates
- Recurring tasks
- Reminders
- Daily statistics
- Streaks
- Scheduled jobs

---

# Caching

Before adding frontend caching, ask:

> Does this data actually need caching?

Consider:

- Data volatility
- Request frequency
- User experience
- Cache invalidation
- Memory usage
- Stale data risk

Prefer existing application/query-state infrastructure if the project already uses one.

Do not introduce a caching library solely because it is popular.

---

# Request Cancellation

For requests that can become obsolete, consider cancellation.

Important examples:

```text
Search
Autocomplete
Rapid filter changes
Route changes
Repeated API requests
```

Avoid allowing old responses to overwrite newer UI state.

Example:

```text
User searches:
"ang"

then:
"angular"

The "ang" response arrives after "angular".

The UI must not display the stale result.
```

Use appropriate RxJS operators such as:

```text
switchMap
```

when the latest request should replace the previous one.

---

# Retry Strategy

Do not retry every API request automatically.

Consider retrying transient failures such as:

```text
network interruption
temporary 5xx
```

Be careful with mutating operations.

For example:

```text
POST /tasks
```

should not blindly retry if doing so can create duplicates.

Before retrying a mutation, consider:

- Idempotency
- Idempotency key
- Backend guarantees

---

# Concurrency

Frontend API integration must consider concurrent updates.

For example:

```text
User opens Task A

Tab 1:
updates priority

Tab 2:
updates description using stale data
```

The frontend should understand whether the backend uses:

- Versioning
- ETags
- Row version
- Last-write-wins
- Conflict detection

If the API returns:

```text
409 Conflict
```

document exactly how the frontend should handle it.

Do not hide concurrency conflicts silently.

---

# Optimistic UI

Optimistic updates may be used when:

- The operation is simple.
- Failure can be safely reverted.
- The user benefits from immediate feedback.

Before using optimistic updates, determine:

```text
What happens if the request fails?
What happens if another update occurs?
Can the operation be safely rolled back?
```

Avoid optimistic updates for operations where incorrect temporary state could be harmful or confusing.

---

# Multiple API Calls

When a page requires multiple APIs, identify dependencies.

Example:

```text
GET /profile
GET /tasks
GET /statistics
```

Determine whether they can run independently.

Independent requests may be executed concurrently.

Dependent requests should respect their dependency.

Avoid unnecessary sequential requests:

```text
Request A
 ↓
Request B
 ↓
Request C
```

when they can safely execute concurrently.

---

# N+1 API Problem

Look for frontend patterns such as:

```text
GET /tasks
GET /users/1
GET /users/2
GET /users/3
...
```

Identify unnecessary request multiplication.

Consider:

- Backend aggregation
- Batch endpoint
- Embedded summary data
- Appropriate caching

Do not automatically create a new endpoint; first verify whether the problem is real.

---

# API Contract Review

Before frontend implementation, identify:

### Contract problems

- Missing fields
- Ambiguous field names
- Inconsistent naming
- Inconsistent response formats
- Missing error codes
- Missing pagination metadata
- Unclear date format
- Unclear nullability
- Missing authorization rules
- Missing validation rules

Report these before writing frontend code when they materially affect implementation.

---

# API Documentation Output

When asked:

> "Give me the API information for this frontend feature."

Produce:

## 1. Feature

Short description.

## 2. Required APIs

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/tasks` | Fetch tasks |
| POST | `/api/tasks` | Create task |
| PATCH | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |

## 3. Request Models

TypeScript interfaces.

## 4. Response Models

TypeScript interfaces.

## 5. Error Contract

Status codes and frontend handling.

## 6. UI States

Loading, empty, error, success, etc.

## 7. Frontend Integration

Recommended Angular service/facade structure.

## 8. Best Practices

Relevant recommendations only.

## 9. API Concerns

Potential backend contract problems.

## 10. Example Usage

Show a concise Angular example when useful.

---

# Do Not Guess API Contracts

If the backend API is available in the project:

> Inspect the actual backend implementation before documenting the API.

Do not invent:

```text
endpoint
field
response
status code
authorization rule
```

If the API contract is unavailable, clearly mark assumptions.

---

# Backend vs Frontend Responsibilities

Clearly distinguish:

### Backend

Responsible for:

- Authorization
- Validation
- Business rules
- Data integrity
- Transactions
- Concurrency control
- Security
- Persistence

### Frontend

Responsible for:

- Form validation for UX
- Loading states
- Error presentation
- UI state
- User feedback
- Request orchestration
- Display formatting
- Client-side interaction

Never move a security-critical business rule only to the frontend.

---

# Best Practice Principle

For every frontend API integration, ask:

```text
Is the API contract clear?
        ↓
Are request/response types defined?
        ↓
Are loading/error/empty states handled?
        ↓
Is authentication handled correctly?
        ↓
Is authorization enforced by backend?
        ↓
Are retries safe?
        ↓
Are stale responses possible?
        ↓
Are concurrent updates possible?
        ↓
Can duplicate requests cause problems?
        ↓
Is the implementation unnecessarily complex?
```

---

# Final Principle

The agent should not simply tell the frontend developer:

> "Call this endpoint."

It should explain:

```text
What API exists
        ↓
Why the frontend needs it
        ↓
How to call it
        ↓
What to send
        ↓
What comes back
        ↓
What can go wrong
        ↓
How the UI should react
        ↓
How to integrate it cleanly
        ↓
What best practices apply
```

The final result should be a **frontend-ready API contract and integration guide**, not merely an endpoint list.
