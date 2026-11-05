using System;
using System.Globalization;

namespace Edi.TemplateEmail;

internal sealed class EmailBuilder(MailConfiguration configuration, string mailType, CultureInfo culture = null) : IEmailBuilder
{
    private readonly TemplatePipeline _pipeline = new();

    public IEmailBuilder Map(string name, object value)
    {
        _pipeline.Map(name, value);
        return this;
    }

    public IEmailBuilder MapRange(params (string name, object value)[] values)
    {
        foreach (var (name, value) in values)
        {
            _pipeline.Map(name, value);
        }

        return this;
    }

    public CommonMailMessage BuildMessage(string[] receipts, string[] ccReceipts = null)
    {
        if (receipts is null || receipts.Length == 0)
        {
            throw new ArgumentNullException(nameof(receipts));
        }

        var template = new TemplateMailMessage(configuration, mailType, culture);
        if (!template.Loaded)
        {
            throw new InvalidOperationException(culture is null
                ? $"Mail type '{mailType}' was not found in the mail configuration."
                : $"Mail type '{mailType}' has no template for culture '{culture.Name}' or its parents, and no default template.");
        }

        var engine = new TemplateEngine(template, _pipeline);
        return new CommonMailMessage
        {
            Subject = engine.Format(template.Subject, false, culture).Trim(),
            Body = engine.Format(template.Text, template.IsHtml, culture).Trim(),
            Receipts = [.. receipts],
            CcReceipts = ccReceipts is null ? null : [.. ccReceipts],
            BodyIsHtml = template.IsHtml
        };
    }
}
