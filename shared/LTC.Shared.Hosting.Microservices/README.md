# LTC.Shared.Hosting.Microservices

## Purpose

Provides common microservice infrastructure components shared across APIs.

## Responsibilities

- JWT authentication setup helpers.
- Swagger/OpenAPI shared setup.
- gRPC/shared middleware helpers.
- Cross-service hosting extensions.

## Key Areas

- `Authentication/*`
- `OpenApi/Swagger/*`
- Exception filters and middleware utilities.

## Notes

Changes here affect multiple services; keep APIs backward compatible when possible.