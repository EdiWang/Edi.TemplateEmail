using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(PipelineItem item)
        {
            Add(item.Name, item.Value);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _pipeline.Clear();
        }

        /// <summary>
        /// Gets the pipeline items.
        /// </summary>
        /// <value>The pipeline items.</value>
        public IEnumerable<PipelineItem> PipelineItems
        {
            get {
                return _pipeline.Keys.Select(name => this[name]);
            }
        }

        public PipelineItem this[string name] => new PipelineItem { Name = name, Value = _pipeline[name] };

        public TemplatePipeline()
        {

        }

        public TemplatePipeline(IEnumerable<KeyValuePair<string, object>> items)
        {
            foreach (var item in items)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public class PipelineItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }

        /// <summary>
        /// Gets the value of the specified property
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public string GetValue(string propertyName)
        {
            try
            {
                if (Value is string s)
                {
                    return s;
                }
                return !Value.GetType().GetProperties().Any() ? 
                                        Value.ToString() : 
                                        Value.GetType().GetProperty(propertyName).GetValue(Value, null).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
