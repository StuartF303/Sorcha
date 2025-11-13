# Task: Implement RegisterService Business Logic

**ID:** REG-004
**Status:** Not Started
**Priority:** Critical
**Estimate:** 12 hours
**Created:** 2025-11-13
**Specification:** [siccar-register-service.md](../specs/siccar-register-service.md)

## Objective

Implement the core RegisterService class containing business logic for register creation, management, and lifecycle operations.

## Tasks

### Interface Definition
- [ ] Create `Services/IRegisterService.cs` interface
- [ ] Define register creation methods
- [ ] Define register update methods
- [ ] Define register query methods
- [ ] Define register deletion methods
- [ ] Define register status management methods

### RegisterService Implementation
- [ ] Create `Services/RegisterService.cs` class
- [ ] Implement `IRegisterService` interface
- [ ] Add constructor with dependency injection:
  - `IRegisterRepository` repository
  - `IEventPublisher` eventPublisher
  - `ILogger<RegisterService>` logger
  - `IAuthorizationService` authService (optional)

### Register Creation
- [ ] Implement `CreateRegisterAsync(Register newRegister, string tenantId)`
- [ ] Validate register properties (name, ID format)
- [ ] Check register count limit (25 max)
- [ ] Check for duplicate register IDs
- [ ] Initialize register with default values
- [ ] Insert register into repository
- [ ] Publish `RegisterCreated` event
- [ ] Log register creation
- [ ] Handle and wrap exceptions

### Register Retrieval
- [ ] Implement `GetRegisterAsync(string registerId)`
- [ ] Implement `GetRegistersAsync()`
- [ ] Implement `GetRegistersByTenantAsync(string tenantId)`
- [ ] Implement `QueryRegistersAsync(Func<Register, bool> predicate)`
- [ ] Add caching support (optional)
- [ ] Handle not found cases

### Register Update
- [ ] Implement `UpdateRegisterAsync(Register register)`
- [ ] Validate register exists
- [ ] Validate only mutable properties changed
- [ ] Update timestamp
- [ ] Save to repository
- [ ] Publish `RegisterUpdated` event
- [ ] Log update operation

### Register Status Management
- [ ] Implement `UpdateRegisterStatusAsync(string registerId, RegisterStatusTypes newStatus)`
- [ ] Validate status transitions
- [ ] Update register status
- [ ] Publish `RegisterStatusChanged` event
- [ ] Log status changes

### Register Height Management
- [ ] Implement `IncrementRegisterHeightAsync(string registerId)`
- [ ] Implement atomic height increment
- [ ] Validate height consistency
- [ ] Publish `RegisterHeightUpdated` event

### Register Deletion
- [ ] Implement `DeleteRegisterAsync(string registerId)`
- [ ] Validate register exists
- [ ] Check if register has dependent data
- [ ] Soft delete vs hard delete logic
- [ ] Clean up register collections
- [ ] Publish `RegisterDeleted` event
- [ ] Log deletion operation

### Validation Logic
- [ ] Create `ValidateRegister(Register register)` method
- [ ] Validate register ID format (guid without hyphens)
- [ ] Validate register name (max 38 chars)
- [ ] Validate tenant ID presence
- [ ] Check for required properties

### Error Handling
- [ ] Wrap repository exceptions with RegisterException
- [ ] Handle RegisterNotFoundException
- [ ] Handle RegisterAlreadyExistsException
- [ ] Handle RegisterLimitExceededException
- [ ] Log all exceptions with context

## Implementation Example

```csharp
public class RegisterService : IRegisterService
{
    private readonly IRegisterRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RegisterService> _logger;
    private const int MaximumAllowedRegisters = 25;

    public RegisterService(
        IRegisterRepository repository,
        IEventPublisher eventPublisher,
        ILogger<RegisterService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Register> CreateRegisterAsync(
        Register newRegister,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ValidateRegister(newRegister);

        var count = await _repository.CountRegisters(cancellationToken);
        if (count >= MaximumAllowedRegisters)
        {
            throw new RegisterLimitExceededException(
                $"Maximum of {MaximumAllowedRegisters} registers allowed");
        }

        newRegister.TenantId = tenantId;
        newRegister.CreatedAt = DateTime.UtcNow;
        newRegister.UpdatedAt = DateTime.UtcNow;
        newRegister.Height = 0;
        newRegister.Status = RegisterStatusTypes.ONLINE;

        var register = await _repository.InsertRegisterAsync(
            newRegister, cancellationToken);

        await _eventPublisher.PublishAsync(
            Topics.RegisterCreatedTopicName,
            new RegisterCreated
            {
                Id = register.Id,
                Name = register.Name,
                TenantId = tenantId,
                CreatedAt = register.CreatedAt
            });

        _logger.LogInformation(
            "Register {RegisterId} created for tenant {TenantId}",
            register.Id, tenantId);

        return register;
    }

    private void ValidateRegister(Register register)
    {
        if (string.IsNullOrWhiteSpace(register.Id))
            throw new ArgumentException("Register ID is required");
        if (string.IsNullOrWhiteSpace(register.Name))
            throw new ArgumentException("Register name is required");
        if (register.Name.Length > 38)
            throw new ArgumentException("Register name max 38 characters");
        // Additional validations...
    }
}
```

## Acceptance Criteria

- [ ] All IRegisterService methods implemented
- [ ] Validation logic covers all edge cases
- [ ] Events published for all mutations
- [ ] Comprehensive error handling
- [ ] Structured logging for all operations
- [ ] XML documentation for all public methods
- [ ] Unit tests >90% coverage

## Definition of Done

- All methods implemented and tested
- Unit tests passing
- Code review approved
- XML documentation complete
- Integration with repository verified
- Event publishing verified

---

**Dependencies:** REG-001, REG-002, REG-003, REG-011
**Blocks:** REG-016, REG-022
