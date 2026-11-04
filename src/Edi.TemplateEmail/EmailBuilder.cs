using System;

namespace Edi.TemplateEmail;

internal sealed class EmailBuilder(MailConfiguration configuration, string mailType) : IEmailBuilder
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

        var template = new TemplateMailMessage(configuration, mailType);
        if (!template.Loaded)
        {
            throw new InvalidOperationException($"Mail type '{mailType}' was not found in the mail configuration.");
        }

        var engine = new TemplateEngine(template, _pipeline);
        return new CommonMailMessage
        {
            Subject = engine.Format(template.Subject).Trim(),
            Body = engine.Format(template.Text, encodeHtml: template.IsHtml).Trim(),
            Receipts = [.. receipts],
            CcReceipts = ccReceipts is null ? null : [.. ccReceipts],
            BodyIsHtml = template.IsHtml
        };
    }
}
