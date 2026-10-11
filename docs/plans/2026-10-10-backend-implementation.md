# Backend — Control de parqueaderos de visitantes · Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** API ASP.NET Core (.NET 10) que registra ingresos y salidas de vehículos visitantes garantizando R1–R7, con autenticación JWT, sobre PostgreSQL 17.

**Architecture:** DDD + Clean + Hexagonal. `Domain` (agregado `ParkingSpot` con `ParkingAssignment` interna, VOs, `Result`/`Error`) ← `Application` (handlers de escritura + puertos) ← `Infrastructure` (EF Core/Npgsql, identidad, JWT) / `Api` (Minimal APIs, ProblemDetails, composition root). Concurrencia: versión del agregado (`xmin`) + índices únicos parciales.

**Tech Stack:** .NET 10.0.401 · ASP.NET Core Minimal APIs · EF Core + Npgsql + EFCore.NamingConventions · JWT Bearer + `JsonWebTokenHandler` · `PasswordHasher<T>` · xUnit (v2) · Shouldly · `FakeTimeProvider` · `WebApplicationFactory`.

**Spec:** `CLAUDE.md`, `docs/REQUIREMENTS.md`, `docs/DOMAIN.md`, `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`, ADR-0001…0005.

## Global Constraints

- Working directory for all commands: repo root `prueba-domonow/`. Backend lives in `backend/`.
- Branch: `feat/backend` (created in Task 1), merged into `main` with `--no-ff` at the end.
- Central Package Management is ON (`backend/Directory.Packages.props`): `PackageReference` **never** has `Version`; add packages with `dotnet add <proj> package <id>` (the SDK writes the version into `Directory.Packages.props`). Use the latest stable version the command resolves.
- `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`. Every task ends with `dotnet build backend/DomoNow.Parking.slnx` → `0 Warning(s) 0 Error(s)`. If an analyzer rule fires: fix the code; only if the rule is wrong for this context, suppress it in `.editorconfig` with a `#` comment explaining why, and note it in `docs/PROGRESS.md`.
- File-scoped namespaces; `sealed` by default; no `!` without a justifying comment; `CancellationToken` on every public async method; no `DateTime.Now`/`UtcNow` (use `TimeProvider`).
- Error codes `Parking.<Reason>` in English; **error messages in Spanish** (shown to the operator as-is).
- Enums persisted as strings; dates as `DateTimeOffset` UTC (`timestamptz`); snake_case naming in DB.
- Plate format after normalization: car `^[A-Z]{3}[0-9]{3}$`, motorcycle `^[A-Z]{3}[0-9]{2}[A-Z]$`. Normalization: keep only letters/digits, upper-invariant. Raw input longer than 15 chars is invalid.
- `SpotCode` format `^P[0-9]{2}$`. `VisitorName` trimmed 2–100. `DestinationUnit` trimmed 1–20.
- Secrets (connection strings, JWT key, seed operator password) only via `dotnet user-secrets` or env vars. Never in tracked files.
- HTTP mapping: Validation→400, Unauthorized→401, NotFound→404, Conflict→409, BusinessRule→422. Bodies are RFC 9457 ProblemDetails with extension `code`.
- Commit messages: Conventional Commits in English, ending with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- After each task: append an entry to `docs/PROGRESS.md` (bitácora + tablero). If an AI proposal was accepted/modified/rejected by the engineer during the task, add a row to `AI-USAGE.md` § Registro continuo.

## Review Focus

1. **Two simultaneous check-ins on the same spot** → exactly one `201`, the rest `409 Parking.SpotAlreadyTaken`/`Parking.SpotOccupied`; never two active rows. (Task 9 concurrency test.)
2. **Same plate checked in on two different spots at the same time** → one `201`, one `409 Parking.VehicleAlreadyInside`. (Task 9 concurrency test.)
3. **Plate inputs with separators, lowercase, spaces or garbage** (`" abc-123 "`, `"abc.12d"`, `"AB-12"`, `"ÑAB123"`, 200-char string) → normalized or `400 Parking.InvalidLicensePlate`, never a 500. (Task 2 theory tests.)
4. **Checkout of an already-finished or nonexistent assignment, or with `checkedOutAt` before check-in / in the future** → `404`/`422`, spot state unchanged. (Task 3 + Task 4 + Task 9.)
5. **Missing/expired/forged JWT or a token without the `Operator` role on write endpoints** → `401`/`403`, never data. (Task 9 auth tests.)

---

## File Structure

```
backend/
├── DomoNow.Parking.slnx
├── .config/dotnet-tools.json                         (dotnet-ef local tool)
├── src/
│   ├── DomoNow.Parking.Domain/
│   │   ├── Common/ Error.cs · ErrorType.cs · Result.cs · Entity.cs · AggregateRoot.cs
│   │   └── ParkingSpots/ ParkingSpot.cs · ParkingAssignment.cs · ParkingSpotStatus.cs · AssignmentStatus.cs
│   │                     SpotCode.cs · LicensePlate.cs · VisitorName.cs · DestinationUnit.cs · ParkingErrors.cs
│   ├── DomoNow.Parking.Application/
│   │   ├── Abstractions/ IParkingSpotRepository.cs · IUnitOfWork.cs
│   │   ├── ParkingAssignments/CheckIn/ CheckInCommand.cs · CheckInResponse.cs · CheckInHandler.cs
│   │   ├── ParkingAssignments/CheckOut/ CheckOutCommand.cs · CheckOutResponse.cs · CheckOutHandler.cs
│   │   ├── Queries/ IParkingReadModel.cs · ParkingSpotDto.cs · ActiveAssignmentDto.cs
│   │   ├── Authentication/ Roles.cs · AuthErrors.cs · LoginCommand.cs · LoginResponse.cs · LoginHandler.cs
│   │   │                   IOperatorCredentialStore.cs · IAccessTokenIssuer.cs · AuthenticatedOperator.cs · AccessToken.cs
│   │   └── DependencyInjection.cs
│   ├── DomoNow.Parking.Infrastructure/
│   │   ├── Persistence/ ParkingDbContext.cs · UnitOfWork.cs · DatabaseInitializer.cs · DatabaseOptions.cs
│   │   │   ├── Configurations/ ParkingSpotConfiguration.cs · ParkingAssignmentConfiguration.cs · OperatorConfiguration.cs
│   │   │   └── Migrations/ (generated)
│   │   ├── Repositories/ ParkingSpotRepository.cs · ParkingReadModel.cs
│   │   ├── Identity/ Operator.cs · OperatorCredentialStore.cs · JwtOptions.cs · JwtAccessTokenIssuer.cs · SeedOperatorOptions.cs
│   │   └── DependencyInjection.cs
│   └── DomoNow.Parking.Api/
│       ├── Program.cs · appsettings.json · appsettings.Development.json
│       ├── ErrorHandling/ ErrorResults.cs · GlobalExceptionHandler.cs
│       ├── Security/ AuthenticationSetup.cs
│       └── Endpoints/ AuthEndpoints.cs · ParkingSpotEndpoints.cs · ParkingAssignmentEndpoints.cs · Contracts.cs
└── tests/
    ├── DomoNow.Parking.Domain.UnitTests/ ValueObjectTests.cs · ParkingSpotTests.cs
    ├── DomoNow.Parking.Application.UnitTests/ Fakes.cs · CheckInHandlerTests.cs · CheckOutHandlerTests.cs · LoginHandlerTests.cs
    └── DomoNow.Parking.Api.IntegrationTests/ ParkingApiFactory.cs · ApiClientExtensions.cs · AuthTests.cs · ParkingFlowTests.cs · ConcurrencyTests.cs
```

---

### Task 1: Scaffold solution, projects and tooling

**Files:**
- Create: `backend/DomoNow.Parking.slnx`, 4 `src` projects, 3 `tests` projects, `backend/.config/dotnet-tools.json`
- Modify: `backend/Directory.Packages.props` (by `dotnet add package`), `.editorconfig` (generated-code section)

**Interfaces:**
- Produces: project references `Application→Domain`, `Infrastructure→Application`, `Api→Application,Infrastructure`, `Domain.UnitTests→Domain`, `Application.UnitTests→Application`, `Api.IntegrationTests→Api`.

- [ ] **Step 1: Create branch**

```bash
git checkout -b feat/backend
```

- [ ] **Step 2: Create solution and projects**

```bash
cd backend
dotnet new sln -n DomoNow.Parking
dotnet new classlib -n DomoNow.Parking.Domain -o src/DomoNow.Parking.Domain
dotnet new classlib -n DomoNow.Parking.Application -o src/DomoNow.Parking.Application
dotnet new classlib -n DomoNow.Parking.Infrastructure -o src/DomoNow.Parking.Infrastructure
dotnet new web -n DomoNow.Parking.Api -o src/DomoNow.Parking.Api
dotnet new classlib -n DomoNow.Parking.Domain.UnitTests -o tests/DomoNow.Parking.Domain.UnitTests
dotnet new classlib -n DomoNow.Parking.Application.UnitTests -o tests/DomoNow.Parking.Application.UnitTests
dotnet new classlib -n DomoNow.Parking.Api.IntegrationTests -o tests/DomoNow.Parking.Api.IntegrationTests
rm src/*/Class1.cs tests/*/Class1.cs
dotnet sln DomoNow.Parking.slnx add src/*/*.csproj tests/*/*.csproj
```

Expected: `DomoNow.Parking.slnx` exists (the .NET 10 SDK creates `.slnx` by default; if it created `.sln`, run `dotnet sln DomoNow.Parking.sln migrate` and delete the `.sln`).

- [ ] **Step 3: Remove per-project properties duplicated by `Directory.Build.props`**

In every generated `.csproj`, delete the `<TargetFramework>`, `<ImplicitUsings>` and `<Nullable>` lines (they come from `Directory.Build.props`). Keep `<Project Sdk="...">`.

- [ ] **Step 4: Project references**

```bash
dotnet add src/DomoNow.Parking.Application reference src/DomoNow.Parking.Domain
dotnet add src/DomoNow.Parking.Infrastructure reference src/DomoNow.Parking.Application
dotnet add src/DomoNow.Parking.Api reference src/DomoNow.Parking.Application src/DomoNow.Parking.Infrastructure
dotnet add tests/DomoNow.Parking.Domain.UnitTests reference src/DomoNow.Parking.Domain
dotnet add tests/DomoNow.Parking.Application.UnitTests reference src/DomoNow.Parking.Application
dotnet add tests/DomoNow.Parking.Api.IntegrationTests reference src/DomoNow.Parking.Api
```

- [ ] **Step 5: Packages**

```bash
dotnet add src/DomoNow.Parking.Application package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/DomoNow.Parking.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/DomoNow.Parking.Infrastructure package EFCore.NamingConventions
dotnet add src/DomoNow.Parking.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
dotnet add src/DomoNow.Parking.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/DomoNow.Parking.Api package Microsoft.AspNetCore.OpenApi
dotnet add src/DomoNow.Parking.Api package Microsoft.EntityFrameworkCore.Design
for p in tests/*; do
  dotnet add $p package Microsoft.NET.Test.Sdk
  dotnet add $p package xunit
  dotnet add $p package xunit.runner.visualstudio
  dotnet add $p package Shouldly
done
dotnet add tests/DomoNow.Parking.Application.UnitTests package Microsoft.Extensions.TimeProvider.Testing
dotnet add tests/DomoNow.Parking.Api.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/DomoNow.Parking.Api.IntegrationTests package Microsoft.Extensions.Configuration.UserSecrets
```

Verify `Directory.Packages.props` now lists every package with a `Version` and no `.csproj` has `Version=` on a `PackageReference`.

- [ ] **Step 6: Infrastructure needs the ASP.NET Core shared framework** (for `PasswordHasher<T>`, options, configuration). Add to `src/DomoNow.Parking.Infrastructure/DomoNow.Parking.Infrastructure.csproj`:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

- [ ] **Step 7: Test project settings.** In each `tests/*/*.csproj` add:

```xml
<PropertyGroup>
  <IsPackable>false</IsPackable>
  <IsTestProject>true</IsTestProject>
</PropertyGroup>
<ItemGroup>
  <Using Include="Xunit" />
  <Using Include="Shouldly" />
</ItemGroup>
```

