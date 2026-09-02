# RaceDay API Endpoint Plan

This plan covers all endpoints the RaceDay RESTful API (built in Part 2) will expose. It matches the Part 1 ERD (Users, Venues, Events, Categories, Enrolments, Results).

Roles: **None** = public, **Any** = any authenticated user, **Organiser** = Organiser only, **Participant** = Participant only.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None | { firstName, lastName, email, password, role, phoneNumber } | 201 Created – user created; 400 Bad Request – validation failed; 409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and starts a session storing their user ID and role. | None | { email, password } | 200 OK – login successful, session started; 401 Unauthorized – invalid credentials |
| POST | /api/auth/logout | Ends the current user's session. | Any | None | 200 OK – session cleared |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's own profile information. | Any | None | 200 OK – profile data; 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's own profile information. | Any | { firstName, lastName, phoneNumber } | 200 OK – profile updated; 400 Bad Request; 401 Unauthorized |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Returns a list of all RaceDay events. | None | None | 200 OK – list of events |
| GET | /api/events/{id} | Returns the details of a specific event using its event ID. | None | None | 200 OK – event details; 404 Not Found |
| POST | /api/events | Creates a new event under the logged-in Organiser. | Organiser | { eventName, description, eventDate, eventType, distance, venueId } | 201 Created – event created; 400 Bad Request; 401/403 – access invalid |
| PUT | /api/events/{id} | Updates an existing event owned by the logged-in Organiser. | Organiser | { eventName, description, eventDate, eventType, distance, venueId } | 200 OK – event updated; 400 Bad Request; 403 Forbidden – not the owning Organiser; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser | None | 200 OK – event deleted; 403 Forbidden; 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Returns all categories available for a specific event. | None | None | 200 OK – list of categories; 404 Not Found – event does not exist |
| POST | /api/events/{eventId}/categories | Creates a new age or distance category for an event owned by the logged-in Organiser. | Organiser | { categoryName, description } | 201 Created – category created; 400 Bad Request; 403 Forbidden; 404 Not Found |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser | { categoryName, description } | 200 OK – category updated; 403 Forbidden; 404 Not Found |
| DELETE | /api/categories/{id} | Deletes an existing category. | Organiser | None | 200 OK – category deleted; 403 Forbidden; 404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/enrolments | Enrols the logged-in Participant into the event under a chosen category. | Participant | { categoryId } | 201 Created – enrolment recorded; 400 Bad Request; 404 Not Found – event or category does not exist; 409 Conflict – already enrolled |
| GET | /api/users/me/enrolments | Returns all events the logged-in Participant has enrolled in. | Participant | None | 200 OK – list of enrolments |
| GET | /api/events/{eventId}/enrolments | Returns all Participants enrolled in a specific event, for the Organiser who owns it. | Organiser | None | 200 OK – list of enrolments; 403 Forbidden – not the owning Organiser; 404 Not Found |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/result | Captures the finish time and finishing position for a Participant's enrolment. | Organiser | { finishTime, finishPosition, totalFinishers } | 201 Created – result recorded; 400 Bad Request; 403 Forbidden; 404 Not Found – enrolment does not exist; 409 Conflict – result already captured |
| GET | /api/users/me/results | Returns the logged-in Participant's own race results across all events. | Participant | None | 200 OK – list of results |
| GET | /api/events/{eventId}/results | Returns all published results for a specific event, for the owning Organiser. | Organiser | None | 200 OK – list of results; 403 Forbidden; 404 Not Found |
