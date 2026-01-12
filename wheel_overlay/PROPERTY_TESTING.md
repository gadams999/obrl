# Property-Based Testing Guide

## Overview

This project uses property-based testing (PBT) with [FsCheck](https://fscheck.github.io/FsCheck/) to validate that code behaves correctly across a wide range of inputs. Unlike traditional unit tests that check specific examples, property tests verify that certain properties hold true for all valid inputs.

## Iteration Control Mechanism

### How It Works

Property tests in this project use conditional compilation to control the number of test iterations based on build configuration. This allows fast feedback during development while maintaining thorough validation before merging code.

**Build Configurations:**
- **Debug**: 100 iterations (default for local development)
- **Release**: 100 iterations (used for production builds and merges)
- **FastTests**: 10 iterations (used for PR checks and rapid development)

The `FastTests` configuration defines the `FAST_TESTS` preprocessor symbol, which property tests use to conditionally set iteration counts.

### Preprocessor Directive Pattern

Every property test must follow this pattern:

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
            // Test logic here
            var result = SystemUnderTest.Method(input);
            return result.MeetsExpectation();
        });
}
```

**Key Elements:**
1. **Conditional Compilation**: `#if FAST_TESTS` / `#else` / `#endif` directives
2. **MaxTest Values**: 10 for fast tests, 100 for thorough tests
3. **Trait Attributes**: Feature and Property traits for test organization
4. **Property Method**: Returns `Property` type from FsCheck

## Adding New Property Tests

### Step 1: Write the Test

Create your property test following the standard pattern:

```csharp
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
[Trait("Feature", "my-feature")]
[Trait("Property", "Property 1: Description of what this validates")]
public Property MyProperty_ShouldHoldForAllInputs()
{
    return Prop.ForAll(
        Arb.From<MyInputType>(),
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

### Step 2: Verify Directives

Run the validation script to ensure your test has the correct pattern:

```powershell
.\Scripts\Validate-PropertyTests.ps1
```

If your test is missing directives, the script will report it.

### Step 3: Add Directives Automatically (if needed)

If you forgot to add directives, use the automation script:

```powershell
# Preview changes
.\Scripts\Add-PropertyTestDirectives.ps1 -WhatIf

# Apply changes
.\Scripts\Add-PropertyTestDirectives.ps1
```

## Running Property Tests

### Local Development

**Fast feedback (10 iterations):**
```bash
dotnet test --configuration FastTests
```

**Thorough validation (100 iterations):**
```bash
dotnet test --configuration Debug
# or
dotnet test --configuration Release
```

### CI/CD Pipeline

- **Pull Requests**: Automatically use FastTests configuration (10 iterations)
- **Merges to Main**: Automatically use Release configuration (100 iterations)

## Troubleshooting

### Issue: Test Missing Preprocessor Directives

**Symptom:** Build fails with validation error listing test files

**Solution:**
1. Run `.\Scripts\Add-PropertyTestDirectives.ps1 -WhatIf` to preview changes
2. Run `.\Scripts\Add-PropertyTestDirectives.ps1` to apply directives
3. Rebuild and verify tests pass

### Issue: Configuration Not Detected

**Symptom:** Tests always run with same iteration count regardless of configuration

**Solution:**
1. Verify you're specifying configuration: `dotnet test --configuration FastTests`
2. Check that `FAST_TESTS` symbol is defined in WheelOverlay.Tests.csproj:
   ```xml
   <PropertyGroup Condition="'$(Configuration)' == 'FastTests'">
     <DefineConstants>FAST_TESTS</DefineConstants>
   </PropertyGroup>
   ```
3. Clean and rebuild: `dotnet clean && dotnet build --configuration FastTests`

### Issue: Inconsistent Test Results

**Symptom:** Test passes with 10 iterations but fails with 100 iterations

**Solution:**
This is expected behavior - more iterations find more edge cases. The failing case indicates a real bug:
1. Note the failing input from the test output
2. Add a unit test for that specific case
3. Fix the code to handle the edge case
4. Verify both unit test and property test pass

### Issue: Tests Too Slow

**Symptom:** Property tests take too long to run

**Solution:**
1. Use FastTests configuration during development: `dotnet test --configuration FastTests`
2. Only run full 100 iterations before committing or when investigating failures
3. Consider if your test is doing expensive operations that could be optimized

### Issue: Custom Iteration Count Needed

**Symptom:** A specific test needs different iteration counts (e.g., 50 instead of 10/100)

**Solution:**
You can override for specific tests, but document why:

```csharp
// Note: This test uses custom iteration count because [reason]
#if FAST_TESTS
[Property(MaxTest = 5)]  // Custom: reduced due to expensive operation
#else
[Property(MaxTest = 50)]  // Custom: reduced due to expensive operation
#endif
[Trait("Feature", "my-feature")]
[Trait("Property", "Property N: Expensive operation")]
public Property ExpensiveProperty()
{
    // Test implementation
}
```

## Best Practices

### 1. Write Meaningful Property Descriptions

Use the Property trait to describe what the test validates:

```csharp
[Trait("Property", "Property 1: Adding a task increases list length by one")]
```

### 2. Use Appropriate Generators

FsCheck provides generators for common types. Use them to create realistic test data:

```csharp
// Good: Uses FsCheck's built-in generators
Prop.ForAll(
    Arb.From<NonEmptyString>(),
    Arb.From<PositiveInt>(),
    (name, count) => { /* test logic */ });

