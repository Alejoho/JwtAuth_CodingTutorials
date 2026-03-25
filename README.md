# Integration of a Blazor frontend with an ASP.NET Web API using JWT in .NET 10

The Web API uses Entity Framework with a SQL Server database and `Microsoft.AspNetCore.Identity` for authentication. It logs information at every endpoint and whenever a `JwtBearerEvent` is fired to make the application flow easier to trace. When a user authenticates, the API returns the JWT and refresh token in the response body.

The frontend is a WebAssembly standalone project. The app has three pages: one for login, one to get all book reviews, and one to get the summary of reviews. Registration was not implemented here, so to test you need to call the API's register endpoint with a third-party tool like Bruno or Scalar.

If a user who is not logged in tries to access secure pages, they will be redirected to the login page.

Both tokens are stored in session storage, and the access token (JWT) is cached in the `AuthenticationService` class. Inserting the access token into requests to the API and refreshing it is handled by the `AuthenticationHandler`.

## Endpoints

### AuthenticationController
- `api/authentication/register` - public POST endpoint. Registers a new user.
- `api/authentication/login` - public POST endpoint. Authenticates the user. Returns the JWT, the JWT expiration, and the refresh token in the response body as JSON.
- `api/authentication/refresh` - public POST endpoint. It receives a JWT and a refresh token. It validates all data in the JWT except the expiration time and checks the refresh token; if valid, it issues new tokens.
- `api/authentication/revoke` - secure DELETE endpoint. Deletes the refresh token saved in the database for the user.

### BookReviewsController
- `api/bookreviews` - secure GET endpoint. Gets all the reviews.
- `api/bookreviews/[id]` - secure GET endpoint. Gets the review with the specified id if found.
- `api/bookreviews/summary` - secure GET endpoint. Gets a summary of all titles with their average review score.
- `api/bookreviews` - secure POST endpoint. Creates a new review.
- `api/bookreviews` - secure PUT endpoint. Updates a review.
- `api/bookreviews` - secure DELETE endpoint. Deletes a review.

## Insertion of the JWT in the HTTP requests of the frontend

Before sending each request, the `AuthenticationHandler` adds the Authorization header if both of the following conditions are met:
1. A JWT is stored in session storage in the browser.
2. The base URL of the request destination matches the API base URL.

## Refresh flow in the frontend

If a request to the API returns 401 (Unauthorized), a request is made to the refresh endpoint. If that response is 200 (OK), the original request is retried with the new JWT. If the refresh response is 403 (Forbidden), the user is logged out.