In `tests/DomoNow.Parking.Api.IntegrationTests/*.csproj` also add `<UserSecretsId>domonow-parking-integration-tests</UserSecretsId>` inside the `PropertyGroup`.

- [ ] **Step 8: Treat EF migrations as generated code.** Append to repo-root `.editorconfig`:

```ini
# Migraciones generadas por EF Core: no se les aplica el estilo del proyecto
[backend/src/DomoNow.Parking.Infrastructure/Persistence/Migrations/*.cs]
generated_code = true
```

- [ ] **Step 9: dotnet-ef local tool**

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

- [ ] **Step 10: Build**

Run: `dotnet build DomoNow.Parking.slnx`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. (The `web` template's `Program.cs` is replaced in Task 8; if it trips an analyzer now, apply the Global Constraints rule.)

- [ ] **Step 11: Commit**

```bash
cd ..
git add backend .editorconfig
git commit -m "chore(backend): scaffold solution with clean architecture projects" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Domain kernel and value objects (R4)

**Files:**
- Create: `backend/src/DomoNow.Parking.Domain/Common/{ErrorType,Error,Result,Entity,AggregateRoot}.cs`
- Create: `backend/src/DomoNow.Parking.Domain/ParkingSpots/{ParkingErrors,SpotCode,LicensePlate,VisitorName,DestinationUnit}.cs`
- Test: `backend/tests/DomoNow.Parking.Domain.UnitTests/ValueObjectTests.cs`

**Interfaces:**
- Produces:
  - `enum ErrorType { Validation, NotFound, Conflict, BusinessRule, Unauthorized }`
  - `sealed record Error(string Code, string Message, ErrorType Type)` + static factories `Validation/NotFound/Conflict/BusinessRule/Unauthorized(string code, string message)`
  - `class Result { bool IsSuccess; bool IsFailure; Error Error; static Result Success(); static Result Failure(Error); static Result<T> Success<T>(T); static Result<T> Failure<T>(Error); implicit from Error }`
  - `sealed class Result<TValue> : Result { TValue Value; implicit from TValue; implicit from Error }`
  - `abstract class Entity { Guid Id }`, `abstract class AggregateRoot : Entity`
  - VOs (`sealed record`, `string Value`, `ToString()=>Value`): `SpotCode.Create(string?)`, `LicensePlate.Create(string?)`, `VisitorName.Create(string?)`, `DestinationUnit.Create(string?)` → `Result<VO>`

- [ ] **Step 1: Write the failing tests** — `backend/tests/DomoNow.Parking.Domain.UnitTests/ValueObjectTests.cs`

```csharp
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Domain.UnitTests;

public sealed class LicensePlateTests
{
    [Theory]
    [InlineData("abc-123", "ABC123")]
    [InlineData(" ABC 123 ", "ABC123")]
    [InlineData("abc.12d", "ABC12D")]
    [InlineData("ABC123", "ABC123")]
    public void Create_WithValidInput_StoresNormalizedValue(string raw, string expected)
    {
        var result = LicensePlate.Create(raw);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenMissing_ReturnsRequiredError(string? raw)
    {
        var result = LicensePlate.Create(raw);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Parking.LicensePlateRequired");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Theory]
    [InlineData("AB-12")]
    [InlineData("ABCD123")]
    [InlineData("123ABC")]
    [InlineData("ABC1234")]
    [InlineData("ÑAB123")]
    [InlineData("ABC12DD")]
    public void Create_WithInvalidFormat_ReturnsInvalidError(string raw)
    {
        var result = LicensePlate.Create(raw);

        result.Error.Code.ShouldBe("Parking.InvalidLicensePlate");
    }

    [Fact]
    public void Create_WithOversizedInput_ReturnsInvalidError()
    {
        var result = LicensePlate.Create(new string('A', 200));

        result.Error.Code.ShouldBe("Parking.InvalidLicensePlate");
    }

    [Fact]
    public void Plates_WithSameNormalizedValue_AreEqual()
    {
        LicensePlate.Create("abc-123").Value.ShouldBe(LicensePlate.Create("ABC 123").Value);
    }
}

public sealed class SpotCodeTests
{
    [Theory]
    [InlineData("P01", "P01")]
    [InlineData("p15", "P15")]
    public void Create_WithValidCode_Succeeds(string raw, string expected) =>
        SpotCode.Create(raw).Value.Value.ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("P1")]
    [InlineData("X01")]
    [InlineData("P001")]
    public void Create_WithInvalidCode_Fails(string? raw) =>
        SpotCode.Create(raw).Error.Code.ShouldBe("Parking.InvalidSpotCode");
}

public sealed class VisitorNameTests
{
    [Fact]
    public void Create_TrimsValue() =>
        VisitorName.Create("  Carlos Pérez ").Value.Value.ShouldBe("Carlos Pérez");

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("A")]
    public void Create_WhenTooShortOrMissing_Fails(string? raw) =>
        VisitorName.Create(raw).Error.Code.ShouldBe("Parking.InvalidVisitorName");

    [Fact]
    public void Create_WhenLongerThan100_Fails() =>
        VisitorName.Create(new string('a', 101)).Error.Code.ShouldBe("Parking.InvalidVisitorName");
}

public sealed class DestinationUnitTests
{
    [Fact]
    public void Create_TrimsValue() =>
        DestinationUnit.Create(" 302 ").Value.Value.ShouldBe("302");

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_WhenMissing_Fails(string? raw) =>
        DestinationUnit.Create(raw).Error.Code.ShouldBe("Parking.InvalidDestinationUnit");

    [Fact]
    public void Create_WhenLongerThan20_Fails() =>
        DestinationUnit.Create(new string('1', 21)).Error.Code.ShouldBe("Parking.InvalidDestinationUnit");
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/DomoNow.Parking.slnx`
Expected: build FAIL — `LicensePlate`, `SpotCode`, `ErrorType`… not found.

- [ ] **Step 3: Implement the kernel**

`Common/ErrorType.cs`
```csharp
namespace DomoNow.Parking.Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule,
    Unauthorized,
}
```

`Common/Error.cs`
```csharp
namespace DomoNow.Parking.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
}
```

`Common/Result.cs`
```csharp
namespace DomoNow.Parking.Domain.Common;

public class Result
{
    private readonly Error? _error;

    protected Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, null);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, Error? error) : base(error) => _value = value;

    public TValue Value => IsSuccess
        ? _value! // Non-null by construction: successful results are only created with a value.
        : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<TValue>(TValue value) => new(value, null);
    public static implicit operator Result<TValue>(Error error) => new(default, error);
}
```

`Common/Entity.cs`
```csharp
namespace DomoNow.Parking.Domain.Common;

public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    public Guid Id { get; private set; }
}
```

`Common/AggregateRoot.cs`
```csharp
namespace DomoNow.Parking.Domain.Common;

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id)
    {
    }
}
```

- [ ] **Step 4: Implement errors and value objects**

`ParkingSpots/ParkingErrors.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public static class ParkingErrors
{
    public static readonly Error LicensePlateRequired = Error.Validation(
        "Parking.LicensePlateRequired", "La placa del vehículo es obligatoria.");

    public static readonly Error InvalidVisitorName = Error.Validation(
        "Parking.InvalidVisitorName", "El nombre del visitante es obligatorio y debe tener entre 2 y 100 caracteres.");

    public static readonly Error InvalidDestinationUnit = Error.Validation(
        "Parking.InvalidDestinationUnit", "La unidad que visita es obligatoria y debe tener máximo 20 caracteres.");

    public static readonly Error CheckOutBeforeCheckIn = Error.BusinessRule(
        "Parking.CheckOutBeforeCheckIn", "La fecha de salida no puede ser anterior a la fecha de ingreso.");

    public static readonly Error CheckOutInFuture = Error.BusinessRule(
        "Parking.CheckOutInFuture", "La fecha de salida no puede estar en el futuro.");

    public static readonly Error SpotAlreadyTaken = Error.Conflict(
        "Parking.SpotAlreadyTaken", "El parqueadero acaba de ser asignado por otra solicitud. Actualice e intente de nuevo.");

    public static readonly Error VehicleAlreadyInsideConflict = Error.Conflict(
        "Parking.VehicleAlreadyInside", "El vehículo ya se encuentra dentro de la propiedad.");

    public static Error InvalidLicensePlate(string raw) => Error.Validation(
        "Parking.InvalidLicensePlate",
        $"La placa '{Truncate(raw)}' no es válida. Formatos aceptados: ABC123 (carro) o ABC12D (moto).");

    public static Error InvalidSpotCode(string? raw) => Error.Validation(
        "Parking.InvalidSpotCode", $"El código de parqueadero '{Truncate(raw ?? string.Empty)}' no es válido.");

    public static Error SpotNotFound(Guid spotId) => Error.NotFound(
        "Parking.SpotNotFound", $"No existe el parqueadero '{spotId}'.");

    public static Error ActiveAssignmentNotFound(Guid assignmentId) => Error.NotFound(
        "Parking.ActiveAssignmentNotFound", $"No existe una asignación activa con id '{assignmentId}'.");

    public static Error SpotOccupied(SpotCode code) => Error.Conflict(
        "Parking.SpotOccupied", $"El parqueadero {code} ya está ocupado.");

    public static Error SpotOutOfService(SpotCode code) => Error.BusinessRule(
        "Parking.SpotOutOfService", $"El parqueadero {code} está fuera de servicio.");

    public static Error AssignmentNotActive(Guid assignmentId) => Error.Conflict(
        "Parking.AssignmentNotActive", $"La asignación '{assignmentId}' no está activa.");

    public static Error VehicleAlreadyInside(LicensePlate plate) => Error.Conflict(
        "Parking.VehicleAlreadyInside", $"El vehículo {plate} ya se encuentra dentro de la propiedad.");

    private static string Truncate(string value) => value.Length <= 20 ? value : value[..20] + "…";
}
```

`ParkingSpots/LicensePlate.cs`
```csharp
using System.Text.RegularExpressions;
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed partial record LicensePlate
{
    private const int MaxRawLength = 15;

    private LicensePlate(string value) => Value = value;

    public string Value { get; }

    public static Result<LicensePlate> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ParkingErrors.LicensePlateRequired;
        }

        if (raw.Length > MaxRawLength)
        {
            return ParkingErrors.InvalidLicensePlate(raw);
        }

        var normalized = Normalize(raw);
        if (!ValidFormat().IsMatch(normalized))
        {
            return ParkingErrors.InvalidLicensePlate(raw);
        }

        return new LicensePlate(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string raw) =>
        string.Concat(raw.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    // Carro: ABC123 · Moto: ABC12D
    [GeneratedRegex("^[A-Z]{3}([0-9]{3}|[0-9]{2}[A-Z])$")]
    private static partial Regex ValidFormat();
}
```

`ParkingSpots/SpotCode.cs`
```csharp
using System.Text.RegularExpressions;
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed partial record SpotCode
{
    private SpotCode(string value) => Value = value;

    public string Value { get; }

    public static Result<SpotCode> Create(string? raw)
    {
        var normalized = raw?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ValidFormat().IsMatch(normalized))
        {
            return ParkingErrors.InvalidSpotCode(raw);
        }

        return new SpotCode(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^P[0-9]{2}$")]
    private static partial Regex ValidFormat();
}
```

`ParkingSpots/VisitorName.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed record VisitorName
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    private VisitorName(string value) => Value = value;

    public string Value { get; }

    public static Result<VisitorName> Create(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (trimmed.Length is < MinLength or > MaxLength)
        {
            return ParkingErrors.InvalidVisitorName;
        }

        return new VisitorName(trimmed);
    }

    public override string ToString() => Value;
}
```

`ParkingSpots/DestinationUnit.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed record DestinationUnit
{
    private const int MaxLength = 20;

    private DestinationUnit(string value) => Value = value;

    public string Value { get; }

    public static Result<DestinationUnit> Create(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > MaxLength)
        {
            return ParkingErrors.InvalidDestinationUnit;
        }

        return new DestinationUnit(trimmed);
    }

    public override string ToString() => Value;
}
```

Note: `ParkingErrors.SpotOccupied(SpotCode)` and `VehicleAlreadyInside(LicensePlate)` compile now because both VOs exist in this task.

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/DomoNow.Parking.slnx`
Expected: all `ValueObjectTests` PASS, build with 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add backend
git commit -m "feat(domain): add result kernel and parking value objects with plate normalization" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: `ParkingSpot` aggregate (R1, R2, R3, R5)

