# TMS API Versioning Policy

## 1. Purpose

The TMS API uses URL-based versioning to allow the API to evolve without breaking existing clients. Each major API version represents a stable contract that clients can depend on.

Example:

- `/api/v1/courses`
- `/api/v2/courses`

New versions are introduced when changes cannot be made safely in the existing contract.

---

## 2. Breaking Changes

A breaking change is any change that can cause an existing client to fail or behave differently.

The following changes require a new API version:

- Removing an existing response field.
- Renaming a response field.
- Changing the data type of an existing field.
- Changing HTTP status codes returned by an endpoint.
- Changing authentication or authorization requirements.
- Tightening validation rules that reject previously valid requests.
- Changing the default sorting or filtering behavior.
- Changing the structure of the response body.

Example:

V1 response:

```json
{
  "items": [],
  "totalCount": 25
}