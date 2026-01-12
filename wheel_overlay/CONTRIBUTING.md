# Contributing to WheelOverlay

Thank you for your interest in contributing to WheelOverlay! This guide will help you get started.

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 or later (recommended) or VS Code
- Git
- Windows OS (for DirectInput support)

### Setting Up Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/YOUR_USERNAME/obrl.git
   cd obrl/wheel_overlay
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   cd WheelOverlay.Tests
   dotnet test
   ```

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Use descriptive branch names:
- `feature/add-new-layout`
- `fix/crash-on-startup`
- `docs/update-readme`

### 2. Make Your Changes

- Write clean, readable code
- Follow existing code style and conventions
- Add tests for new functionality
- Update documentation as needed

### 3. Test Your Changes

Run tests frequently during development:

```bash
# Fast feedback (10 iterations per property test)
dotnet test --configuration FastTests

# Thorough validation (100 iterations per property test)
dotnet test --configuration Release
```

### 4. Commit Your Changes

Write clear, descriptive commit messages:

```bash
git add .
git commit -m "Add new grid layout feature

- Implement configurable row/column counts
- Add UI controls for grid configuration
- Update tests to cover new layout mode"
```

### 5. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a pull request on GitHub with:
- Clear description of changes
- Reference to any related issues
- Screenshots for UI changes
- Test results

## Testing Guidelines

### Test Coverage Requirements

All new code should include appropriate tests:

- **Unit Tests**: For individual functions and classes
- **Property-Based Tests**: For validating invariants across many inputs
- **Integration Tests**: For end-to-end workflows

### Writing Property-Based Tests

Property-based tests use FsCheck to validate that properties hold across many randomly generated inputs.

#### Required Pattern

Every property test MUST follow this pattern:

```csharp
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
[Trait("Feature", "feature-name")]
[Trait("Property", "Property N: Description")]
public Property PropertyName()
{
    return Prop.ForAll(
        Arb.From<InputType>(),
        input =>
        {
            // Arrange
            var sut = new SystemUnderTest();
            
            // Act
            var result = sut.DoSomething(input);
            
            // Assert
            return result.IsValid();
        });
}
```

#### Key Requirements

1. **Preprocessor Directives**: All property tests must have `#if FAST_TESTS` / `#else` / `#endif` directives
2. **MaxTest Values**: 10 for FAST_TESTS, 100 for else block
3. **Trait Attributes**: Include Feature and Property traits
4. **Descriptive Names**: Use clear, descriptive test method names

#### Automation Script

If you forget to add preprocessor directives, use the automation script:

```powershell
# Preview what will be changed
.\Scripts\Add-PropertyTestDirectives.ps1 -WhatIf

# Apply changes
.\Scripts\Add-PropertyTestDirectives.ps1

# Validate all tests have correct pattern
.\Scripts\Validate-PropertyTests.ps1
```

#### Examples of Good Property Tests

**Round-trip property:**
```csharp
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
[Trait("Feature", "configuration")]
[Trait("Property", "Property 1: Serialization round-trip preserves data")]
public Property Configuration_RoundTrip()
{
    return Prop.ForAll(
        GenerateValidConfiguration(),
        config =>
        {
            var json = JsonSerializer.Serialize(config);
            var deserialized = JsonSerializer.Deserialize<Configuration>(json);
            return config.Equals(deserialized);
        });
}
```

**Invariant property:**
```csharp
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
[Trait("Feature", "task-list")]
[Trait("Property", "Property 2: Adding task increases length by one")]
public Property AddTask_IncreasesLength()
{
    return Prop.ForAll(
        Arb.From<NonEmptyString>(),
        taskDescription =>
        {
            var list = new TaskList();
            var initialLength = list.Count;
            list.Add(taskDescription);
            return list.Count == initialLength + 1;
        });
}
```

**Error handling property:**
```csharp
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
[Trait("Feature", "validation")]
[Trait("Property", "Property 3: Invalid input returns error")]
public Property Validation_RejectsInvalidInput()
{
    return Prop.ForAll(
        GenerateInvalidInput(),
        invalidInput =>
        {
            var result = Validator.Validate(invalidInput);
            return result.IsError;
        });
}
```

### Writing Unit Tests

Unit tests should be clear and focused:

```csharp
[Fact]
public void Constructor_WithValidParameters_CreatesInstance()
{
    // Arrange
    var param1 = "test";
    var param2 = 42;
    
    // Act
    var instance = new MyClass(param1, param2);
    
    // Assert
    Assert.NotNull(instance);
    Assert.Equal(param1, instance.Property1);
    Assert.Equal(param2, instance.Property2);
}
```

