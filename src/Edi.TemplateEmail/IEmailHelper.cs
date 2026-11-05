using System;
using System.Globalization;

namespace Edi.TemplateEmail;

public interface IEmailHelper
{
    /// <summary>
    /// Creates an independent builder. Template selection is deferred until BuildMessage.
    /// </summary>
    IEmailBuilder ForType(string mailType);

    /// <summary>
    /// Creates an independent builder using the recipient's culture for formatting
    /// and exact, parent, then default template selection at BuildMessage.
    /// </summary>
    /// <remarks>
    /// Existing implementations remain compatible; implement this overload to support explicit cultures.
    /// </remarks>
    IEmailBuilder ForType(string mailType, CultureInfo culture)
        => throw new NotSupportedException("This email helper does not support explicit cultures.");
}
