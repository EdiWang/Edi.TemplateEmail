# TODO

Remaining work. Include focused regression tests with each change. Public behavior
changes require migration notes and updates to README.md and the compatibility
rules in AGENTS.md where applicable. Keep tests local; do not send real email or
run the interactive console as an automated test.

## Issue 1: Report configuration and input errors before sending

- [x] Validate template configuration at load time, including missing required
  fields and duplicate message-type/culture keys. Identify the template and field
  in validation errors.
- [ ] In a breaking release, reject unknown token entities/properties with useful
  errors while retaining empty output for legitimate null values.
- [x] Validate SMTP settings and recipient addresses, and complete MIME
  conversion before connecting or authenticating.
- [ ] Test malformed configuration, duplicate keys, token typos, null values, and
  invalid recipients locally. Keep address parsing and SMTP dependencies in the
  SMTP package.

Relevant files: `src/Edi.TemplateEmail/EmailHelper.cs`,
`src/Edi.TemplateEmail/EmailBuilder.cs`,
`src/Edi.TemplateEmail/TemplateEngine.cs`,
`src/Edi.TemplateEmail/PipelineItem.cs`,
`src/Edi.TemplateEmail.Smtp/ServiceCollectionExtensions.cs`,
`src/Edi.TemplateEmail.Smtp/SmtpExtensions.cs`.

## Issue 2: Select and format templates with an explicit culture

- [ ] Accept the recipient's culture explicitly for template selection and
  formatting.
- [ ] Define fallback as exact culture, parent culture, then an explicit default
  template.
- [ ] Use the selected culture consistently for supported date/number formatting.
  Preserve custom `ToString()` behavior where culture does not apply.
- [ ] Test exact/parent/default selection, culture-name casing, and date/number
  output.

Relevant files: `src/Edi.TemplateEmail/TemplateMailMessage.cs`,
`src/Edi.TemplateEmail/PipelineItem.cs`,
`src/Edi.TemplateEmail/EmailBuilder.cs`,
`src/Edi.TemplateEmail/IEmailBuilder.cs`,
`src/Edi.TemplateEmail/IEmailHelper.cs`.

## Issue 3: Use cross-platform template paths

- [ ] Replace manual backslash path construction in
  `src/Edi.TemplateEmail.TestConsole/Program.cs` with `Path.Combine` and document
  the sample's expected template location for cross-platform use.

## Issue 4: Reuse DI template configuration when justified by performance

- [ ] If repeated XML parsing has a measurable performance cost, load a reusable
  configuration snapshot for DI-created helpers while keeping builders independent.
- [ ] Before introducing caching, define reload behavior and cover configuration
  reuse, reload semantics, and builder isolation with local tests.

Relevant files: `src/Edi.TemplateEmail.Smtp/ServiceCollectionExtensions.cs`,
`src/Edi.TemplateEmail/EmailHelper.cs`, `README.md`.
