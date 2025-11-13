# Previous-Codebase Tasks Directory

This directory is part of the Spec-Kit structure for managing individual work units and development tasks.

## Purpose

The `tasks/` directory contains individual task files that describe specific units of work with enough context for developers or AI agents to implement them. Each task should be:

- **Self-contained:** Complete description of what needs to be done
- **Actionable:** Clear acceptance criteria and definition of done
- **Contextual:** Links to relevant specifications and architecture
- **Testable:** Specific testing requirements and validation steps

## Task File Structure

Each task should follow this template:

```markdown
# Task: [Short descriptive title]

**ID:** TASK-XXX
**Status:** [Not Started | In Progress | In Review | Completed | Blocked]
**Priority:** [Critical | High | Medium | Low]
**Estimate:** [Story points or time estimate]
**Assignee:** [Developer name or "Unassigned"]
**Created:** YYYY-MM-DD
**Updated:** YYYY-MM-DD

## Context

Brief description of why this task exists and how it fits into the larger project.

**Related Specifications:**
- [Requirement FR-X from spec.md](../spec.md#requirement-reference)
- [Architecture section from plan.md](../plan.md#architecture-reference)

**Dependencies:**
- TASK-YYY must be completed first
- Requires feature flag X to be enabled
- Depends on Service Y being deployed

## Objective

Clear statement of what this task accomplishes.

## Implementation Details

### Changes Required

1. **File/Component A**
   - Specific change 1
   - Specific change 2

2. **File/Component B**
   - Specific change 1
   - Specific change 2

### Technical Approach

Describe the technical approach, including:
- Algorithms or patterns to use
- Libraries or frameworks involved
- Data structures and models
- API contracts or interfaces

### Constitutional Compliance

- ✅ Complies with principle X from constitution.md
- ✅ Follows architectural pattern Y from plan.md
- ⚠️ Potential issue with principle Z (justification required)

## Testing Requirements

### Unit Tests
- [ ] Test scenario 1
- [ ] Test scenario 2
- [ ] Test edge case 1

### Integration Tests
- [ ] Integration scenario 1
- [ ] Integration scenario 2

### Manual Testing
- [ ] Manual test step 1
- [ ] Manual test step 2

## Acceptance Criteria

- [ ] Criterion 1 met
- [ ] Criterion 2 met
- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated

## Implementation Notes

(Space for notes during implementation, challenges encountered, decisions made)

## Review Checklist

- [ ] Code follows constitutional principles
- [ ] Tests achieve required coverage
- [ ] Documentation updated
- [ ] Performance impact assessed
- [ ] Security review completed (if needed)
- [ ] Breaking changes documented

---

**Task Control**
- **Created By:** [Name]
- **Reviewed By:** [Name]
- **Approved By:** [Name]
```

## Task Workflow

### 1. Task Creation

Tasks can be created from:
- Requirements in spec.md
- Architecture work in plan.md
- Bug reports
- Technical debt
- Improvements

**Process:**
1. Create task file: `TASK-XXX-short-description.md`
2. Fill in template with all required information
3. Link to relevant specifications
4. Estimate effort
5. Assign priority

### 2. Task Assignment

- Team lead assigns based on priority and skills
- AI agents can pick up unassigned tasks with clear requirements
- Update status to "In Progress" when starting
- Add implementation notes as work progresses

### 3. Implementation

- Follow technical approach outlined in task
- Maintain notes section with decisions and discoveries
- Update task file with any changes to requirements
- Check off acceptance criteria as completed

### 4. Review

- Create PR referencing task ID
- Ensure all acceptance criteria met
- Code review against constitutional principles
- Update task status to "In Review"

### 5. Completion

- Merge PR
- Update task status to "Completed"
- Archive or move to completed/ subdirectory
- Update related specifications if needed

## Task Organization

### By Status

```
tasks/
├── backlog/           # Tasks not yet started
├── in-progress/       # Currently being worked on
├── review/            # In code review
├── completed/         # Finished tasks (archived)
└── blocked/           # Tasks blocked by dependencies
```

### By Category

```
tasks/
├── features/          # New feature implementations
├── bugs/             # Bug fixes
├── refactoring/      # Code improvements
├── infrastructure/   # DevOps and infrastructure
├── documentation/    # Documentation updates
└── testing/          # Test improvements
```

## Task Naming Convention

Format: `TASK-[NUMBER]-[short-description].md`

Examples:
- `TASK-001-implement-wallet-encryption.md`
- `TASK-042-add-blueprint-validation.md`
- `TASK-099-upgrade-ef-core.md`

## Best Practices

### Writing Tasks

1. **Be Specific**
   - Clear, unambiguous requirements
   - Concrete acceptance criteria
   - Specific file and component references

2. **Provide Context**
   - Link to specifications
   - Explain the "why" not just the "what"
   - Reference related tasks and dependencies

3. **Make Testable**
   - Define test requirements
   - Include edge cases
   - Specify validation methods

4. **Keep Updated**
   - Update status regularly
   - Document decisions and changes
   - Note blockers immediately

### For AI Agents

When working on a task:

1. **Read completely** before starting
2. **Check dependencies** are met
3. **Verify constitutional compliance** for approach
4. **Implement and test** according to requirements
5. **Update task file** with notes and status
6. **Create PR** referencing task ID

### For Developers

1. **Self-assign** tasks you're working on
2. **Update estimates** if scope changes
3. **Ask questions** in PR or task comments
4. **Document decisions** in implementation notes
5. **Mark blockers** clearly and promptly

## Integration with Development

### Task References in Commits

```bash
git commit -m "TASK-042: Implement blueprint validation engine

- Add validation rules for blueprint syntax
- Implement custom validator for workflow logic
- Add unit tests for validation scenarios

Refs: TASK-042"
```

### Task References in PRs

```markdown
## Related Task

Implements [TASK-042: Add Blueprint Validation](../.specify/tasks/TASK-042-add-blueprint-validation.md)

## Changes

[PR description referencing task requirements]

## Acceptance Criteria

All criteria from TASK-042 have been met:
- [x] Criterion 1
- [x] Criterion 2
- [x] Criterion 3
```

## Task Metrics

Track these metrics for process improvement:

- **Cycle Time:** Time from "In Progress" to "Completed"
- **Lead Time:** Time from "Not Started" to "Completed"
- **Blocked Time:** Time spent in "Blocked" status
- **Rework Rate:** Tasks that return from "Review" to "In Progress"

## Examples

See example tasks in `examples/` subdirectory:
- `examples/TASK-EXAMPLE-001-feature-task.md`
- `examples/TASK-EXAMPLE-002-bug-fix-task.md`
- `examples/TASK-EXAMPLE-003-refactoring-task.md`

## Migration from Other Systems

If migrating tasks from other systems (Azure DevOps, Jira, GitHub Issues):

1. Export tasks in your current format
2. Transform to spec-kit task format
3. Maintain reference to original work item ID
4. Include link to original item for history

## Tools and Automation

### Task Management Scripts

Future automation possibilities:
- Task validation (ensure all required fields present)
- Status updates from Git commits
- Task board generation from markdown files
- Dependency graph visualization

### Editor Integration

Recommended VS Code extensions:
- Markdown All in One
- Markdown Lint
- Task Provider (custom extension)

## Questions and Support

- **Task Template Questions:** Reference this README
- **Task Assignment:** Contact team lead
- **Task Blocking Issues:** Document in task and notify team
- **Process Improvements:** Create issue with `process-improvement` label

---

**Document Control**
- **Created:** 2025-11-11
- **Owner:** Previous-Codebase Development Team
- **Status:** Active
- **Review:** Per sprint or as needed
