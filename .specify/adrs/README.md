# Architecture Decision Records (ADRs)

**Purpose:** Document significant architectural decisions made in the Sorcha project.

## What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences.

## ADR Format

Each ADR follows this structure:

1. **Title** - Short present tense imperative phrase
2. **Status** - Proposed, Accepted, Deprecated, Superseded
3. **Context** - Forces at play (technical, political, social, project)
4. **Decision** - Response to these forces
5. **Consequences** - Impact after applying the decision

## ADR Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [ADR-001](adr-001-grpc-service-communication.md) | Adopt gRPC for Internal Service Communication | Accepted | 2025-12-13 |

## Creating a New ADR

1. Copy the template from `adr-template.md`
2. Number it sequentially (ADR-XXX)
3. Fill in all sections
4. Submit for review
5. Update this index

## References

- [Architecture Decision Records](https://adr.github.io/)
- [Michael Nygard's ADR Format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
