# TMS API Versioning Policy

## 1. Purpose

The TMS API uses URL-based versioning to allow the API to evolve without breaking existing clients. Each major API version represents a stable contract that clients can depend on.

Example:

* `/api/v1/courses`
* `/api/v2/courses`

New versions are introduced only when changes cannot be made safely within the existing API contract.

---

## 2. Breaking Changes

A breaking change is any change that can cause an existing client to fail or behave differently. Breaking changes always require a new major API version.

Examples of breaking changes include:

* Removing an existing response field.
* Renaming a response field.
* Changing the data type of an existing field.
* Changing the structure of a response body.
* Changing HTTP status codes returned by an endpoint.
* Tightening validation rules so that previously valid requests are rejected.
* Changing authentication or authorization requirements.
* Changing the default sorting, filtering, or paging behavior.
* Removing or renaming an endpoint.

Example:

**Version 1**

```json
{
  "items": [],
  "totalCount": 25
}
```

**Version 2 (breaking)**

```json
{
  "courses": []
}
```

This is a breaking change because the `items` and `totalCount` fields were removed.

---

## 3. Non-Breaking (Additive) Changes

The following changes are considered non-breaking and may be added to the current API version:

* Adding a new optional response field.
* Adding a new endpoint.
* Adding a new optional query parameter.
* Adding support for new filtering or sorting options without changing existing behavior.
* Adding new response headers.
* Improving performance without changing API behavior.

Existing clients should continue to work without modification.

---

## 4. Sunset Policy

When a new API version is released, the previous version remains available for **at least six months**.

The TMS API guarantees a minimum six-month support window after V2 is released so that training centres and partner organizations have enough time to migrate without service disruption.

After the sunset period, the older version may be retired.

---

## 5. Communication Policy

Whenever a new API version is released, the following communication process will be followed:

* Include **Deprecation**, **Sunset**, and **Link** HTTP headers on deprecated API responses.
* Record all breaking and non-breaking changes in the project's **CHANGELOG**.
* Notify every partner or team using the API by email.
* Send a calendar invitation announcing the planned shutdown date of the deprecated version.

This communication begins as soon as the new version is published.

---

## 6. Version Skipping

Clients are not required to migrate through every API version.

For example, a client using **V1** may migrate directly to **V3** without first upgrading to **V2**, provided they update their application to follow the V3 API contract.

Each major API version is independently supported according to the versioning policy.

---

## Summary

The TMS API follows semantic versioning principles by introducing new major versions only for breaking changes, allowing additive improvements within existing versions, supporting older versions for at least six months, communicating changes clearly, and allowing clients to migrate directly to any supported API version.
