# Contributing to Sorcha

Thank you for your interest in contributing to Sorcha! This document provides guidelines and instructions for contributing.

## Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

### Our Standards

- Be respectful and inclusive
- Welcome newcomers and help them learn
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/sorcha.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Commit with clear messages
6. Push to your fork
7. Create a Pull Request

## Development Setup

### Prerequisites

- .NET 10 SDK or later
- Git
- Your favorite IDE (Visual Studio 2025, VS Code, or Rider)

### Building the Project

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running the Application

```bash
dotnet run --project src/Sorcha.AppHost
```

## Coding Guidelines

### C# Style

- Follow standard C# coding conventions
- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Use implicit usings where appropriate
- Target .NET 10

### Code Organization

- Keep classes focused and single-purpose
- Use minimal APIs for endpoints
- Follow SOLID principles
- Write testable code

### Naming Conventions

- Use PascalCase for public members
- Use camelCase for private fields
- Use meaningful, descriptive names
- Avoid abbreviations unless widely known

### Comments and Documentation

- Write XML documentation for public APIs
- Use comments to explain "why," not "what"
- Keep comments up-to-date with code changes
- Document complex algorithms

## Commit Messages

Write clear, concise commit messages:

```
<type>: <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```
feat: add blueprint validation engine

Implements JSON schema validation for blueprint definitions
with comprehensive error reporting.

Closes #123
```

```
fix: resolve memory leak in execution engine

Fixed disposable resources not being properly released
in long-running blueprint executions.
```

## Pull Request Process

1. **Update Documentation**: Ensure README.md and relevant docs are updated
2. **Add Tests**: Include unit tests for new functionality
3. **Update Changelog**: Add entry to CHANGELOG.md if it exists
4. **Follow Style**: Ensure code follows project style guidelines
5. **Pass CI**: All CI checks must pass
6. **Get Review**: At least one maintainer must approve

### PR Checklist

- [ ] Code builds without errors or warnings
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated
- [ ] Commit messages are clear and follow guidelines
- [ ] No merge conflicts
- [ ] Follows coding guidelines

## Testing

### Unit Tests

- Write unit tests for all new functionality
- Use xUnit as the testing framework
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

### Integration Tests

- Test service interactions
- Use test containers where appropriate
- Clean up resources after tests

### Test Organization

```
tests/
├── Sorcha.Blueprint.Engine.Tests/
├── Sorcha.Blueprint.Designer.Tests/
└── Sorcha.Integration.Tests/
```

## Documentation

### Code Documentation

- Use XML documentation comments for public APIs
- Document parameters, return values, and exceptions
- Include usage examples for complex APIs

### User Documentation

Documentation lives in the `docs/` directory:

- `docs/architecture.md` - System architecture
- `docs/getting-started.md` - Getting started guide
- `docs/blueprint-schema.md` - Blueprint definition schema
- `docs/api-reference.md` - API documentation

### Documentation Style

- Use clear, simple language
- Include code examples
- Add diagrams where helpful
- Keep docs up-to-date with code

## Issue Reporting

### Bug Reports

Include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)
- Stack traces or error messages

### Feature Requests

Include:
- Clear description of the feature
- Use cases and benefits
- Proposed implementation (if any)
- Alternatives considered

### Issue Labels

- `bug` - Something isn't working
- `enhancement` - New feature or request
- `documentation` - Documentation improvements
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention needed

## Release Process

1. Update version numbers
2. Update CHANGELOG.md
3. Create release branch
4. Run all tests
5. Create GitHub release
6. Publish NuGet packages (if applicable)

## Getting Help

- Check existing documentation
- Search closed issues
- Ask in GitHub Discussions
- Join community chat (if available)

## Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- Project documentation

Thank you for contributing to Sorcha!
