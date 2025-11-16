# Peer Service Integration Tests - Archived

**Date Archived:** November 16, 2025
**Reason:** Tests incompatible with current Peer Service architecture
**Location:** tests/Sorcha.Peer.Service.Integration.Tests/

---

## Summary

The Peer Service integration tests have been archived due to incompatibility with the current Peer Service implementation. These tests were written for an earlier, simplified version of the service and are no longer valid.

## Issue

The integration tests were designed for a Peer Service with:
- Single `peer.proto` file
- Simple REST API endpoints
- Basic gRPC service implementation

The current Peer Service has evolved to:
- Multiple proto files (`peer_discovery.proto`, `peer_communication.proto`, `transaction_distribution.proto`)
- Different service structure with separate concerns
- Enhanced architecture with discovery, communication, and distribution components

## Status File

See `tests/Sorcha.Peer.Service.Integration.Tests/STATUS.md` for detailed information about the incompatibility.

## Recommendation

**Option 1: Rewrite Tests** (Preferred when time permits)
- Update test infrastructure to match current architecture
- Rewrite tests for the three service components
- This provides value by testing actual implementation

**Option 2: Leave Archived**
- Keep as reference for test structure
- Document planned test approach
- Implement new tests when Peer Service work resumes

## Related Documentation

- `tests/Sorcha.Peer.Service.Integration.Tests/README.md` - Original test documentation
- `tests/Sorcha.Peer.Service.Integration.Tests/ARCHITECTURE.md` - Test architecture guide
- `tests/Sorcha.Peer.Service.Integration.Tests/QUICKSTART.md` - Quick start guide
- `docs/peer-service-completion.md` - Peer Service completion status

## Notes

The documentation created for these tests remains valuable as a guide for:
- Test infrastructure patterns
- Documentation standards
- Script organization
- Testing best practices

These patterns should be applied when new Peer Service tests are written.

---

**Archived by:** Sorcha Development Team
**Review Date:** When Peer Service work resumes
**Priority:** Low - Not blocking MVD delivery
