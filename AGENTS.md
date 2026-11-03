# AGENTS.md

## Scope and project map

These instructions apply to the entire repository. This .NET 10 library ships as two NuGet packages:

- `src/Edi.TemplateEmail/`: XML template loading, token mapping, fluent `EmailHelper` API, and `CommonMailMessage`.
- `src/Edi.TemplateEmail.Smtp/`: SMTP settings, dependency injection registration, MIME conversion, and sending via MailKit/MimeKit.
- `src/Edi.TemplateEmail.Tests/`: xUnit v3 tests for both packages.
- `src/Edi.TemplateEmail.TestConsole/`: interactive sample that can send real email; do not use it as an automated test.
- `README.md`: installation and usage examples, including the XML template format.
- `.github/workflows/dotnet.yml`: Release build, pack, and NuGet publication on pushes to `master`.

Keep the core package independent of SMTP dependencies. MailKit, MimeKit, and dependency injection helpers belong in the SMTP package.

## Build and validation

Use the .NET 10 SDK. Run commands from the repository root; restore is implicit.

```sh
dotnet build src/Edi.TemplateEmail.slnx --configuration Release
dotnet test --project src/Edi.TemplateEmail.Tests/Edi.TemplateEmail.Tests.csproj --configuration Release
```

- Run the build and tests for code changes. CI currently does not run tests, so a successful workflow does not replace local test validation.
- For documentation-only changes, check referenced paths, commands, and the diff; a build is unnecessary.
- Report what was checked and any checks that could not run.

For changes to package metadata or packaged assets, also verify both packages:

```sh
dotnet pack src/Edi.TemplateEmail/Edi.TemplateEmail.csproj --configuration Release
dotnet pack src/Edi.TemplateEmail.Smtp/Edi.TemplateEmail.Smtp.csproj --configuration Release
```

## Change conventions

- Follow nearby C# code: file-scoped namespaces, simple public models, small methods, and fluent APIs where already established. Use collection expressions and target-typed `new` when consistent with surrounding code.
- Preserve public method names, return types, constructor behavior, and exception behavior unless the task explicitly changes the contract; consumers depend on these NuGet packages.
- Do not enable nullable reference types project-wide as part of an unrelated change.
- Document public extension methods and DI helpers when it clarifies their use; avoid comments that restate the implementation.

## Compatibility rules

Preserve these behaviors unless the task explicitly changes them, and cover intentional changes with tests:

- Builder order is `ForType(...)`, then `Map(...)`/`MapRange(...)`, then `BuildMessage(...)`. `ForType` resets the pipeline; engine creation stays deferred until `BuildMessage`.
- `BuildMessage` requires recipients, formats and trims subject/body, and copies the template's HTML flag.
- XML uses the `MailConfiguration` root with `MailMessage` entries. `MessageType` and `IsHtml` are attributes; `MessageSubject`, `MessageBody`, and `MessageCulture` are elements. Preserve this serialized shape.
- Tokens use `{Entity.Property}`, with both parts matching `\w+`. Strings support `{Entity.Value}` and return the same value for any property name. Values without public properties fall back to `ToString()`; other objects resolve public properties by name. Missing entities/properties and null mapped/property values become an empty string.
- Template selection matches `MessageType` exactly, prefers a case-insensitive match for `CultureInfo.CurrentCulture.Name`, and otherwise falls back to the first matching message type.
- SMTP operations remain extensions on `CommonMailMessage`. Preserve MIME mapping of sender, from address, subject, recipients, optional CC, and HTML/plain text body.
- `SendAsync` stays asynchronous, propagates cancellation, authenticates only when a user name is present, and disconnects cleanly.
- `SkipCertificateValidation` is a development-only escape hatch. Keep it disabled by default and do not broaden the bypass.

## Tests

- Use xUnit v3 `[Fact]` tests with the existing Arrange / Act / Assert style.
- Add or update focused tests for changes to template selection, token formatting, XML serialization, recipient validation, MIME conversion, DI registration, or SMTP settings.
- Keep tests local: no real email, SMTP credentials, or network-dependent tests. Validate SMTP conversion and configuration without sending.
- Use temporary files for XML file tests and clean them up with `IDisposable` or equivalent cleanup.

## Documentation and packaging

- Update `README.md` when changing the fluent API, XML format, SMTP settings, DI registration, or installation. Keep examples based on XML templates and the `ForType(...).Map(...)/MapRange(...).BuildMessage(...)` flow.
- Package versions and NuGet metadata live in each library's `.csproj`; keep them aligned unless the task deliberately requires a difference. Avoid unrelated version changes.
- Both packages include `README.md` and `img/edi-logo-blue.png`; preserve those assets and packaging paths.
