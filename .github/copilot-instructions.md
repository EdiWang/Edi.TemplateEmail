# Copilot Instructions for Edi.TemplateEmail

## Project Overview

This repository contains a .NET 10 email templating library published as two NuGet packages:

- `Edi.TemplateEmail`: core XML template loading, token mapping, and `CommonMailMessage` building.
- `Edi.TemplateEmail.Smtp`: SMTP sending extensions using MailKit and MimeKit.

Keep the core package independent from SMTP-specific dependencies. MailKit, MimeKit, and dependency injection helpers belong in `src/Edi.TemplateEmail.Smtp` only.

## Repository Layout

- `src/Edi.TemplateEmail`: core template engine, XML configuration models, fluent `EmailHelper` API, and message model.
- `src/Edi.TemplateEmail.Smtp`: `EmailSettings`, DI registration, MIME conversion, and SMTP sending extensions.
- `src/Edi.TemplateEmail.Tests`: xUnit v3 tests for core and SMTP mapping behavior.
- `src/Edi.TemplateEmail.TestConsole`: interactive sample console app.
- `.github/workflows/dotnet.yml`: release build, pack, and NuGet push workflow.

## Coding Style

- Use modern C# with file-scoped namespaces and the existing namespace layout: `Edi.TemplateEmail` and `Edi.TemplateEmail.Smtp`.
- Match the current lightweight library style: simple public models, small focused methods, and fluent APIs where already established.
- Prefer collection expressions and target-typed `new` where they improve readability and are consistent with nearby code.
- Do not introduce nullable reference type annotations project-wide unless the repository is intentionally migrated.
- Keep public API changes conservative because this is a NuGet package. Preserve existing method names, return types, constructor behavior, XML shape, and exception behavior unless the task explicitly changes them.
- Add XML documentation for public extension methods or DI helpers when it clarifies the public surface. Avoid noisy comments inside straightforward implementation code.

## Core Template Behavior

- `EmailHelper` is a fluent builder. Expected call order is `ForType(...)`, then `Map(...)` or `MapRange(...)`, then `BuildMessage(...)`.
- `ForType` resets the pipeline and delays `TemplateEngine` creation until `BuildMessage`.
- `BuildMessage` validates that recipients are present, loads the selected template, formats subject/body, trims both, and copies `BodyIsHtml` from the template configuration.
- XML configuration compatibility matters. Preserve the `MailConfiguration` root with `MailMessage` entries and the existing `MailMessageConfiguration` attributes/elements:
  - `MessageType` and `IsHtml` are XML attributes.
  - `MessageSubject`, `MessageBody`, and `MessageCulture` are XML elements.
- Template tokens use the exact format `{Entity.Property}` where both parts are word characters.
- Mapping a string value supports `{Entity.Value}` because `PipelineItem.GetValue` returns the string for any requested property.
- Mapping an object resolves public properties by name. Missing entities, missing properties, null mapped values, and null property values resolve to an empty string.
- `TemplateMailMessage` selects the first matching `MessageType`, preferring one whose `MessageCulture` matches `CultureInfo.CurrentCulture.Name` case-insensitively.

## SMTP Package Behavior

- Keep SMTP functionality as extension methods on `CommonMailMessage` in `Edi.TemplateEmail.Smtp`.
- `ToMimeMessage` should map sender, from address, subject, recipients, optional CC recipients, and HTML/plain text body consistently with existing tests.
- `SendAsync` should remain asynchronous, accept a `CancellationToken`, authenticate only when a SMTP user name is present, and disconnect cleanly.
- Treat `SmtpSettings.SkipCertificateValidation` as a development-only escape hatch. Do not broaden it or enable it by default.

## Tests

- Use xUnit v3 with `[Fact]` tests in `src/Edi.TemplateEmail.Tests`.
- Follow the existing Arrange / Act / Assert style.
- Add or update tests whenever changing template selection, token formatting, XML serialization, recipient validation, MIME conversion, DI registration, or SMTP settings behavior.
- Do not add tests that send real email or require network access. SMTP tests should validate conversion and configuration behavior locally.
- Use temporary files for XML configuration tests and clean them up with `IDisposable` or equivalent test cleanup.

Useful commands:

```powershell
dotnet build .\src\Edi.TemplateEmail.slnx
dotnet test .\src\Edi.TemplateEmail.Tests\Edi.TemplateEmail.Tests.csproj
dotnet pack .\src\Edi.TemplateEmail\Edi.TemplateEmail.csproj --configuration Release
dotnet pack .\src\Edi.TemplateEmail.Smtp\Edi.TemplateEmail.Smtp.csproj --configuration Release
```

## Packaging and CI

- Package metadata lives in each `.csproj`. Keep versions and NuGet metadata aligned between the core and SMTP packages unless there is a deliberate reason to diverge.
- Both packages include the repository `README.md` and `img/edi-logo-blue.png` in the NuGet package.
- The GitHub Actions workflow builds and packs the core and SMTP projects separately using .NET 10.

## Documentation

- Update `README.md` when changing the user-facing fluent API, XML template format, SMTP settings, DI registration, or install instructions.
- Keep examples centered on the XML template plus `ForType(...).MapRange(...).BuildMessage(...)` flow.