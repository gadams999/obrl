# Branch Naming Conventions

## Overview

This project follows standard Git branch naming conventions to maintain clarity and consistency across the codebase.

## Branch Naming Format

All branches should follow this format:

```
<type>/<description>
```

### Branch Types

- **feat/** - New features or enhancements
  - Example: `feat/animated-transitions`
  - Example: `feat/v0.5.2-release`
  - Example: `feat/grid-layout-configuration`

- **fix/** - Bug fixes
  - Example: `fix/crash-on-startup`
  - Example: `fix/v0.5.1-timing-issues`
  - Example: `fix/memory-leak-in-overlay`

- **docs/** - Documentation only changes
  - Example: `docs/update-readme`
  - Example: `docs/api-documentation`
  - Example: `docs/contributing-guide`

- **test/** - Adding or updating tests
  - Example: `test/property-based-tests`
  - Example: `test/integration-coverage`
  - Example: `test/ui-automation`

- **refactor/** - Code refactoring without changing functionality
  - Example: `refactor/extract-services`
  - Example: `refactor/simplify-layout-logic`
  - Example: `refactor/dependency-injection`

- **chore/** - Maintenance tasks, dependency updates, build changes
  - Example: `chore/update-dependencies`
  - Example: `chore/ci-pipeline-improvements`
  - Example: `chore/dotnet-upgrade`

- **perf/** - Performance improvements
  - Example: `perf/optimize-rendering`
  - Example: `perf/reduce-memory-usage`
  - Example: `perf/cache-configuration`

## Description Guidelines

The description part should be:
- **Lowercase** with words separated by hyphens
- **Descriptive** but concise
- **Meaningful** - clearly indicates what the branch is for

### Good Examples
✅ `feat/pbt-iteration-control`
✅ `fix/flash-animation-timing`
✅ `docs/property-testing-guide`
✅ `test/overlay-viewmodel-tests`
✅ `refactor/settings-management`

### Bad Examples
❌ `v0.5.2` (no type prefix)
❌ `feat/MyNewFeature` (not lowercase)
❌ `fix/bug` (not descriptive)
❌ `feat/add_new_feature` (underscores instead of hyphens)
❌ `feature/new-thing` (use `feat/` not `feature/`)

## Version Release Branches

For version releases, use the `feat/` prefix with the version number:

```
feat/v<major>.<minor>.<patch>-release
```

Examples:
- `feat/v0.5.2-release`
- `feat/v1.0.0-release`
- `feat/v2.1.3-release`

## Hotfix Branches

For urgent production fixes, use the `fix/` prefix with version context:

```
fix/v<version>-<description>
```

Examples:
- `fix/v0.5.1-timing-issues`
- `fix/v1.0.0-critical-crash`
- `fix/v2.1.0-security-patch`

## Long-Running Branches

The project maintains these long-running branches:
- **main** - Production-ready code
- **develop** (if used) - Integration branch for features

All feature/fix branches should be created from and merged back to `main` (or `develop` if using GitFlow).

## Branch Lifecycle

1. **Create** branch from main with proper naming
2. **Develop** and commit changes
3. **Push** to remote repository
4. **Create PR** to main
5. **Review** and address feedback
6. **Merge** to main after approval
7. **Delete** branch after merge

## Commit Messages

While not strictly enforced, consider using conventional commit format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Examples:
- `feat(overlay): add animated transitions`
- `fix(tests): increase timing tolerance for CI`
- `docs(readme): update installation instructions`
- `test(viewmodel): add property-based tests`

## Enforcement

Branch naming is not automatically enforced but should be followed for:
- **Clarity** - Easy to understand what the branch is for
- **Organization** - Easier to find and manage branches
- **Automation** - Some CI/CD tools can use branch prefixes for routing
- **Team Collaboration** - Consistent naming helps everyone

## References

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Git Branch Naming Best Practices](https://dev.to/varbsan/a-simplified-convention-for-naming-branches-and-commits-in-git-il4)