**Files:**
- Create: `backend/src/DomoNow.Parking.Domain/ParkingSpots/{ParkingSpotStatus,AssignmentStatus,ParkingAssignment,ParkingSpot}.cs`
- Test: `backend/tests/DomoNow.Parking.Domain.UnitTests/ParkingSpotTests.cs`

**Interfaces:**
- Consumes: Task 2 kernel, VOs, `ParkingErrors`.
- Produces:
  - `enum ParkingSpotStatus { Available, Occupied, OutOfService }`, `enum AssignmentStatus { Active, Finished, Cancelled }`
  - `ParkingSpot`: `static ParkingSpot Create(SpotCode code)`; props `Guid Id`, `SpotCode Code`, `ParkingSpotStatus Status`, `ParkingAssignment? ActiveAssignment`; methods `Result<ParkingAssignment> CheckIn(LicensePlate, VisitorName, DestinationUnit, DateTimeOffset now)`, `Result<ParkingAssignment> CheckOut(Guid assignmentId, DateTimeOffset checkedOutAt, DateTimeOffset now)`, `Result MarkOutOfService()`. Private field-only navigation `_assignments` (`List<ParkingAssignment>`).
  - `ParkingAssignment`: props `Guid Id`, `Guid ParkingSpotId`, `LicensePlate LicensePlate`, `VisitorName VisitorName`, `DestinationUnit DestinationUnit`, `DateTimeOffset CheckedInAt`, `DateTimeOffset? CheckedOutAt`, `AssignmentStatus Status`. Constructor is private (EF binds to it by parameter names).

- [ ] **Step 1: Write the failing tests** — `ParkingSpotTests.cs`

```csharp
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Domain.UnitTests;

public sealed class ParkingSpotTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
    private static readonly LicensePlate Plate = LicensePlate.Create("ABC123").Value;
    private static readonly VisitorName Visitor = VisitorName.Create("Carlos Pérez").Value;
    private static readonly DestinationUnit Unit = DestinationUnit.Create("302").Value;

    private static ParkingSpot AvailableSpot() => ParkingSpot.Create(SpotCode.Create("P15").Value);

    private static (ParkingSpot Spot, ParkingAssignment Assignment) OccupiedSpot()
    {
        var spot = AvailableSpot();
        var assignment = spot.CheckIn(Plate, Visitor, Unit, Now).Value;
        return (spot, assignment);
    }

    [Fact]
    public void Create_StartsAvailableWithoutAssignment()
    {
        var spot = AvailableSpot();

        spot.Status.ShouldBe(ParkingSpotStatus.Available);
        spot.ActiveAssignment.ShouldBeNull();
    }

    [Fact]
    public void CheckIn_WhenAvailable_CreatesActiveAssignmentAndOccupiesSpot()
    {
        var spot = AvailableSpot();

        var result = spot.CheckIn(Plate, Visitor, Unit, Now);

        result.IsSuccess.ShouldBeTrue();
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
        spot.ActiveAssignment.ShouldBe(result.Value);
        result.Value.Status.ShouldBe(AssignmentStatus.Active);
        result.Value.ParkingSpotId.ShouldBe(spot.Id);
        result.Value.LicensePlate.ShouldBe(Plate);
        result.Value.CheckedInAt.ShouldBe(Now);
        result.Value.CheckedOutAt.ShouldBeNull();
    }

    [Fact]
    public void CheckIn_WhenOccupied_ReturnsSpotOccupied()
    {
        var (spot, first) = OccupiedSpot();

        var result = spot.CheckIn(LicensePlate.Create("XYZ987").Value, Visitor, Unit, Now);

        result.Error.Code.ShouldBe("Parking.SpotOccupied");
        spot.ActiveAssignment.ShouldBe(first);
    }

    [Fact]
    public void CheckIn_WhenOutOfService_ReturnsSpotOutOfService()
    {
        var spot = AvailableSpot();
        spot.MarkOutOfService();

        var result = spot.CheckIn(Plate, Visitor, Unit, Now);

        result.Error.Code.ShouldBe("Parking.SpotOutOfService");
        spot.Status.ShouldBe(ParkingSpotStatus.OutOfService);
        spot.ActiveAssignment.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_FinishesAssignmentAndReleasesSpot()
    {
        var (spot, assignment) = OccupiedSpot();
        var exit = Now.AddHours(2);

        var result = spot.CheckOut(assignment.Id, exit, exit);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(AssignmentStatus.Finished);
        result.Value.CheckedOutAt.ShouldBe(exit);
        spot.Status.ShouldBe(ParkingSpotStatus.Available);
        spot.ActiveAssignment.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_AtExactCheckInTime_IsAllowed()
    {
        var (spot, assignment) = OccupiedSpot();

        spot.CheckOut(assignment.Id, Now, Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void CheckOut_BeforeCheckIn_ReturnsErrorAndKeepsState()
    {
        var (spot, assignment) = OccupiedSpot();

        var result = spot.CheckOut(assignment.Id, Now.AddMinutes(-1), Now.AddHours(1));

        result.Error.Code.ShouldBe("Parking.CheckOutBeforeCheckIn");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
        assignment.Status.ShouldBe(AssignmentStatus.Active);
        assignment.CheckedOutAt.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_InTheFuture_ReturnsErrorAndKeepsState()
    {
        var (spot, assignment) = OccupiedSpot();
        var serverNow = Now.AddHours(1);

        var result = spot.CheckOut(assignment.Id, serverNow.AddMinutes(5), serverNow);

        result.Error.Code.ShouldBe("Parking.CheckOutInFuture");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
    }

    [Fact]
    public void CheckOut_WithUnknownAssignment_ReturnsAssignmentNotActive()
    {
        var (spot, _) = OccupiedSpot();

        var result = spot.CheckOut(Guid.NewGuid(), Now, Now);

        result.Error.Code.ShouldBe("Parking.AssignmentNotActive");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
    }

    [Fact]
    public void CheckOut_Twice_ReturnsAssignmentNotActive()
    {
        var (spot, assignment) = OccupiedSpot();
        spot.CheckOut(assignment.Id, Now, Now);

        var result = spot.CheckOut(assignment.Id, Now, Now);

        result.Error.Code.ShouldBe("Parking.AssignmentNotActive");
    }

    [Fact]
    public void CheckIn_AfterCheckOut_IsAllowedAgain()
    {
        var (spot, assignment) = OccupiedSpot();
        spot.CheckOut(assignment.Id, Now, Now);

        spot.CheckIn(Plate, Visitor, Unit, Now.AddMinutes(1)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MarkOutOfService_WhenOccupied_ReturnsSpotOccupied()
    {
        var (spot, _) = OccupiedSpot();

        spot.MarkOutOfService().Error.Code.ShouldBe("Parking.SpotOccupied");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test backend/DomoNow.Parking.slnx --filter ParkingSpotTests`
Expected: build FAIL — `ParkingSpot` not found.

- [ ] **Step 3: Implement**

`ParkingSpots/ParkingSpotStatus.cs`
```csharp
namespace DomoNow.Parking.Domain.ParkingSpots;

public enum ParkingSpotStatus
{
    Available,
    Occupied,
    OutOfService,
}
```

`ParkingSpots/AssignmentStatus.cs`
```csharp
namespace DomoNow.Parking.Domain.ParkingSpots;

public enum AssignmentStatus
{
    Active,
    Finished,
    Cancelled,
}
```

`ParkingSpots/ParkingAssignment.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

/// <summary>Entidad interna del agregado <see cref="ParkingSpot"/>: solo se modifica a través de la raíz.</summary>
public sealed class ParkingAssignment : Entity
{
    // EF Core materializa usando este constructor (los nombres de parámetros coinciden con las propiedades).
    private ParkingAssignment(
        Guid id,
        Guid parkingSpotId,
        LicensePlate licensePlate,
        VisitorName visitorName,
        DestinationUnit destinationUnit,
        DateTimeOffset checkedInAt,
        DateTimeOffset? checkedOutAt,
        AssignmentStatus status)
        : base(id)
    {
        ParkingSpotId = parkingSpotId;
        LicensePlate = licensePlate;
        VisitorName = visitorName;
        DestinationUnit = destinationUnit;
        CheckedInAt = checkedInAt;
        CheckedOutAt = checkedOutAt;
        Status = status;
    }

    public Guid ParkingSpotId { get; private set; }
    public LicensePlate LicensePlate { get; private set; }
    public VisitorName VisitorName { get; private set; }
    public DestinationUnit DestinationUnit { get; private set; }
    public DateTimeOffset CheckedInAt { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public AssignmentStatus Status { get; private set; }

    internal static ParkingAssignment Start(
        Guid parkingSpotId,
        LicensePlate licensePlate,
        VisitorName visitorName,
        DestinationUnit destinationUnit,
        DateTimeOffset checkedInAt) =>
        new(Guid.CreateVersion7(), parkingSpotId, licensePlate, visitorName, destinationUnit,
            checkedInAt, checkedOutAt: null, AssignmentStatus.Active);

    internal Result Finish(DateTimeOffset checkedOutAt, DateTimeOffset now)
    {
        if (Status != AssignmentStatus.Active)
        {
            return ParkingErrors.AssignmentNotActive(Id);
        }

        if (checkedOutAt < CheckedInAt)
        {
            return ParkingErrors.CheckOutBeforeCheckIn;
        }

        if (checkedOutAt > now)
        {
            return ParkingErrors.CheckOutInFuture;
        }

        CheckedOutAt = checkedOutAt;
        Status = AssignmentStatus.Finished;
        return Result.Success();
    }
}
```

`ParkingSpots/ParkingSpot.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

/// <summary>
/// Aggregate root. Protege R1 (una asignación activa), R2 (fuera de servicio), R3 (salida libera)
/// y R5 (fechas, vía <see cref="ParkingAssignment"/>). Solo se carga la asignación activa.
/// </summary>
public sealed class ParkingSpot : AggregateRoot
{
    private readonly List<ParkingAssignment> _assignments = [];

    // EF Core materializa usando este constructor.
    private ParkingSpot(Guid id, SpotCode code, ParkingSpotStatus status) : base(id)
    {
        Code = code;
        Status = status;
    }

    public SpotCode Code { get; private set; }
    public ParkingSpotStatus Status { get; private set; }

    public ParkingAssignment? ActiveAssignment =>
        _assignments.SingleOrDefault(assignment => assignment.Status == AssignmentStatus.Active);

    public static ParkingSpot Create(SpotCode code) =>
        new(Guid.CreateVersion7(), code, ParkingSpotStatus.Available);

    public Result<ParkingAssignment> CheckIn(
        LicensePlate licensePlate,
        VisitorName visitorName,
        DestinationUnit destinationUnit,
        DateTimeOffset now)
    {
        if (Status == ParkingSpotStatus.OutOfService)
        {
            return ParkingErrors.SpotOutOfService(Code);
        }

        if (Status == ParkingSpotStatus.Occupied)
        {
            return ParkingErrors.SpotOccupied(Code);
        }

        var assignment = ParkingAssignment.Start(Id, licensePlate, visitorName, destinationUnit, now);
        _assignments.Add(assignment);
        Status = ParkingSpotStatus.Occupied;
        return assignment;
    }

    public Result<ParkingAssignment> CheckOut(Guid assignmentId, DateTimeOffset checkedOutAt, DateTimeOffset now)
    {
        var assignment = ActiveAssignment;
        if (assignment is null || assignment.Id != assignmentId)
        {
            return ParkingErrors.AssignmentNotActive(assignmentId);
        }

        var finished = assignment.Finish(checkedOutAt, now);
        if (finished.IsFailure)
        {
            return finished.Error;
        }

        Status = ParkingSpotStatus.Available;
        return assignment;
    }

    public Result MarkOutOfService()
    {
        if (Status == ParkingSpotStatus.Occupied)
        {
            return ParkingErrors.SpotOccupied(Code);
        }

        Status = ParkingSpotStatus.OutOfService;
        return Result.Success();
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/DomoNow.Parking.slnx`
Expected: all Domain tests PASS.

