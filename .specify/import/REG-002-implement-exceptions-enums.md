# Task: Implement Exceptions and Enums

**ID:** REG-002
**Status:** Not Started
**Priority:** High
**Estimate:** 3 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Create custom exception types and enumerations for the RegisterService library to provide clear error handling and type-safe status values.

## Tasks

### Custom Exceptions
- [ ] Create `RegisterException.cs` - Base exception for all register operations
- [ ] Create `RegisterNotFoundException.cs` - Register ID not found
- [ ] Create `RegisterAlreadyExistsException.cs` - Duplicate register creation
- [ ] Create `RegisterLimitExceededException.cs` - Max register count reached
- [ ] Create `TransactionNotFoundException.cs` - Transaction ID not found
- [ ] Create `DocketNotFoundException.cs` - Docket ID not found
- [ ] Create `UnauthorizedRegisterAccessException.cs` - Access denied
- [ ] Create `InvalidRegisterStateException.cs` - Invalid state transition
- [ ] Create `ChainValidationException.cs` - Chain integrity failure
- [ ] Add proper exception constructors (message, inner exception)
- [ ] Add XML documentation for all exceptions

### Exception Hierarchy
```csharp
RegisterException (base)
├── RegisterNotFoundException
├── RegisterAlreadyExistsException
├── RegisterLimitExceededException
├── TransactionNotFoundException
├── DocketNotFoundException
├── UnauthorizedRegisterAccessException
├── InvalidRegisterStateException
└── ChainValidationException
```

### Enumerations (if not in Platform)
- [ ] Verify `RegisterStatusTypes` exists in Platform
- [ ] Verify `DocketState` exists in Platform
- [ ] Verify `TransactionTypes` exists in Platform
- [ ] Create any missing enums in RegisterService namespace

### Error Codes
- [ ] Define error code constants for exceptions
- [ ] Add error codes to exception properties
- [ ] Document error codes in README

## Implementation Example

```csharp
public class RegisterException : Exception
{
    public string ErrorCode { get; set; }

    public RegisterException(string message, string errorCode = "REG_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public RegisterException(string message, Exception innerException,
        string errorCode = "REG_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class RegisterNotFoundException : RegisterException
{
    public string RegisterId { get; }

    public RegisterNotFoundException(string registerId)
        : base($"Register with ID '{registerId}' was not found", "REG_NOT_FOUND")
    {
        RegisterId = registerId;
    }
}
```

## Acceptance Criteria

- [ ] All exception classes created
- [ ] Exception hierarchy properly established
- [ ] All exceptions have XML documentation
- [ ] Error codes defined and documented
- [ ] Constructors follow .NET conventions
- [ ] Unit tests for exception creation

## Definition of Done

- All exceptions compile without warnings
- XML documentation complete
- README updated with exception list
- Exception usage documented
- Code review approved

---

**Dependencies:** REG-001
**Blocks:** REG-004, REG-005, REG-006
