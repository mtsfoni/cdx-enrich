# cdx-enrich Development Guide

## Project Overview

cdx-enrich is a .NET 8 CLI tool that enriches CycloneDX SBOMs (Software Bill of Materials) with predefined data from YAML configuration files. It's designed as a pipeline step between SBOM generation and upload to Dependency-Track.

**Tech Stack:** .NET 8, C# with nullable reference types enabled, NUnit for testing

## Build, Test, and Run Commands

All commands should be run from the `/src` directory:

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release

# Run a specific test
dotnet test --filter "FullyQualifiedName~ReplaceLicenseByBomRefTest"

# Run the tool locally
dotnet run --project CdxEnrich/CdxEnrich.csproj -- <args>

# Example: Enrich an SBOM
dotnet run --project CdxEnrich/CdxEnrich.csproj -- input.json -c config.yaml
```

## Architecture

### Functional Programming Pattern

This codebase uses a functional programming approach with a custom `Result<T>` type for error handling:

- **`Result<T>`**: Base abstract class representing either success (`Ok<T>`) or failure (`Error<T>`)
- **`Ok<T>`**: Contains successful data
- **`Error<T>`**: Contains error information via `IErrorType`
- **`Failure` interface**: Implemented by error results to expose `ErrorType` and `ErrorMessage`

**Key methods on Result:**
- `.Map()`: Transform data if successful
- `.Bind()`: Chain operations that return Result types

### Pipeline Architecture

The enrichment process follows a functional pipeline (see `Runner.cs`):

1. **Parse** BOM and config files
2. **Combine** into `InputTuple` (validated)
3. **Execute Actions** (each action returns modified `InputTuple`):
   - `ReplaceLicenseByBomRef.Execute()`
   - `ReplaceLicensesByUrl.Execute()`
4. **Serialize** back to XML/JSON

### Adding New Actions

New enrichment actions should follow this structure (use existing actions as templates):

1. Create a new file in `src/CdxEnrich/Actions/`
2. Implement:
   - `CheckConfig(ConfigRoot)` - Validates configuration
   - `Execute(InputTuple)` - Performs the transformation
3. Add the action to the pipeline in `Runner.Enrich()`
4. Update `ConfigRoot` model in `src/CdxEnrich/Config/Model.cs`
5. Add tests in `src/CdxEnrich.Tests/Actions/<ActionName>/`

Each action operates on the entire BOM and config, returning a modified `InputTuple`.

## Project Structure

```
src/
├── CdxEnrich/                    # Main CLI tool
│   ├── Actions/                  # Enrichment action implementations
│   ├── Config/                   # YAML config parsing and models
│   ├── Serialization/            # BOM serialization (XML/JSON)
│   ├── FunctionalHelpers/        # Result type and functional utilities
│   ├── Program.cs                # CLI argument parsing (System.CommandLine)
│   └── Runner.cs                 # Main enrichment pipeline
└── CdxEnrich.Tests/              # NUnit tests
    ├── Actions/                  # Action-specific tests
    │   └── */testcases/          # Test input files (configs, BOMs, snapshots)
    └── General/                  # Integration tests
```

## Key Conventions

### Test Organization

- Tests use **Verify** for snapshot testing (verify enriched SBOM output)
- Test cases are organized in `testcases/` subdirectories:
  - `configs/` - YAML configuration files
  - `boms/` - Input SBOM files
  - `snapshots/` - Expected output (managed by Verify)
- Invalid configs are prefixed with `invalid*` in filename
- Use `[TestCaseSource]` to run tests against multiple files

### Error Handling

- Never use exceptions for expected errors (e.g., invalid config, missing files)
- Use `Result<T>` pattern throughout the pipeline
- Only catch exceptions at the CLI boundary (`Runner.Enrich()` public method)
- Error messages should be user-friendly and actionable

### Code Style

- **Namespace declarations**: Block-scoped (not file-scoped)
- **Primary constructors**: Preferred for simple classes
- **Expression bodies**: Use for properties/lambdas, avoid for methods/constructors
- See `.editorconfig` for complete style rules
- SonarAnalyzer is enabled; S1944 (invalid casts) is disabled for the Result pattern

### Naming

- Actions are named with verbs (e.g., `ReplaceLicensesByURL`)
- Config properties match action names exactly
- Use `moduleName` constant in actions for error reporting

## Working Directory Note

The solution file and all projects are in `src/`, not the repository root. CI/CD workflows set `working-directory: ./src`.