- [ ] **Step 5: Commit**

```bash
git add backend
git commit -m "feat(domain): add ParkingSpot aggregate enforcing check-in and check-out rules" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Application — ports, check-in (R7) and check-out use cases

**Files:**
- Create: `backend/src/DomoNow.Parking.Application/Abstractions/{IParkingSpotRepository,IUnitOfWork}.cs`
- Create: `backend/src/DomoNow.Parking.Application/ParkingAssignments/CheckIn/{CheckInCommand,CheckInResponse,CheckInHandler}.cs`
- Create: `backend/src/DomoNow.Parking.Application/ParkingAssignments/CheckOut/{CheckOutCommand,CheckOutResponse,CheckOutHandler}.cs`
- Create: `backend/src/DomoNow.Parking.Application/Queries/{IParkingReadModel,ParkingSpotDto,ActiveAssignmentDto}.cs`
- Create: `backend/src/DomoNow.Parking.Application/DependencyInjection.cs`
- Test: `backend/tests/DomoNow.Parking.Application.UnitTests/{Fakes,CheckInHandlerTests,CheckOutHandlerTests}.cs`

**Interfaces:**
- Consumes: Task 2–3 domain types.
- Produces:
  - `IParkingSpotRepository { Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken); Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken); Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken); }`
  - `IUnitOfWork { Task<Result> CommitAsync(CancellationToken); }`
  - `record CheckInCommand(Guid ParkingSpotId, string? LicensePlate, string? VisitorName, string? DestinationUnit)`
  - `record CheckInResponse(Guid AssignmentId, Guid ParkingSpotId, string SpotCode, string LicensePlate, string VisitorName, string DestinationUnit, DateTimeOffset CheckedInAt)`
  - `CheckInHandler.HandleAsync(CheckInCommand, CancellationToken) : Task<Result<CheckInResponse>>`
  - `record CheckOutCommand(Guid AssignmentId, DateTimeOffset? CheckedOutAt)`
  - `record CheckOutResponse(Guid AssignmentId, Guid ParkingSpotId, string LicensePlate, DateTimeOffset CheckedInAt, DateTimeOffset CheckedOutAt)`
  - `CheckOutHandler.HandleAsync(CheckOutCommand, CancellationToken) : Task<Result<CheckOutResponse>>`
  - `IParkingReadModel { Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(ParkingSpotStatus? status, CancellationToken); Task<IReadOnlyList<ActiveAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken); }`
  - `record ParkingSpotDto(Guid Id, string Code, ParkingSpotStatus Status, bool IsAvailable, ActiveAssignmentDto? ActiveAssignment)`
  - `record ActiveAssignmentDto(Guid AssignmentId, Guid ParkingSpotId, string SpotCode, string LicensePlate, string VisitorName, string DestinationUnit, DateTimeOffset CheckedInAt)`
  - `IServiceCollection AddApplication(this IServiceCollection)`

- [ ] **Step 1: Write fakes** — `tests/DomoNow.Parking.Application.UnitTests/Fakes.cs`

```csharp
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.UnitTests;

internal sealed class FakeParkingSpotRepository : IParkingSpotRepository
{
    public List<ParkingSpot> Spots { get; } = [];
    public HashSet<string> PlatesInside { get; } = [];

    public Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken cancellationToken) =>
        Task.FromResult(Spots.SingleOrDefault(spot => spot.Id == spotId));

    public Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        Task.FromResult(Spots.SingleOrDefault(spot => spot.ActiveAssignment?.Id == assignmentId));

    public Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken cancellationToken) =>
        Task.FromResult(PlatesInside.Contains(plate.Value));
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }
    public Error? FailWith { get; set; }

    public Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            return Task.FromResult(Result.Failure(FailWith));
        }

        Commits++;
        return Task.FromResult(Result.Success());
    }
}
```

- [ ] **Step 2: Write failing tests** — `CheckInHandlerTests.cs`

```csharp
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.Extensions.Time.Testing;

namespace DomoNow.Parking.Application.UnitTests;

