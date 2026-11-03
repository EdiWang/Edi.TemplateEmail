# TODO

Remaining work from the project review, numbered sequentially.
Changes to public behavior must include focused tests, migration notes, and updates
to README.md and the compatibility rules in AGENTS.md where applicable.

## Issue 1: Encode ordinary values in HTML email bodies

- [ ] Encode ordinary text values inserted into HTML bodies. Currently, a mapped
  value containing HTML, such as a link, is inserted directly into the message.
- [ ] Provide an explicit way to insert trusted HTML. Keep subject and plain-text
  rendering separate from HTML encoding; validate URL values for their intended
  use rather than treating HTML encoding as URL validation.
- [ ] Test HTML characters, untrusted markup, explicitly trusted HTML, and
  unchanged subject/plain-text behavior. Document how existing templates that
  insert HTML must migrate.

Relevant files: `src/Edi.TemplateEmail/TemplateEngine.cs`,
`src/Edi.TemplateEmail/EmailHelper.cs`.

## Issue 2: Give each message its own builder state

- [ ] Separate reusable template configuration from per-message state.
  `ForType()` currently resets the same helper and returns it, so interleaved
  chains for Alice and Bob can build Alice's message using Bob's mappings.
- [ ] Return a fresh builder for each `ForType()` call and keep mutable engine
  and pipeline details internal. Preserve deferred engine creation until build.
- [ ] Snapshot recipient collections when building a message so later changes to
  the caller's arrays cannot change its delivery targets.
- [ ] Test interleaved builders, parallel independent builds, and recipient-array
  mutation. Update public interfaces, DI registration, examples, and migration
  guidance together.

Relevant files: `src/Edi.TemplateEmail/EmailHelper.cs`,
`src/Edi.TemplateEmail/IEmailHelper.cs`,
`src/Edi.TemplateEmail/CommonMailMessage.cs`,
`src/Edi.TemplateEmail.Smtp/ServiceCollectionExtensions.cs`.

## Issue 3: Report configuration and input errors before sending

- [ ] Validate template configuration at load time, including missing required
  fields and duplicate message-type/culture keys. Errors should identify the
  template and field instead of failing later with a generic null-input error.
- [ ] In a breaking release, reject unknown token entities/properties with useful
  errors while retaining empty output for legitimate null values. Document the
  change from the current silent empty-string behavior.
- [ ] Validate SMTP settings and recipient addresses, and complete MIME
  conversion before connecting or authenticating. `FromAddress` is already
  checked during MIME conversion, but that conversion currently happens after
  the SMTP connection and authentication.
- [ ] Test malformed configuration, duplicate keys, token typos, null values, and
  invalid recipients locally. Keep address parsing and SMTP dependencies in the
  SMTP package.

Relevant files: `src/Edi.TemplateEmail/EmailHelper.cs`,
`src/Edi.TemplateEmail/TemplateEngine.cs`,
`src/Edi.TemplateEmail.Smtp/ServiceCollectionExtensions.cs`,
`src/Edi.TemplateEmail.Smtp/SmtpExtensions.cs`.

## Issue 4: Separate SMTP delivery results from cleanup failures

- [ ] Define what happens when SMTP accepts a message but disconnecting fails or
  the cancellation token is canceled afterward. The current method reports a
  failure in either case, which can make a caller retry an accepted message.
- [ ] Ensure cleanup does not replace an established delivery result or hide the
  original send/authentication exception. Preserve asynchronous operation and
  cancellation of actual sending work.
- [ ] Add a minimal replaceable SMTP-client boundary for local tests covering
  accepted-send/disconnect-failure, send failure, and cancellation. Do not add
  automatic retries; SMTP delivery can still have an uncertain outcome.

Relevant file: `src/Edi.TemplateEmail.Smtp/SmtpExtensions.cs`.

## Issue 5: Make tests a prerequisite for publishing

The local `dotnet test` runner configuration is fixed. The remaining work is CI
integration and coverage of behavior that is still untested.

- [ ] Run Release build and tests on pull requests as well as pushes to master.
- [ ] Require successful tests before either NuGet package is published. Use:
  `dotnet test --project src/Edi.TemplateEmail.Tests/Edi.TemplateEmail.Tests.csproj --configuration Release`.
- [ ] Cover both DI registration overloads, helper lifetime/state isolation,
  `ForType()` reset behavior, unknown template types, and the literal XML shape
  using hand-written XML rather than only serializer-generated fixtures.
- [ ] Add regression tests alongside the remaining fixes above. Keep all tests
  local; do not execute the email-sending console as an automated test.

Relevant files: `.github/workflows/dotnet.yml`, `src/Edi.TemplateEmail.Tests/`.

## Issue 6: Select and format templates with an explicit culture

- [ ] Accept the recipient's culture explicitly instead of depending exclusively
  on `CultureInfo.CurrentCulture`, which may reflect a background worker's
  environment rather than the recipient's language.
- [ ] Define fallback as exact culture, parent culture, then an explicit default
  template. Currently, `zh-TW` does not match a `zh` template and falls back to
  the first template of the requested type.
- [ ] Use the selected culture consistently for supported date/number formatting.
  Preserve custom `ToString()` behavior where culture does not apply.
- [ ] Test exact/parent/default selection, culture-name casing, and date/number
  output. Document the change from first-match fallback and ambient formatting.

Relevant files: `src/Edi.TemplateEmail/TemplateMailMessage.cs`,
`src/Edi.TemplateEmail/PipelineItem.cs`,
`src/Edi.TemplateEmail/EmailHelper.cs`.

## Issue 7: Use cross-platform template paths

- [ ] Replace manual backslash path construction in README.md and
  `src/Edi.TemplateEmail.TestConsole/Program.cs` with `Path.Combine` and document
  the expected template location for cross-platform use.

## Issue 8: Document DI usage and define configuration loading behavior

- [ ] Document DI usage. If repeated XML parsing becomes significant, load a
  reusable configuration snapshot for DI-created helpers while keeping builders
  independent. Specify reload behavior before introducing caching; shared-read
  file access is already fixed.
