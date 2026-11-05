# TODO

Remaining work. Include focused regression tests with each change. Public behavior
changes require migration notes and updates to README.md and the compatibility
rules in AGENTS.md where applicable. Keep tests local; do not send real email or
run the interactive console as an automated test.

## Issue 1: Report unknown token errors in a breaking release

- [ ] In a breaking release, reject unknown token entities/properties with useful
  errors while retaining empty output for legitimate null values.
- [ ] Test unknown entities/properties and token typos, and confirm legitimate
  null mapped/property values still render as empty strings under the new behavior.

Relevant files: `src/Edi.TemplateEmail/EmailBuilder.cs`,
`src/Edi.TemplateEmail/TemplateEngine.cs`,
`src/Edi.TemplateEmail/PipelineItem.cs`,
`src/Edi.TemplateEmail.Tests/TemplateEngineTests.cs`,
`src/Edi.TemplateEmail.Tests/PipelineItemTests.cs`.