public sealed class CheckInHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeParkingSpotRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CheckInHandler _handler;
    private readonly ParkingSpot _spot = ParkingSpot.Create(SpotCode.Create("P15").Value);

    public CheckInHandlerTests()
    {
        _repository.Spots.Add(_spot);
        _handler = new CheckInHandler(_repository, _unitOfWork, new FakeTimeProvider(Now));
    }

    private CheckInCommand ValidCommand(string plate = "abc-123") =>
        new(_spot.Id, plate, "Carlos Pérez", "302");

    [Fact]
    public async Task HandleAsync_WithValidCommand_ChecksInAndCommits()
    {
        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LicensePlate.ShouldBe("ABC123");
        result.Value.SpotCode.ShouldBe("P15");
        result.Value.CheckedInAt.ShouldBe(Now);
        _spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
        _unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPlate_ReturnsValidationErrorWithoutCommit()
    {
        var result = await _handler.HandleAsync(ValidCommand("12"), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.InvalidLicensePlate");
        _unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenSpotDoesNotExist_ReturnsNotFound()
    {
        var command = ValidCommand() with { ParkingSpotId = Guid.NewGuid() };

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.SpotNotFound");
    }

    [Fact]
    public async Task HandleAsync_WhenVehicleAlreadyInside_ReturnsConflictAndLeavesSpotAvailable()
    {
        _repository.PlatesInside.Add("ABC123");

        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.VehicleAlreadyInside");
        _spot.Status.ShouldBe(ParkingSpotStatus.Available);
        _unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenSpotOccupied_ReturnsDomainError()
    {
        await _handler.HandleAsync(ValidCommand("XYZ987"), CancellationToken.None);

        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.SpotOccupied");
    }

    [Fact]
    public async Task HandleAsync_WhenCommitConflicts_PropagatesConflict()
    {
        _unitOfWork.FailWith = ParkingErrors.SpotAlreadyTaken;

        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.Error.ShouldBe(ParkingErrors.SpotAlreadyTaken);
    }
}
```

`CheckOutHandlerTests.cs`
```csharp
using DomoNow.Parking.Application.ParkingAssignments.CheckOut;
using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.Extensions.Time.Testing;

namespace DomoNow.Parking.Application.UnitTests;

public sealed class CheckOutHandlerTests
{
    private static readonly DateTimeOffset CheckedInAt = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeParkingSpotRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(CheckedInAt);
    private readonly ParkingSpot _spot = ParkingSpot.Create(SpotCode.Create("P02").Value);
    private readonly ParkingAssignment _assignment;
    private readonly CheckOutHandler _handler;

    public CheckOutHandlerTests()
    {
        _assignment = _spot.CheckIn(
            LicensePlate.Create("ABC123").Value,
            VisitorName.Create("Carlos Pérez").Value,
            DestinationUnit.Create("302").Value,
            CheckedInAt).Value;
        _repository.Spots.Add(_spot);
        _clock.Advance(TimeSpan.FromHours(2));
        _handler = new CheckOutHandler(_repository, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_WithoutExplicitTime_UsesServerTime()
    {
        var result = await _handler.HandleAsync(new CheckOutCommand(_assignment.Id, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CheckedOutAt.ShouldBe(_clock.GetUtcNow());
        _spot.Status.ShouldBe(ParkingSpotStatus.Available);
        _unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithExplicitPastTime_UsesIt()
    {
        var lateRegistration = CheckedInAt.AddMinutes(30);

        var result = await _handler.HandleAsync(new CheckOutCommand(_assignment.Id, lateRegistration), CancellationToken.None);

        result.Value.CheckedOutAt.ShouldBe(lateRegistration);
    }

    [Fact]
    public async Task HandleAsync_WithTimeBeforeCheckIn_ReturnsBusinessRuleError()
    {
        var result = await _handler.HandleAsync(
            new CheckOutCommand(_assignment.Id, CheckedInAt.AddMinutes(-5)), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.CheckOutBeforeCheckIn");
        _unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownAssignment_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(new CheckOutCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.ActiveAssignmentNotFound");
    }
}
```

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test backend/DomoNow.Parking.slnx --filter "FullyQualifiedName~Application.UnitTests"`
Expected: build FAIL — `CheckInHandler` / `IParkingSpotRepository` not found.

- [ ] **Step 4: Implement ports**

`Abstractions/IParkingSpotRepository.cs`
```csharp
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.Abstractions;

/// <summary>Puerto de salida: un repositorio por agregado. Devuelve la raíz con solo su asignación activa.</summary>
public interface IParkingSpotRepository
{
    Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken cancellationToken);

    Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken);

    /// <summary>R7: consulta de conjunto que cruza agregados (ADR-0005).</summary>
    Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken cancellationToken);
}
```

`Abstractions/IUnitOfWork.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Confirma la transacción. Los conflictos de concurrencia/unicidad vuelven como errores de negocio.</summary>
    Task<Result> CommitAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Implement check-in**

`ParkingAssignments/CheckIn/CheckInCommand.cs`
```csharp
namespace DomoNow.Parking.Application.ParkingAssignments.CheckIn;

public sealed record CheckInCommand(Guid ParkingSpotId, string? LicensePlate, string? VisitorName, string? DestinationUnit);
```

`ParkingAssignments/CheckIn/CheckInResponse.cs`
```csharp
namespace DomoNow.Parking.Application.ParkingAssignments.CheckIn;

public sealed record CheckInResponse(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string SpotCode,
    string LicensePlate,
    string VisitorName,
    string DestinationUnit,
    DateTimeOffset CheckedInAt);
```

`ParkingAssignments/CheckIn/CheckInHandler.cs`
```csharp
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.ParkingAssignments.CheckIn;

public sealed class CheckInHandler(IParkingSpotRepository spots, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<Result<CheckInResponse>> HandleAsync(CheckInCommand command, CancellationToken cancellationToken)
    {
        var plate = LicensePlate.Create(command.LicensePlate);
        if (plate.IsFailure)
        {
            return plate.Error;
        }

        var visitor = VisitorName.Create(command.VisitorName);
        if (visitor.IsFailure)
        {
            return visitor.Error;
        }

        var unit = DestinationUnit.Create(command.DestinationUnit);
        if (unit.IsFailure)
        {
            return unit.Error;
        }

        var spot = await spots.GetByIdAsync(command.ParkingSpotId, cancellationToken);
        if (spot is null)
        {
            return ParkingErrors.SpotNotFound(command.ParkingSpotId);
        }

        if (await spots.IsVehicleInsideAsync(plate.Value, cancellationToken))
        {
            return ParkingErrors.VehicleAlreadyInside(plate.Value);
        }

        var assignment = spot.CheckIn(plate.Value, visitor.Value, unit.Value, clock.GetUtcNow());
        if (assignment.IsFailure)
        {
            return assignment.Error;
        }

        var committed = await unitOfWork.CommitAsync(cancellationToken);
        if (committed.IsFailure)
        {
            return committed.Error;
        }

        var created = assignment.Value;
        return new CheckInResponse(
            created.Id, spot.Id, spot.Code.Value, created.LicensePlate.Value,
            created.VisitorName.Value, created.DestinationUnit.Value, created.CheckedInAt);
    }
}
```

- [ ] **Step 6: Implement check-out**

`ParkingAssignments/CheckOut/CheckOutCommand.cs`
```csharp
namespace DomoNow.Parking.Application.ParkingAssignments.CheckOut;

/// <param name="CheckedOutAt">Opcional (registro tardío). Si es null se usa la hora del servidor.</param>
public sealed record CheckOutCommand(Guid AssignmentId, DateTimeOffset? CheckedOutAt);
```

`ParkingAssignments/CheckOut/CheckOutResponse.cs`
```csharp
namespace DomoNow.Parking.Application.ParkingAssignments.CheckOut;

public sealed record CheckOutResponse(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string LicensePlate,
    DateTimeOffset CheckedInAt,
    DateTimeOffset CheckedOutAt);
```

`ParkingAssignments/CheckOut/CheckOutHandler.cs`
```csharp
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.ParkingAssignments.CheckOut;

public sealed class CheckOutHandler(IParkingSpotRepository spots, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<Result<CheckOutResponse>> HandleAsync(CheckOutCommand command, CancellationToken cancellationToken)
    {
        var spot = await spots.GetByActiveAssignmentIdAsync(command.AssignmentId, cancellationToken);
        if (spot is null)
        {
            return ParkingErrors.ActiveAssignmentNotFound(command.AssignmentId);
        }

        var now = clock.GetUtcNow();
        var finished = spot.CheckOut(command.AssignmentId, command.CheckedOutAt ?? now, now);
        if (finished.IsFailure)
        {
            return finished.Error;
        }

        var committed = await unitOfWork.CommitAsync(cancellationToken);
        if (committed.IsFailure)
        {
            return committed.Error;
        }

        var assignment = finished.Value;
        return new CheckOutResponse(
            assignment.Id,
            spot.Id,
            assignment.LicensePlate.Value,
            assignment.CheckedInAt,
            assignment.CheckedOutAt ?? now);
    }
}
```

- [ ] **Step 7: Read-side port and DTOs**

`Queries/IParkingReadModel.cs`
```csharp
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.Queries;

/// <summary>
/// Puerto de lectura. Las consultas no pasan por el agregado: proyectan directo a DTO.
/// No hay handler porque no hay lógica que orquestar (ADR-0003).
/// </summary>
public interface IParkingReadModel
{
    Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(ParkingSpotStatus? status, CancellationToken cancellationToken);

    Task<IReadOnlyList<ActiveAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken);
}
```

`Queries/ParkingSpotDto.cs`
```csharp
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.Queries;

public sealed record ParkingSpotDto(
    Guid Id,
    string Code,
    ParkingSpotStatus Status,
    bool IsAvailable,
    ActiveAssignmentDto? ActiveAssignment);
```

`Queries/ActiveAssignmentDto.cs`
```csharp
namespace DomoNow.Parking.Application.Queries;

public sealed record ActiveAssignmentDto(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string SpotCode,
    string LicensePlate,
    string VisitorName,
    string DestinationUnit,
    DateTimeOffset CheckedInAt);
```

- [ ] **Step 8: DI registration** — `DependencyInjection.cs`

```csharp
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Application.ParkingAssignments.CheckOut;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CheckInHandler>();
        services.AddScoped<CheckOutHandler>();
        return services;
    }
}
```

(`LoginHandler` is registered in Task 5.)

- [ ] **Step 9: Run tests**

Run: `dotnet test backend/DomoNow.Parking.slnx`
Expected: all Domain + Application tests PASS.

- [ ] **Step 10: Commit**

```bash
git add backend
git commit -m "feat(application): add check-in and check-out use cases with read model port" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Application — login use case (ADR-0004)

**Files:**
- Create: `backend/src/DomoNow.Parking.Application/Authentication/{Roles,AuthErrors,AuthenticatedOperator,AccessToken,IOperatorCredentialStore,IAccessTokenIssuer,LoginCommand,LoginResponse,LoginHandler}.cs`
- Modify: `backend/src/DomoNow.Parking.Application/DependencyInjection.cs`
- Test: `backend/tests/DomoNow.Parking.Application.UnitTests/LoginHandlerTests.cs`

**Interfaces:**
- Produces:
  - `static class Roles { const string Operator = "Operator"; }`
  - `record AuthenticatedOperator(Guid Id, string Username, string Role)`
  - `record AccessToken(string Value, DateTimeOffset ExpiresAt)`
  - `IOperatorCredentialStore { Task<AuthenticatedOperator?> ValidateAsync(string username, string password, CancellationToken); }`
  - `IAccessTokenIssuer { AccessToken Issue(AuthenticatedOperator op); }`
  - `record LoginCommand(string? Username, string? Password)`; `record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string Username, string Role)`
  - `LoginHandler.HandleAsync(LoginCommand, CancellationToken) : Task<Result<LoginResponse>>`

- [ ] **Step 1: Failing tests** — `LoginHandlerTests.cs`

```csharp
using DomoNow.Parking.Application.Authentication;

namespace DomoNow.Parking.Application.UnitTests;

public sealed class LoginHandlerTests
{
    private static readonly AuthenticatedOperator Portero = new(Guid.NewGuid(), "portero", Roles.Operator);

    private sealed class StubCredentialStore : IOperatorCredentialStore
    {
        public Task<AuthenticatedOperator?> ValidateAsync(string username, string password, CancellationToken cancellationToken) =>
            Task.FromResult(username == "portero" && password == "correct-password" ? Portero : null);
    }

    private sealed class StubTokenIssuer : IAccessTokenIssuer
    {
        public AccessToken Issue(AuthenticatedOperator authenticatedOperator) =>
            new($"token-for-{authenticatedOperator.Username}", DateTimeOffset.UnixEpoch);
    }

    private readonly LoginHandler _handler = new(new StubCredentialStore(), new StubTokenIssuer());

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsToken()
    {
        var result = await _handler.HandleAsync(new LoginCommand("portero", "correct-password"), CancellationToken.None);

        result.Value.AccessToken.ShouldBe("token-for-portero");
        result.Value.Role.ShouldBe(Roles.Operator);
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        var result = await _handler.HandleAsync(new LoginCommand("portero", "wrong"), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("portero", "")]
    public async Task HandleAsync_WithMissingFields_ReturnsValidationError(string? username, string? password)
    {
        var result = await _handler.HandleAsync(new LoginCommand(username, password), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.CredentialsRequired);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test backend/DomoNow.Parking.slnx --filter LoginHandlerTests`
Expected: build FAIL — `LoginHandler` not found.

- [ ] **Step 3: Implement**

`Authentication/Roles.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public static class Roles
{
    public const string Operator = "Operator";
}
```

`Authentication/AuthErrors.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Application.Authentication;

public static class AuthErrors
{
    public static readonly Error CredentialsRequired = Error.Validation(
        "Auth.CredentialsRequired", "El usuario y la contraseña son obligatorios.");

    // Mensaje genérico a propósito: no revela si el usuario existe.
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "Usuario o contraseña incorrectos.");
}
```

`Authentication/AuthenticatedOperator.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public sealed record AuthenticatedOperator(Guid Id, string Username, string Role);
```

`Authentication/AccessToken.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
```

`Authentication/IOperatorCredentialStore.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public interface IOperatorCredentialStore
{
    /// <returns>El operador si las credenciales son válidas; si no, null.</returns>
    Task<AuthenticatedOperator?> ValidateAsync(string username, string password, CancellationToken cancellationToken);
}
```

`Authentication/IAccessTokenIssuer.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public interface IAccessTokenIssuer
{
    AccessToken Issue(AuthenticatedOperator authenticatedOperator);
}
```

`Authentication/LoginCommand.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public sealed record LoginCommand(string? Username, string? Password);
```

`Authentication/LoginResponse.cs`
```csharp
namespace DomoNow.Parking.Application.Authentication;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string Username, string Role);
```

`Authentication/LoginHandler.cs`
```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Application.Authentication;

public sealed class LoginHandler(IOperatorCredentialStore credentials, IAccessTokenIssuer tokens)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrEmpty(command.Password))
        {
            return AuthErrors.CredentialsRequired;
        }

        var authenticated = await credentials.ValidateAsync(command.Username.Trim(), command.Password, cancellationToken);
        if (authenticated is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var token = tokens.Issue(authenticated);
        return new LoginResponse(token.Value, token.ExpiresAt, authenticated.Username, authenticated.Role);
    }
}
```

- [ ] **Step 4: Register** — in `DependencyInjection.AddApplication` add `services.AddScoped<LoginHandler>();` (with `using DomoNow.Parking.Application.Authentication;`).

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/DomoNow.Parking.slnx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend
git commit -m "feat(application): add operator login use case and auth ports" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Infrastructure — persistence, concurrency, R7 index, migration

**Files:**
- Create: `backend/src/DomoNow.Parking.Infrastructure/Persistence/{ParkingDbContext,UnitOfWork}.cs`
- Create: `backend/src/DomoNow.Parking.Infrastructure/Persistence/Configurations/{ParkingSpotConfiguration,ParkingAssignmentConfiguration}.cs`
- Create: `backend/src/DomoNow.Parking.Infrastructure/Repositories/{ParkingSpotRepository,ParkingReadModel}.cs`
- Create: `backend/src/DomoNow.Parking.Infrastructure/Identity/Operator.cs` + `Persistence/Configurations/OperatorConfiguration.cs` (table needed in the same initial migration)
- Create: `backend/src/DomoNow.Parking.Infrastructure/DependencyInjection.cs`
- Generated: `Persistence/Migrations/*_InitialCreate.cs`

**Interfaces:**
- Consumes: ports from Tasks 4–5.
- Produces:
  - `ParkingDbContext(DbContextOptions<ParkingDbContext>)` with `DbSet<ParkingSpot> ParkingSpots`, `DbSet<ParkingAssignment> ParkingAssignments`, `DbSet<Operator> Operators`
  - Constraint names `ux_parking_assignments_active_spot`, `ux_parking_assignments_active_plate`, `ck_parking_assignments_checkout_after_checkin`
  - `Operator { Guid Id; string Username; string PasswordHash; string Role }`
  - `IServiceCollection AddInfrastructure(this IServiceCollection, IConfiguration)` (identity services added in Task 7)
  - Connection string key: `ConnectionStrings:Parking`

No unit tests here: this is adapter code verified by the integration tests of Task 9 (real PostgreSQL). The step-level check is a clean build + a migration that contains both partial unique indexes.

- [ ] **Step 1: `Identity/Operator.cs`** (persistence model, not domain)

```csharp
namespace DomoNow.Parking.Infrastructure.Identity;

/// <summary>Usuario autorizado (subdominio genérico de identidad, ADR-0004). No pertenece al dominio Parking.</summary>
public sealed class Operator
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Username { get; init; }
    public string PasswordHash { get; set; } = string.Empty;
    public required string Role { get; init; }
}
```

- [ ] **Step 2: `Persistence/ParkingDbContext.cs`**

```csharp
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Persistence;

public sealed class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingAssignment> ParkingAssignments => Set<ParkingAssignment>();
    public DbSet<Operator> Operators => Set<Operator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingDbContext).Assembly);
}
```

- [ ] **Step 3: Configurations**

`Configurations/ParkingSpotConfiguration.cs`
```csharp
using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomoNow.Parking.Infrastructure.Persistence.Configurations;

internal sealed class ParkingSpotConfiguration : IEntityTypeConfiguration<ParkingSpot>
{
    public const string AssignmentsField = "_assignments";

    public void Configure(EntityTypeBuilder<ParkingSpot> builder)
    {
        builder.ToTable("parking_spots");
        builder.HasKey(spot => spot.Id);

        builder.Property(spot => spot.Code)
            .HasConversion(code => code.Value, value => SpotCode.Create(value).Value)
            .HasMaxLength(3)
            .IsRequired();
        builder.HasIndex(spot => spot.Code).IsUnique();

        builder.Property(spot => spot.Status).HasConversion<string>().HasMaxLength(20);

        // Versión del agregado: Npgsql mapea un uint con IsRowVersion a la columna de sistema xmin (ADR-0002).
        builder.Property<uint>("Version").IsRowVersion();

        builder.Ignore(spot => spot.ActiveAssignment);

        builder.HasMany<ParkingAssignment>(AssignmentsField)
            .WithOne()
            .HasForeignKey(assignment => assignment.ParkingSpotId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(AssignmentsField).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

> If EF rejects the field-only navigation `_assignments`, add to `ParkingSpot` a **private** property `private IReadOnlyCollection<ParkingAssignment> Assignments => _assignments;` and change `AssignmentsField` to `"Assignments"`. Record the change in PROGRESS.

`Configurations/ParkingAssignmentConfiguration.cs`
```csharp
using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomoNow.Parking.Infrastructure.Persistence.Configurations;

internal sealed class ParkingAssignmentConfiguration : IEntityTypeConfiguration<ParkingAssignment>
{
    public const string ActiveSpotIndex = "ux_parking_assignments_active_spot";
    public const string ActivePlateIndex = "ux_parking_assignments_active_plate";
    private const string ActiveFilter = "status = 'Active'";

    public void Configure(EntityTypeBuilder<ParkingAssignment> builder)
    {
        builder.ToTable("parking_assignments", table => table.HasCheckConstraint(
            "ck_parking_assignments_checkout_after_checkin",
            "checked_out_at IS NULL OR checked_out_at >= checked_in_at"));
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.LicensePlate)
            .HasConversion(plate => plate.Value, value => LicensePlate.Create(value).Value)
            .HasMaxLength(6)
            .IsRequired();
        builder.Property(assignment => assignment.VisitorName)
            .HasConversion(name => name.Value, value => VisitorName.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(assignment => assignment.DestinationUnit)
            .HasConversion(unit => unit.Value, value => DestinationUnit.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(assignment => assignment.Status).HasConversion<string>().HasMaxLength(20);

        // R1/R6 y R7: garantías finales en BD bajo concurrencia (ADR-0002, ADR-0005).
        builder.HasIndex(assignment => assignment.ParkingSpotId)
            .IsUnique()
            .HasFilter(ActiveFilter)
            .HasDatabaseName(ActiveSpotIndex);
        builder.HasIndex(assignment => assignment.LicensePlate)
            .IsUnique()
            .HasFilter(ActiveFilter)
            .HasDatabaseName(ActivePlateIndex);
    }
}
```

`Configurations/OperatorConfiguration.cs`
```csharp
using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomoNow.Parking.Infrastructure.Persistence.Configurations;

internal sealed class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("operators");
        builder.HasKey(op => op.Id);
        builder.Property(op => op.Username).HasMaxLength(50).IsRequired();
        builder.HasIndex(op => op.Username).IsUnique();
        builder.Property(op => op.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(op => op.Role).HasMaxLength(20).IsRequired();
    }
}
```

- [ ] **Step 4: `Persistence/UnitOfWork.cs`** — translates DB conflicts into business errors

```csharp
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DomoNow.Parking.Infrastructure.Persistence;

public sealed class UnitOfWork(ParkingDbContext dbContext) : IUnitOfWork
{
    public async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra transacción modificó el mismo parqueadero (xmin cambió): perdimos la carrera (R6).
            return ParkingErrors.SpotAlreadyTaken;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception, ParkingAssignmentConfiguration.ActiveSpotIndex))
        {
            return ParkingErrors.SpotAlreadyTaken;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception, ParkingAssignmentConfiguration.ActivePlateIndex))
        {
            return ParkingErrors.VehicleAlreadyInsideConflict;
        }
    }

    // Solo se capturan las violaciones conocidas; cualquier otra sigue siendo un error inesperado (500).
    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == constraintName;
}
```

- [ ] **Step 5: `Repositories/ParkingSpotRepository.cs`**

```csharp
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Persistence;
using DomoNow.Parking.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Repositories;

public sealed class ParkingSpotRepository(ParkingDbContext dbContext) : IParkingSpotRepository
{
    public async Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken cancellationToken)
    {
        var spot = await dbContext.ParkingSpots.SingleOrDefaultAsync(s => s.Id == spotId, cancellationToken);
        if (spot is null)
        {
            return null;
        }

        // El agregado solo carga su asignación activa; el historial no participa en invariantes.
        await dbContext.Entry(spot)
            .Collection<ParkingAssignment>(ParkingSpotConfiguration.AssignmentsField)
            .Query()
            .Where(assignment => assignment.Status == AssignmentStatus.Active)
            .LoadAsync(cancellationToken);

        return spot;
    }

    public async Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var spotId = await dbContext.ParkingAssignments
            .Where(a => a.Id == assignmentId && a.Status == AssignmentStatus.Active)
            .Select(a => (Guid?)a.ParkingSpotId)
            .SingleOrDefaultAsync(cancellationToken);

        return spotId is null ? null : await GetByIdAsync(spotId.Value, cancellationToken);
    }

    public Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken cancellationToken) =>
        dbContext.ParkingAssignments.AnyAsync(
            a => a.LicensePlate == plate && a.Status == AssignmentStatus.Active, cancellationToken);
}
```

- [ ] **Step 6: `Repositories/ParkingReadModel.cs`**

```csharp
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Repositories;

