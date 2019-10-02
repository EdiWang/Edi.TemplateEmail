using System.Linq;

namespace Edi.TemplateEmail
{
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
                    Value.GetType().GetProperty(propertyName)?.GetValue(Value, null).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}