### Running Tests Locally

**During active development:**
```bash
dotnet test --configuration FastTests
```
This runs quickly (10 iterations per property test) for rapid feedback.

**Before committing:**
```bash
dotnet test --configuration Release
```
This runs thoroughly (100 iterations per property test) to catch edge cases.

### Test Validation

Before committing, validate that all property tests have correct directives:

```powershell
.\Scripts\Validate-PropertyTests.ps1
```

If validation fails, run the automation script to fix:

```powershell
.\Scripts\Add-PropertyTestDirectives.ps1
```

## Code Style

### General Guidelines

- Use meaningful variable and method names
- Keep methods focused and small
- Add comments for complex logic
- Follow C# naming conventions:
  - PascalCase for classes, methods, properties
  - camelCase for local variables and parameters
  - _camelCase for private fields

### Example

```csharp
public class ProfileManager
{
    private readonly IProfileRepository _repository;
    private readonly ILogger _logger;
    
    public ProfileManager(IProfileRepository repository, ILogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Profile> LoadProfileAsync(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be empty", nameof(profileName));
        }
        
        _logger.LogInformation("Loading profile: {ProfileName}", profileName);
        
        try
        {
            return await _repository.GetByNameAsync(profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile: {ProfileName}", profileName);
            throw;
        }
    }
}
```

## Documentation

### Code Documentation

- Add XML documentation comments for public APIs
- Document complex algorithms or non-obvious logic
- Include examples in documentation when helpful

```csharp
/// <summary>
/// Loads a profile by name from the repository.
/// </summary>
/// <param name="profileName">The name of the profile to load.</param>
/// <returns>The loaded profile.</returns>
/// <exception cref="ArgumentException">Thrown when profileName is null or empty.</exception>
/// <exception cref="ProfileNotFoundException">Thrown when profile is not found.</exception>
public async Task<Profile> LoadProfileAsync(string profileName)
{
    // Implementation
}
```

### User Documentation

When adding user-facing features:

1. Update README.md with usage instructions
2. Add screenshots or GIFs for UI changes
3. Update version history
4. Consider adding troubleshooting section

## Pull Request Process

### Before Submitting

1. **Run all tests**
   ```bash
   dotnet test --configuration Release
   ```

2. **Validate property tests**
   ```powershell
   .\Scripts\Validate-PropertyTests.ps1
   ```

3. **Build successfully**
   ```bash
   dotnet build --configuration Release
   ```

4. **Update documentation** if needed

### PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] All tests pass locally
- [ ] Added tests for new functionality
- [ ] Property tests have correct directives

## Screenshots (if applicable)
Add screenshots for UI changes

## Related Issues
Fixes #123
```

### Review Process

1. Automated checks run (build, tests, validation)
2. Code review by maintainers
3. Address feedback
4. Approval and merge

## CI/CD Pipeline

### Automated Checks

When you create a PR, the CI pipeline automatically:

1. **Validates property tests** have correct preprocessor directives
2. **Builds** the project with FastTests configuration
3. **Runs tests** with 10 iterations per property test (fast feedback)
4. **Reports results** in the PR

### After Merge

When your PR is merged to main:

1. **Builds** with Release configuration
2. **Runs tests** with 100 iterations per property test (thorough validation)
3. **Creates release** if version changed
4. **Builds MSI installer**

### If CI Fails

**Validation failure:**
- Run `.\Scripts\Add-PropertyTestDirectives.ps1` locally
- Commit the updated test files
- Push to your branch

**Test failure:**
- Check which configuration was used (FastTests or Release)
- Run locally with same configuration
- Fix the failing test or code
- Push the fix

**Build failure:**
- Check build logs for errors
- Fix compilation errors
- Push the fix

## Common Tasks

### Adding a New Feature

1. Create feature branch
2. Implement feature with tests
3. Update documentation
4. Run full test suite
5. Create PR

### Fixing a Bug

1. Create fix branch
2. Add test that reproduces bug
3. Fix the bug
4. Verify test passes
5. Create PR

### Updating Dependencies

1. Update package references
2. Test thoroughly
3. Update documentation if APIs changed
4. Create PR

## Getting Help

- **Questions**: Open a [GitHub Discussion](https://github.com/gadams999/obrl/discussions)
- **Bugs**: Open a [GitHub Issue](https://github.com/gadams999/obrl/issues)
- **Documentation**: Check README.md, PROPERTY_TESTING.md, CI_CD_SETUP.md

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on the code, not the person
- Help others learn and grow

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

