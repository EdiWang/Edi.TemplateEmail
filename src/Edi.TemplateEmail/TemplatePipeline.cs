using System.Collections.Generic;

namespace Edi.TemplateEmail;

public class TemplatePipeline
{
    private readonly Dictionary<string, PipelineItem> _pipeline = new();

    public TemplatePipeline Map(string name, object value)
    {
        _pipeline[name] = new PipelineItem { Name = name, Value = value };
        return this;
    }

    public bool HasEntity(string entityName)
    {
        return _pipeline.ContainsKey(entityName);
    }

    public PipelineItem this[string name] => _pipeline[name];
}