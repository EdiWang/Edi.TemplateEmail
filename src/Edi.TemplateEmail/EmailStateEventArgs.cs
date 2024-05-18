using System;

namespace Edi.TemplateEmail;

public class EmailStateEventArgs(bool isSuccess, Guid? emailId) : EventArgs
{
    public bool IsSuccess { get; set; } = isSuccess;

    public Guid? EmailId { get; set; } = emailId;

    public string ServerResponse { get; set; }
}