public sealed class ParkingReadModel(ParkingDbContext dbContext) : IParkingReadModel
{
    public async Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(
        ParkingSpotStatus? status, CancellationToken cancellationToken)
    {
        var spots = await dbContext.ParkingSpots.AsNoTracking()
            .Where(spot => status == null || spot.Status == status)
            .OrderBy(spot => spot.Code)
            .ToListAsync(cancellationToken);

        var activeBySpot = (await GetActiveAssignmentsAsync(cancellationToken))
            .ToDictionary(assignment => assignment.ParkingSpotId);

        return spots
            .Select(spot => new ParkingSpotDto(
                spot.Id,
                spot.Code.Value,
                spot.Status,
                spot.Status == ParkingSpotStatus.Available,
                activeBySpot.GetValueOrDefault(spot.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<ActiveAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken)
    {
        // Se materializan entidades (sin tracking) y se mapea en memoria: EF no traduce .Value de los VOs convertidos.
        var rows = await (
                from assignment in dbContext.ParkingAssignments.AsNoTracking()
                where assignment.Status == AssignmentStatus.Active
                join spot in dbContext.ParkingSpots.AsNoTracking() on assignment.ParkingSpotId equals spot.Id
                orderby assignment.CheckedInAt
                select new { assignment, spot })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ActiveAssignmentDto(
                row.assignment.Id,
                row.spot.Id,
                row.spot.Code.Value,
                row.assignment.LicensePlate.Value,
                row.assignment.VisitorName.Value,
                row.assignment.DestinationUnit.Value,
                row.assignment.CheckedInAt))
            .ToList();
    }
}
```

- [ ] **Step 7: `DependencyInjection.cs`**

```csharp
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Infrastructure.Persistence;
using DomoNow.Parking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Parking")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Parking' (configúrela con dotnet user-secrets).");

        services.AddDbContext<ParkingDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();
        services.AddScoped<IParkingReadModel, ParkingReadModel>();
        return services;
    }
}
```

- [ ] **Step 8: Development connection string (user-secrets on the Api project)**

```bash
dotnet user-secrets init --project backend/src/DomoNow.Parking.Api
dotnet user-secrets set "ConnectionStrings:Parking" "Host=localhost;Port=5432;Database=domonow_parking;Username=postgres;Password=<password from postgres-dev-credentials.txt>" --project backend/src/DomoNow.Parking.Api
```

- [ ] **Step 9: Generate migration** (requires the Api `Program.cs` to call `AddInfrastructure`; if Task 8 is not done yet, temporarily add `builder.Services.AddInfrastructure(builder.Configuration);` to the template `Program.cs`)

```bash
cd backend
dotnet ef migrations add InitialCreate -p src/DomoNow.Parking.Infrastructure -s src/DomoNow.Parking.Api -o Persistence/Migrations
```

Expected: migration created. Open the generated `*_InitialCreate.cs` and confirm it contains:
- `filter: "status = 'Active'"` on `ux_parking_assignments_active_spot` and `ux_parking_assignments_active_plate`
- `ck_parking_assignments_checkout_after_checkin`
- **no** `version` column in `parking_spots` (it maps to the system column `xmin`)

- [ ] **Step 10: Build + apply to dev DB**

Run: `dotnet build DomoNow.Parking.slnx` → 0 warnings.
Run: `dotnet ef database update -p src/DomoNow.Parking.Infrastructure -s src/DomoNow.Parking.Api`
Expected: `Done.` Then `psql -U postgres -d domonow_parking -c "\d parking_assignments"` lists both partial unique indexes.

- [ ] **Step 11: Commit**

```bash
cd ..
git add backend
git commit -m "feat(infrastructure): add EF Core persistence with concurrency token and partial unique indexes" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Infrastructure — identity, JWT issuer and database seeding

**Files:**
- Create: `backend/src/DomoNow.Parking.Infrastructure/Identity/{JwtOptions,SeedOperatorOptions,OperatorCredentialStore,JwtAccessTokenIssuer}.cs`
- Create: `backend/src/DomoNow.Parking.Infrastructure/Persistence/{DatabaseOptions,DatabaseInitializer}.cs`
- Modify: `backend/src/DomoNow.Parking.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IOperatorCredentialStore`, `IAccessTokenIssuer`, `AuthenticatedOperator`, `AccessToken`, `Roles` (Task 5); `Operator`, `ParkingDbContext` (Task 6).
- Produces:
  - `JwtOptions { Issuer, Audience, SigningKey, ExpiresInMinutes=60 }` bound from section `Jwt`; validated on start (key ≥ 32 bytes).
  - `SeedOperatorOptions { Username, Password }` bound from `SeedOperator` (optional).
  - `DatabaseOptions { MigrateAndSeedOnStartup }` bound from `Database`.
  - `static Task DatabaseInitializer.InitializeAsync(IServiceProvider services, CancellationToken)`: migrates; seeds `P01`–`P30` (`P04`, `P17` out of service) if table empty; seeds operator if configured and missing.

- [ ] **Step 1: Options**

`Identity/JwtOptions.cs`
```csharp
namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; init; } = "domonow-parking-api";
    public string Audience { get; init; } = "domonow-parking-web";
    public string SigningKey { get; init; } = string.Empty;
    public int ExpiresInMinutes { get; init; } = 60;
}
```

`Identity/SeedOperatorOptions.cs`
```csharp
namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class SeedOperatorOptions
{
    public const string SectionName = "SeedOperator";

    public string? Username { get; init; }
    public string? Password { get; init; }
}
```

`Persistence/DatabaseOptions.cs`
```csharp
namespace DomoNow.Parking.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool MigrateAndSeedOnStartup { get; init; }
}
```

- [ ] **Step 2: `Identity/OperatorCredentialStore.cs`**

```csharp
using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class OperatorCredentialStore(ParkingDbContext dbContext, IPasswordHasher<Operator> hasher)
    : IOperatorCredentialStore
{
    // Hash ficticio para que un usuario inexistente cueste lo mismo que uno existente (evita enumeración por tiempo).
    private static readonly Operator TimingDummy = new() { Username = "dummy", Role = Roles.Operator };
    private static readonly string DummyHash = new PasswordHasher<Operator>().HashPassword(TimingDummy, "dummy-password");

    public async Task<AuthenticatedOperator?> ValidateAsync(
        string username, string password, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        var found = await dbContext.Operators.AsNoTracking()
            .SingleOrDefaultAsync(op => op.Username == normalized, cancellationToken);

        if (found is null)
        {
            hasher.VerifyHashedPassword(TimingDummy, DummyHash, password);
            return null;
        }

        var verification = hasher.VerifyHashedPassword(found, found.PasswordHash, password);
        return verification == PasswordVerificationResult.Failed
            ? null
            : new AuthenticatedOperator(found.Id, found.Username, found.Role);
    }
}
```

- [ ] **Step 3: `Identity/JwtAccessTokenIssuer.cs`**

```csharp
using System.Text;
using DomoNow.Parking.Application.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(AuthenticatedOperator authenticatedOperator)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.ExpiresInMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = authenticatedOperator.Id.ToString(),
                [JwtRegisteredClaimNames.UniqueName] = authenticatedOperator.Username,
                ["role"] = authenticatedOperator.Role,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
```

- [ ] **Step 4: `Persistence/DatabaseInitializer.cs`**

