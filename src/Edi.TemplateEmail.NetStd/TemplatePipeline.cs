using System.Collections.Generic;

namespace Edi.TemplateEmail.NetStd
{
    public class TemplatePipeline
    {
        /// <summary>
        /// The internal pipeline
        /// </summary>
        readonly Dictionary<string, object> _pipeline = new Dictionary<string, object>();

        /// <summary>
        /// Adds the specified obj by name to the pipeline.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="obj">The object.</param>
        private void Add(string name, object obj)
        {
            _pipeline[name] = obj;
        }

        public TemplatePipeline Map(string name, object value)
        {
            Add(name, value);
            return this;
        }

        /// <summary>
        /// Determines whether the specified entity name has entity.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <returns>
        /// 	<c>true</c> if the specified entity name has entity; otherwise, <c>false</c>.
        /// </returns>
        public bool HasEntity(string entityName)
        {
            return _pipeline.ContainsKey(entityName);
        }

        public PipelineItem this[string name] => new PipelineItem { Name = name, Value = _pipeline[name] };
    }
}