// Better: Create custom generators for domain types
Prop.ForAll(
    GenerateValidProfile(),
    profile => { /* test logic */ });
```

### 3. Keep Properties Simple

Each property should test one thing:

```csharp
// Good: Tests one property
[Property]
public Property AddingTask_IncreasesLength()
{
    return Prop.ForAll(
        Arb.From<TaskDescription>(),
        desc => {
            var list = new TaskList();
            var initialLength = list.Count;
            list.Add(desc);
            return list.Count == initialLength + 1;
        });
}

// Avoid: Tests multiple properties
[Property]
public Property AddingTask_IncreasesLengthAndPreservesOrder()
{
    // Testing two things makes failures harder to diagnose
}
```

### 4. Use Descriptive Test Names

Test names should clearly indicate what property is being validated:

```csharp
// Good
public Property RoundTrip_SerializeDeserialize_PreservesData()

// Good
public Property EmptyInput_ReturnsEmptyResult()

// Avoid
public Property Test1()
```

### 5. Document Complex Properties

If a property is non-obvious, add a comment explaining the invariant:

```csharp
// Property: For any valid configuration, serializing then deserializing
// should produce an equivalent configuration (round-trip property)
#if FAST_TESTS
[Property(MaxTest = 10)]
#else
[Property(MaxTest = 100)]
#endif
public Property Configuration_RoundTrip()
{
    // Implementation
}
```

## Understanding Property-Based Testing

### What is a Property?

A property is a statement that should be true for all valid inputs. Examples:

- **Round-trip property**: `deserialize(serialize(x)) == x`
- **Invariant property**: `sort(list).length == list.length`
- **Metamorphic property**: `reverse(reverse(list)) == list`
- **Error handling property**: `parse(invalidInput).isError == true`

### Why Use Property-Based Testing?

**Advantages:**
- Finds edge cases you didn't think of
- Tests many inputs automatically
- Documents system invariants
- Complements unit tests

**When to Use:**
- Testing pure functions
- Validating data transformations
- Checking invariants
- Testing serialization/deserialization
- Validating parsers and formatters

**When Not to Use:**
- Testing UI interactions (use unit tests)
- Testing external dependencies (use integration tests)
- Testing specific business rules (use unit tests)

## Additional Resources

- [FsCheck Documentation](https://fscheck.github.io/FsCheck/)
- [Property-Based Testing Introduction](https://fsharpforfunandprofit.com/posts/property-based-testing/)
- [Choosing Properties for Property-Based Testing](https://fsharpforfunandprofit.com/posts/property-based-testing-2/)