```csharp
using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DomoNow.Parking.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const int SpotCount = 30;
    private static readonly string[] OutOfServiceCodes = ["P04", "P17"];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedSpotsAsync(dbContext, cancellationToken);
        await SeedOperatorAsync(
            dbContext,
            scope.ServiceProvider.GetRequiredService<IOptions<SeedOperatorOptions>>().Value,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<Operator>>(),
            cancellationToken);
    }

    private static async Task SeedSpotsAsync(ParkingDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.ParkingSpots.AnyAsync(cancellationToken))
        {
            return;
        }

        for (var number = 1; number <= SpotCount; number++)
        {
            var spot = ParkingSpot.Create(SpotCode.Create($"P{number:00}").Value);
            if (OutOfServiceCodes.Contains(spot.Code.Value))
            {
                spot.MarkOutOfService();
            }

            dbContext.ParkingSpots.Add(spot);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOperatorAsync(
        ParkingDbContext dbContext,
        SeedOperatorOptions seed,
        IPasswordHasher<Operator> hasher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seed.Username) || string.IsNullOrEmpty(seed.Password))
        {
            return;
        }

        var username = seed.Username.Trim().ToLowerInvariant();
        if (await dbContext.Operators.AnyAsync(op => op.Username == username, cancellationToken))
        {
            return;
        }

        var newOperator = new Operator { Username = username, Role = Roles.Operator };
        newOperator.PasswordHash = hasher.HashPassword(newOperator, seed.Password);
        dbContext.Operators.Add(newOperator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Register identity in `AddInfrastructure`** (append before `return services;`, add usings `DomoNow.Parking.Application.Authentication`, `DomoNow.Parking.Infrastructure.Identity`, `Microsoft.AspNetCore.Identity`, `System.Text`)

```csharp
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwt => Encoding.UTF8.GetByteCount(jwt.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:SigningKey debe tener al menos {JwtOptions.MinimumKeyBytes} bytes (configúrela con dotnet user-secrets).")
            .Validate(jwt => jwt.ExpiresInMinutes > 0, "Jwt:ExpiresInMinutes debe ser positivo.")
            .ValidateOnStart();
        services.Configure<SeedOperatorOptions>(configuration.GetSection(SeedOperatorOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddSingleton<IPasswordHasher<Operator>, PasswordHasher<Operator>>();
        services.AddScoped<IOperatorCredentialStore, OperatorCredentialStore>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
```

- [ ] **Step 6: Development secrets** (generate a random key; never commit it)

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project backend/src/DomoNow.Parking.Api
dotnet user-secrets set "SeedOperator:Username" "portero" --project backend/src/DomoNow.Parking.Api
dotnet user-secrets set "SeedOperator:Password" "<choose a local dev password>" --project backend/src/DomoNow.Parking.Api
```

(PowerShell alternative for the key: `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))`.)

- [ ] **Step 7: Build**

Run: `dotnet build backend/DomoNow.Parking.slnx` → 0 warnings. Run `dotnet test backend/DomoNow.Parking.slnx` → previous tests still PASS.

- [ ] **Step 8: Commit**

```bash
git add backend
git commit -m "feat(infrastructure): add operator credential store, JWT issuer and database seeding" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Api — endpoints, ProblemDetails, auth, CORS, rate limiting

**Files:**
- Replace: `backend/src/DomoNow.Parking.Api/Program.cs`
- Modify: `backend/src/DomoNow.Parking.Api/appsettings.json`, `appsettings.Development.json`
- Create: `backend/src/DomoNow.Parking.Api/ErrorHandling/{ErrorResults,GlobalExceptionHandler}.cs`
- Create: `backend/src/DomoNow.Parking.Api/Security/AuthenticationSetup.cs`
- Create: `backend/src/DomoNow.Parking.Api/Endpoints/{Contracts,AuthEndpoints,ParkingSpotEndpoints,ParkingAssignmentEndpoints}.cs`

**Interfaces:**
- Consumes: handlers (Tasks 4–5), `IParkingReadModel`, `AddApplication`, `AddInfrastructure`, `JwtOptions`, `DatabaseOptions`, `DatabaseInitializer`.
- Produces HTTP API:
  - `POST /api/auth/login` (anonymous, rate-limited) body `{ username, password }` → `200 LoginResponse` | 400 | 401
  - `GET /api/parking-spots?status={Available|Occupied|OutOfService}` (authenticated) → `200 ParkingSpotDto[]`
  - `GET /api/parking-assignments/active` (authenticated) → `200 ActiveAssignmentDto[]`
  - `POST /api/parking-assignments` (role Operator) body `{ parkingSpotId, licensePlate, visitorName, destinationUnit }` → `201 CheckInResponse` | 400 | 404 | 409 | 422
  - `PUT /api/parking-assignments/{id}/checkout` (role Operator) optional body `{ checkedOutAt }` → `200 CheckOutResponse` | 404 | 409 | 422
  - Enums serialized as strings. `public partial class Program` for `WebApplicationFactory`.

- [ ] **Step 1: `ErrorHandling/ErrorResults.cs`**

```csharp
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Api.ErrorHandling;

internal static class ErrorResults
{
    public static IResult ToProblem(this Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "No autorizado"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflicto"),
            ErrorType.BusinessRule => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida"),
            _ => (StatusCodes.Status500InternalServerError, "Error inesperado"),
        };

        return TypedResults.Problem(
            title: title,
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
```

- [ ] **Step 2: `ErrorHandling/GlobalExceptionHandler.cs`**

```csharp
using Microsoft.AspNetCore.Diagnostics;

namespace DomoNow.Parking.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "Error inesperado",
                Detail = "Ocurrió un error inesperado. Intente de nuevo más tarde.",
                Status = StatusCodes.Status500InternalServerError,
            },
        });
    }
}
```

- [ ] **Step 3: `Security/AuthenticationSetup.cs`**

```csharp
using System.Text;
using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DomoNow.Parking.Api.Security;

internal static class AuthenticationSetup
{
    public const string OperatorPolicy = "OperatorOnly";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "unique_name",
                    RoleClaimType = "role",
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(OperatorPolicy, policy => policy.RequireRole(Roles.Operator));

        return services;
    }
}
```

- [ ] **Step 4: `Endpoints/Contracts.cs`** (HTTP request bodies)

```csharp
namespace DomoNow.Parking.Api.Endpoints;

public sealed record LoginRequest(string? Username, string? Password);

public sealed record CheckInRequest(Guid ParkingSpotId, string? LicensePlate, string? VisitorName, string? DestinationUnit);

public sealed record CheckOutRequest(DateTimeOffset? CheckedOutAt);
```

- [ ] **Step 5: `Endpoints/AuthEndpoints.cs`**

```csharp
using DomoNow.Parking.Api.ErrorHandling;
using DomoNow.Parking.Application.Authentication;

namespace DomoNow.Parking.Api.Endpoints;

internal static class AuthEndpoints
{
    public const string LoginRateLimitPolicy = "login";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, LoginHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new LoginCommand(request.Username, request.Password), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitPolicy)
            .WithTags("Auth");

        return app;
    }
}
```

- [ ] **Step 6: `Endpoints/ParkingSpotEndpoints.cs`**

```csharp
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Api.Endpoints;

internal static class ParkingSpotEndpoints
{
    public static IEndpointRouteBuilder MapParkingSpotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/parking-spots", async (ParkingSpotStatus? status, IParkingReadModel readModel, CancellationToken ct) =>
                Results.Ok(await readModel.GetSpotsAsync(status, ct)))
            .RequireAuthorization()
            .WithTags("Parking spots");

        return app;
    }
}
```

- [ ] **Step 7: `Endpoints/ParkingAssignmentEndpoints.cs`**

```csharp
using DomoNow.Parking.Api.ErrorHandling;
using DomoNow.Parking.Api.Security;
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Application.ParkingAssignments.CheckOut;
using DomoNow.Parking.Application.Queries;

namespace DomoNow.Parking.Api.Endpoints;

internal static class ParkingAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapParkingAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parking-assignments").WithTags("Parking assignments");

        group.MapGet("/active", async (IParkingReadModel readModel, CancellationToken ct) =>
                Results.Ok(await readModel.GetActiveAssignmentsAsync(ct)))
            .RequireAuthorization();

        group.MapPost("/", async (CheckInRequest request, CheckInHandler handler, CancellationToken ct) =>
            {
                var command = new CheckInCommand(
                    request.ParkingSpotId, request.LicensePlate, request.VisitorName, request.DestinationUnit);
                var result = await handler.HandleAsync(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/parking-assignments/{result.Value.AssignmentId}", result.Value)
                    : result.Error.ToProblem();
            })
            .RequireAuthorization(AuthenticationSetup.OperatorPolicy);

        group.MapPut("/{id:guid}/checkout", async (
                Guid id, CheckOutRequest? request, CheckOutHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new CheckOutCommand(id, request?.CheckedOutAt), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .RequireAuthorization(AuthenticationSetup.OperatorPolicy);

        return app;
    }
}
```

- [ ] **Step 8: `Program.cs`**

```csharp
using System.Text.Json.Serialization;
using DomoNow.Parking.Api.Endpoints;
using DomoNow.Parking.Api.ErrorHandling;
using DomoNow.Parking.Api.Security;
using DomoNow.Parking.Application;
using DomoNow.Parking.Infrastructure;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .WithHeaders("Authorization", "Content-Type")
    .WithMethods("GET", "POST", "PUT")));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(AuthEndpoints.LoginRateLimitPolicy, limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateAndSeedOnStartup)
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Lifetime.ApplicationStopping);
}

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapParkingSpotEndpoints();
app.MapParkingAssignmentEndpoints();

await app.RunAsync();

public partial class Program;
```

- [ ] **Step 9: Configuration files (no secrets)**

`appsettings.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "domonow-parking-api",
    "Audience": "domonow-parking-web",
    "ExpiresInMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": []
  },
  "Database": {
    "MigrateAndSeedOnStartup": false
  }
}
```

`appsettings.Development.json`
```json
{
  "Cors": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  },
  "Database": {
    "MigrateAndSeedOnStartup": true
  }
}
```

Also set `launchSettings.json` `applicationUrl` for the `http` profile to `http://localhost:5080` (keep `https` profile as generated).

> Checkout body is optional. If `PUT .../checkout` without a body returns `415`, change the parameter to `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CheckOutRequest? request` (`using Microsoft.AspNetCore.Mvc;`) and log the change in PROGRESS.

- [ ] **Step 10: Build and smoke test**

```bash
dotnet build backend/DomoNow.Parking.slnx
dotnet run --project backend/src/DomoNow.Parking.Api --launch-profile http
```

In another terminal:
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5080/api/parking-spots
```
Expected: `401`.
```bash
curl -s -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" -d '{"username":"portero","password":"<dev password>"}'
```
Expected: JSON with `accessToken`. Then `GET /api/parking-spots` with `Authorization: Bearer <token>` → 30 spots, `P04` with `"status":"OutOfService"`. Stop the server.

- [ ] **Step 11: Commit**

```bash
git add backend
git commit -m "feat(api): expose parking endpoints with JWT auth, problem details and rate limiting" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 9: Integration tests — flows, auth, concurrency (R1, R6, R7)

**Files:**
- Create: `backend/tests/DomoNow.Parking.Api.IntegrationTests/{ParkingApiFactory,ApiClientExtensions,AuthTests,ParkingFlowTests,ConcurrencyTests}.cs`

**Interfaces:**
- Consumes: HTTP API from Task 8; `Program`; `ParkingDbContext`.
- Produces: `ParkingApiFactory` (xUnit collection fixture `"api"`), `ApiClientExtensions.LoginAsOperatorAsync()`, `GetSpotAsync(code)`.

Test DB connection: user-secrets of the test project, key `ConnectionStrings:ParkingTests`, or env var `ConnectionStrings__ParkingTests`.

```bash
dotnet user-secrets set "ConnectionStrings:ParkingTests" "Host=localhost;Port=5432;Database=domonow_parking_tests;Username=postgres;Password=<password>" --project backend/tests/DomoNow.Parking.Api.IntegrationTests
```

Isolation strategy: the database is dropped and recreated **once per test run** (collection fixture). Each test uses its **own spot codes and plates**, so tests never interfere.

| Test class | Spots used | Plates used |
|------------|-----------|-------------|
| `ParkingFlowTests` | P01–P08 (P04 fuera de servicio) | AAA111, AAA222, AAA333, AAA444, AAA555, AAA666, AAA777 |
| `ConcurrencyTests` | P10, P11, P12 | BBB100–BBB119, CCC123 |

- [ ] **Step 1: `ParkingApiFactory.cs`**

