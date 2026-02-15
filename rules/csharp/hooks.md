---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C# Hooks

> This file extends [common/hooks.md](../common/hooks.md) with C# specific content.

## PostToolUse Hooks

Configure in `~/.claude/settings.json` or hooks configuration:

### Auto-Format with dotnet format

```json
{
  "hooks": {
    "postToolUse": [
      {
        "name": "dotnet-format",
        "pattern": ".**\\.cs$",
        "command": "dotnet format --include {file}",
        "description": "Auto-format C# files after edit"
      }
    ]
  }
}
```

### Run Roslyn Analyzers

```json
{
  "hooks": {
    "postToolUse": [
      {
        "name": "dotnet-build-check",
        "pattern": ".**\\.cs$",
        "command": "dotnet build --no-incremental /p:TreatWarningsAsErrors=true",
        "description": "Run build with analyzer warnings as errors"
      }
    ]
  }
}
```

### StyleCop Analysis

```json
{
  "hooks": {
    "postToolUse": [
      {
        "name": "stylecop-check",
        "pattern": ".**\\.cs$",
        "command": "dotnet build --no-restore -p:EnforceCodeStyleInBuild=true",
        "description": "Run StyleCop analyzers"
      }
    ]
  }
}
```

## PreCommit Hooks

### Run Tests Before Commit

```bash
#!/bin/bash
# .git/hooks/pre-commit

echo "Running tests before commit..."
dotnet test --no-build --verbosity quiet

if [ $? -ne 0 ]; then
  echo "Tests failed. Commit aborted."
  exit 1
fi

echo "All tests passed."
```

### Check Code Coverage

```bash
#!/bin/bash
# .git/hooks/pre-commit

echo "Checking code coverage..."
dotnet test --collect:"XPlat Code Coverage" --verbosity quiet

# Parse coverage report and check threshold
COVERAGE=$(grep -oP 'line-rate="\K[^"]+' coverage.cobertura.xml | head -1)
THRESHOLD=0.80

if (( $(echo "$COVERAGE < $THRESHOLD" | bc -l) )); then
  echo "Code coverage below 80% threshold. Commit aborted."
  exit 1
fi

echo "Code coverage: ${COVERAGE}%"
```

### Format Check

```bash
#!/bin/bash
# .git/hooks/pre-commit

echo "Checking code formatting..."
dotnet format --verify-no-changes --verbosity quiet

if [ $? -ne 0 ]; then
  echo "Code formatting issues found. Run 'dotnet format' to fix."
  exit 1
fi

echo "Code formatting OK."
```

## Stop Hooks

### Security Audit Before Session End

```json
{
  "hooks": {
    "stop": [
      {
        "name": "security-scan",
        "command": "dotnet list package --vulnerable --include-transitive",
        "description": "Check for vulnerable package dependencies"
      }
    ]
  }
}
```

### Final Build Verification

```json
{
  "hooks": {
    "stop": [
      {
        "name": "final-build",
        "command": "dotnet build --configuration Release",
        "description": "Verify release build succeeds"
      }
    ]
  }
}
```

## Project Configuration

### Enable Analyzers in .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Code Analysis -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisMode>All</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- StyleCop Analyzers -->
    <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    
    <!-- Security Analyzers -->
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### .editorconfig for Consistent Formatting

```ini
# .editorconfig
root = true

[*.cs]
# Indentation
indent_style = space
indent_size = 4

# New line preferences
end_of_line = lf
insert_final_newline = true
charset = utf-8

# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# this. preferences
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:suggestion
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:suggestion

# Expression-level preferences
dotnet_style_prefer_auto_properties = true:warning
dotnet_style_prefer_conditional_expression_over_assignment = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion

# Null-checking preferences
dotnet_style_coalesce_expression = true:warning
dotnet_style_null_propagation = true:warning

# C# Code Style Rules
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_prefer_method_group_conversion = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = false:suggestion
csharp_style_expression_bodied_operators = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = when_on_single_line:suggestion

# Pattern matching preferences
csharp_style_pattern_matching_over_is_with_cast_check = true:warning
csharp_style_pattern_matching_over_as_with_null_check = true:warning
csharp_style_prefer_switch_expression = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_prefer_not_pattern = true:suggestion

# Var preferences
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
```

## Continuous Integration Hooks

### GitHub Actions Workflow

```yaml
# .github/workflows/dotnet.yml
name: .NET CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Check formatting
      run: dotnet format --verify-no-changes
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Code Coverage Report
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
        
    - name: Security Scan
      run: dotnet list package --vulnerable --include-transitive
```

## Agent Support

- **build-error-resolver** - Use when hooks detect build errors
- **csharp-reviewer** - Integrates with formatting and analysis hooks
