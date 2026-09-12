# Stim

A RESTful Web API built with ASP.NET Core, designed to demonstrate practical API development patterns, HTTP semantics, security, performance, and API usability.

Stim is a game/developer catalogue API where clients can retrieve, create, update, and delete developers and games while taking advantage of features commonly found in production APIs.

## Features

* **RESTful API design**
* **Cursor and offset pagination**
* **Searching**
* **Sorting**
* **Data shaping** — request only the fields required by the client
* **HATEOAS** — optional hypermedia links through content negotiation
* **API versioning** using media types
* **JWT authentication**
* **Short-lived access tokens with refresh tokens**
* **Role-Based Access Control (RBAC)**
* **ETag caching and optimistic concurrency control**
* **PostgreSQL `xmin` row-versioning**
* **Rate limiting**
* **Idempotency**
* **FluentValidation**
* **Global exception handling**
* **ASP.NET Core Identity**
* **OpenAPI documentation**
* **Scalar API reference**
* **Unit and integration testing**

## Technology Stack

| Technology            | Purpose                           |
| --------------------- | --------------------------------- |
| C#                    | Programming language              |
| ASP.NET Core          | Web API framework                 |
| Entity Framework Core | ORM and data access               |
| PostgreSQL            | Relational database               |
| ASP.NET Core Identity | User and identity management      |
| JWT                   | Authentication                    |
| FluentValidation      | Request validation                |
| OpenAPI               | API specification                 |
| Scalar                | API documentation and exploration |
| xUnit                 | Automated testing                 |

## Architecture

Stim intentionally uses a straightforward **monolithic architecture**.

The project does not use Clean Architecture or a multi-project architecture. Instead, related responsibilities are organized into folders within a single API project.

```text
Stim.Api
├── Controllers
├── Data
├── Entities
├── Extensions
├── Filters
├── Middleware
├── Migrations
├── Models
├── OpenAPI
├── Services
└── ...
```

The goal is to keep the project easy to understand while demonstrating API design and implementation rather than adding architectural complexity for its own sake.

## API Capabilities

### Pagination

Stim supports both offset-based and cursor-based pagination.

Offset pagination is useful when clients need traditional page navigation:

```http
GET /api/games?page=2&pageSize=20
```

Cursor pagination is available for scenarios where stable pagination across changing datasets is more appropriate:

```http
GET /api/games?paginationType=Cursor&pageSize=20
```

Cursor pagination uses a deterministic ordering strategy and returns cursors that clients can use to retrieve the next or previous page.

### Searching

Resources can be filtered using a search query:

```http
GET /api/games?q=elden
```

### Sorting

Clients can specify the fields used to order results:

```http
GET /api/games?sort=title
```

Multiple sort expressions can also be supplied where supported.

### Data Shaping

Clients can request only the fields they require:

```http
GET /api/games?fields=id,title,price
```

This allows consumers to reduce unnecessary response data.

### HATEOAS

Stim supports optional HATEOAS responses.

The API uses content negotiation to determine whether hypermedia links should be included in the response.

A standard response can return the resource data without links, while a HATEOAS media type enables additional navigation information.

```http
Accept: application/vnd.stim.hateoas+json
```

### API Versioning

The API supports media-type based versioning, allowing different representations of the API to evolve without relying on URL-based versioning.

```http
Accept: application/vnd.stim.hateoas.1+json
```

## Authentication & Authorization

Stim uses JWT bearer authentication with short-lived access tokens and refresh tokens.

Authorization is enforced using role-based policies where appropriate.

Example:

```http
Authorization: Bearer <access-token>
```

Protected endpoints require a valid access token, while administrative operations can require the appropriate role.

## Concurrency Control

The API uses HTTP `ETag` headers together with PostgreSQL's `xmin` system column to implement optimistic concurrency control.

Resources expose their current entity version through an ETag:

```http
ETag: "745"
```

Clients can then use `If-Match` when updating or deleting a resource:

```http
If-Match: "745"
```

If the resource has changed since the client retrieved it, the request can be rejected rather than silently overwriting another update.

## Idempotency

Idempotency support is provided for operations where clients may safely retry a request without unintentionally performing the operation multiple times.

This is particularly useful for unreliable networks and clients that need to retry requests.

## Validation & Error Handling

Request validation is implemented using **FluentValidation**.

The API also uses centralized exception handling so unexpected application errors are handled consistently instead of requiring every controller action to implement its own exception handling.

Errors are returned using the API's standardized error response format.

## API Documentation

Stim exposes an OpenAPI specification and provides an interactive API reference through Scalar.

Once the application is running, the API documentation can be accessed at launch or manually through the configured Scalar endpoint.

## Getting Started

### Prerequisites

* .NET 10 SDK
* PostgreSQL
* Git

### Clone the Repository

```bash
git clone https://github.com/Sifiso-core/Stim.git
cd Stim
```

### Configure the Database

Configure the PostgreSQL connection string using the application's configuration system.

For local development, user secrets or environment variables can be used instead of committing credentials to source control.

### Apply Migrations (Will happen automatically at launch or you can do so manually with the following command)

```bash
dotnet ef database update
```

### Run the API

```bash
dotnet run
```

The API will start on the configured HTTP/HTTPS endpoints.

## Project Goals

Stim was built as a practical portfolio project to demonstrate how to build fairly complex ASP.NET WEB API following Restful practices.