```csharp
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Api.IntegrationTests;

public sealed class ParkingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string OperatorUsername = "portero-test";
    public const string OperatorPassword = "Integration-Tests-Only-1!";

    private static readonly string ConnectionString =
        new ConfigurationBuilder()
            .AddUserSecrets<ParkingApiFactory>()
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("ParkingTests")
        ?? throw new InvalidOperationException(
            "Configure 'ConnectionStrings:ParkingTests' (user-secrets o variable de entorno ConnectionStrings__ParkingTests).");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Parking", ConnectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-with-at-least-32-bytes!");
        builder.UseSetting("SeedOperator:Username", OperatorUsername);
        builder.UseSetting("SeedOperator:Password", OperatorPassword);
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "true");
    }

    public async Task InitializeAsync()
    {
        // Base limpia por corrida: se elimina antes de que el host arranque (el arranque migra y siembra).
        var options = new DbContextOptionsBuilder<ParkingDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var dbContext = new ParkingDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        _ = Server; // fuerza el arranque del host → migración + seed
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ParkingApiFactory>;
```

- [ ] **Step 2: `ApiClientExtensions.cs`**

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Application.Queries;

namespace DomoNow.Parking.Api.IntegrationTests;

internal static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<HttpClient> CreateOperatorClientAsync(this ParkingApiFactory factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = ParkingApiFactory.OperatorUsername, password = ParkingApiFactory.OperatorPassword });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Json);
        login.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    public static async Task<ParkingSpotDto> GetSpotAsync(this HttpClient client, string code)
    {
        var spots = await client.GetFromJsonAsync<List<ParkingSpotDto>>("/api/parking-spots", Json);
        spots.ShouldNotBeNull();
        return spots.Single(spot => spot.Code == code);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(Json);
        body.ShouldNotBeNull();
        return body;
    }

    public static Task<HttpResponseMessage> CheckInAsync(this HttpClient client, Guid spotId, string plate) =>
        client.PostAsJsonAsync("/api/parking-assignments",
            new { parkingSpotId = spotId, licensePlate = plate, visitorName = "Visitante Prueba", destinationUnit = "302" });

    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
```

- [ ] **Step 3: `AuthTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class AuthTests(ParkingApiFactory factory)
{
    [Fact]
    public async Task GetSpots_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/parking-spots");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSpots_WithForgedToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.e30.forged");

        var response = await client.GetAsync("/api/parking-spots");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401WithGenericMessage()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = ParkingApiFactory.OperatorUsername, password = "wrong-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ProblemCodeAsync()).ShouldBe("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Login_WithValidCredentials_AllowsReadingSpots()
    {
        var client = await factory.CreateOperatorClientAsync();

        var response = await client.GetAsync("/api/parking-spots");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 4: `ParkingFlowTests.cs`**

Every test uses its own spot and plate (see isolation table), so the tests are order-independent.

```csharp
using System.Net;
using System.Net.Http.Json;
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class ParkingFlowTests(ParkingApiFactory factory)
{
    [Fact]
    public async Task Spots_AreSeededWithOutOfServiceOnes()
    {
        var client = await factory.CreateOperatorClientAsync();

        var spots = await (await client.GetAsync("/api/parking-spots")).ReadAsync<List<ParkingSpotDto>>();

        spots.Count.ShouldBe(30);
        spots.Single(s => s.Code == "P04").Status.ShouldBe(ParkingSpotStatus.OutOfService);
    }

    [Fact]
    public async Task GetSpots_FilteredByStatus_ReturnsOnlyThatStatus()
    {
        var client = await factory.CreateOperatorClientAsync();

        var spots = await (await client.GetAsync("/api/parking-spots?status=OutOfService"))
            .ReadAsync<List<ParkingSpotDto>>();

        spots.Select(s => s.Code).ShouldBe(["P04", "P17"]);
    }

    [Fact]
    public async Task CheckIn_ThenCheckOut_ReleasesSpot()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P01");

        var checkIn = await client.CheckInAsync(spot.Id, "aaa-111");
        checkIn.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await checkIn.ReadAsync<CheckInResponse>();
        created.LicensePlate.ShouldBe("AAA111");
        (await client.GetSpotAsync("P01")).Status.ShouldBe(ParkingSpotStatus.Occupied);

        var checkOut = await client.PutAsync($"/api/parking-assignments/{created.AssignmentId}/checkout", null);

        checkOut.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetSpotAsync("P01")).Status.ShouldBe(ParkingSpotStatus.Available);
    }

    [Fact]
    public async Task CheckIn_OnOccupiedSpot_Returns409()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P02");
        await client.CheckInAsync(spot.Id, "AAA222");

        var second = await client.CheckInAsync(spot.Id, "AAA333");

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.ProblemCodeAsync()).ShouldBe("Parking.SpotOccupied");
    }

    [Fact]
    public async Task CheckIn_OnOutOfServiceSpot_Returns422()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P04");

        var response = await client.CheckInAsync(spot.Id, "AAA666");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.SpotOutOfService");
    }

    [Fact]
    public async Task CheckIn_SamePlateOnAnotherSpot_Returns409()
    {
        var client = await factory.CreateOperatorClientAsync();
        await client.CheckInAsync((await client.GetSpotAsync("P03")).Id, "AAA444");

        var response = await client.CheckInAsync((await client.GetSpotAsync("P05")).Id, "aaa 444");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.VehicleAlreadyInside");
    }

    [Fact]
    public async Task CheckIn_WithInvalidPlate_Returns400()
    {
        var client = await factory.CreateOperatorClientAsync();

        var response = await client.CheckInAsync((await client.GetSpotAsync("P08")).Id, "12-AB");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.InvalidLicensePlate");
    }

    [Fact]
    public async Task CheckOut_WithTimeBeforeCheckIn_Returns422AndKeepsSpotOccupied()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P06");
        var created = await (await client.CheckInAsync(spot.Id, "AAA777")).ReadAsync<CheckInResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/parking-assignments/{created.AssignmentId}/checkout",
            new { checkedOutAt = created.CheckedInAt.AddHours(-1) });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.CheckOutBeforeCheckIn");
        (await client.GetSpotAsync("P06")).Status.ShouldBe(ParkingSpotStatus.Occupied);
    }

    [Fact]
    public async Task CheckOut_UnknownAssignment_Returns404()
    {
        var client = await factory.CreateOperatorClientAsync();

        var response = await client.PutAsync($"/api/parking-assignments/{Guid.NewGuid()}/checkout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActiveAssignments_ListsVehiclesInside()
    {
        var client = await factory.CreateOperatorClientAsync();
        await client.CheckInAsync((await client.GetSpotAsync("P07")).Id, "AAA555");

        var active = await (await client.GetAsync("/api/parking-assignments/active"))
            .ReadAsync<List<ActiveAssignmentDto>>();

        active.ShouldContain(a => a.LicensePlate == "AAA555" && a.SpotCode == "P07");
    }
}
```

- [ ] **Step 5: `ConcurrencyTests.cs`**

```csharp
using System.Net;
using DomoNow.Parking.Application.Queries;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class ConcurrencyTests(ParkingApiFactory factory)
{
    private const int ConcurrentRequests = 20;

    [Fact]
    public async Task SimultaneousCheckInsOnSameSpot_OnlyOneSucceeds()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P10");

        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentRequests)
            .Select(i => client.CheckInAsync(spot.Id, $"BBB{100 + i}")));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(ConcurrentRequests - 1);
        var active = await (await client.GetAsync("/api/parking-assignments/active"))
            .ReadAsync<List<ActiveAssignmentDto>>();
        active.Count(a => a.SpotCode == "P10").ShouldBe(1);
    }

    [Fact]
    public async Task SimultaneousCheckInsOfSamePlateOnDifferentSpots_OnlyOneSucceeds()
    {
        var client = await factory.CreateOperatorClientAsync();
        var first = await client.GetSpotAsync("P11");
        var second = await client.GetSpotAsync("P12");

        var responses = await Task.WhenAll(
            client.CheckInAsync(first.Id, "CCC123"),
            client.CheckInAsync(second.Id, "CCC123"));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var loser = responses.Single(r => r.StatusCode != HttpStatusCode.Created);
        loser.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await loser.ProblemCodeAsync()).ShouldBe("Parking.VehicleAlreadyInside");
    }
}
```

- [ ] **Step 6: Run the full suite**

Run: `dotnet test backend/DomoNow.Parking.slnx`
Expected: all Domain, Application and Integration tests PASS. Run the concurrency tests 5 times in a row (`--filter ConcurrencyTests`) to check they are not flaky; all runs PASS.

If a concurrency test fails: use `superpowers:systematic-debugging`; do **not** weaken the assertion. Check the generated SQL (`UPDATE ... WHERE id = @p AND xmin = @p`) and that both indexes exist in `domonow_parking_tests`.

- [ ] **Step 7: Commit**

```bash
git add backend
git commit -m "test(api): cover auth, parking flows and concurrent check-ins against PostgreSQL" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 10: Documentation and merge

**Files:**
- Modify: `README.md` (Configurar BD, Ejecutar backend, Ejecutar pruebas), `CLAUDE.md` §5 commands, `docs/PROGRESS.md`, `AI-USAGE.md` (only real cases that happened during execution)

- [ ] **Step 1: README — fill the backend sections**

```markdown
## Configurar la base de datos

1. PostgreSQL 17 corriendo en `localhost:5432`.
2. Configurar secretos de desarrollo (no se versionan):
   ```bash
   cd backend
   dotnet user-secrets set "ConnectionStrings:Parking" "Host=localhost;Port=5432;Database=domonow_parking;Username=postgres;Password=<tu-password>" --project src/DomoNow.Parking.Api
   dotnet user-secrets set "Jwt:SigningKey" "<clave aleatoria de 32+ bytes>" --project src/DomoNow.Parking.Api
   dotnet user-secrets set "SeedOperator:Username" "portero" --project src/DomoNow.Parking.Api
   dotnet user-secrets set "SeedOperator:Password" "<password del operador de prueba>" --project src/DomoNow.Parking.Api
   ```
3. Al arrancar en Development la API aplica migraciones y siembra 30 parqueaderos (P04 y P17 fuera de servicio) y el operador de prueba.

## Ejecutar el backend

```bash
dotnet run --project backend/src/DomoNow.Parking.Api --launch-profile http
```
API en `http://localhost:5080`; documento OpenAPI en `/openapi/v1.json`.

## Ejecutar las pruebas

```bash
dotnet user-secrets set "ConnectionStrings:ParkingTests" "Host=localhost;Port=5432;Database=domonow_parking_tests;Username=postgres;Password=<tu-password>" --project backend/tests/DomoNow.Parking.Api.IntegrationTests
dotnet test backend/DomoNow.Parking.slnx
```
Las pruebas de integración recrean `domonow_parking_tests` en cada corrida e incluyen la prueba de concurrencia (20 ingresos simultáneos al mismo parqueadero → exactamente 1 éxito).
```

- [ ] **Step 2: Update `CLAUDE.md` §5** with the verified commands (build, test, run, migrations add/update).

- [ ] **Step 3: PROGRESS** — mark Phase 1 rows ✅ with test counts from the last `dotnet test` output; log any deviation from this plan.

- [ ] **Step 4: Final verification** (`superpowers:verification-before-completion`)

Run: `dotnet build backend/DomoNow.Parking.slnx` → 0 warnings.
Run: `dotnet test backend/DomoNow.Parking.slnx` → all PASS. Quote the summary line in PROGRESS.

- [ ] **Step 5: Commit and merge**

```bash
git add README.md CLAUDE.md docs AI-USAGE.md
git commit -m "docs: document backend setup, run and test instructions" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git checkout main
git merge --no-ff feat/backend -m "Merge branch 'feat/backend'" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Coverage map (spec → task)

| Requirement | Task(s) |
|-------------|---------|
| GET spots (code, status, availability, filter) | 4 (DTO/port), 6 (read model), 8 (endpoint), 9 |
| POST check-in | 4, 8, 9 |
| PUT checkout | 4, 8, 9 |
| GET active assignments | 4, 6, 8, 9 |
| R1 one active per spot | 3, 6 (index), 9 |
| R2 out of service | 3, 9 |
| R3 checkout releases | 3, 9 |
| R4 plate normalized | 2, 9 |
| R5 dates | 3, 4, 6 (check constraint), 9 |
| R6 concurrency | 6 (xmin + index + UoW), 9 |
| R7 plate once inside | 4, 6 (index + UoW), 9 |
| Required tests (available, occupied, OOS, finish, dates) | 3, 9 |
| JWT / authorized user | 5, 7, 8, 9 |
| Errors / ProblemDetails / security | 8 |
| README run + DB setup | 10 |
