using System;

namespace Edi.TemplateEmail;

public class EmailStateEventArgs : EventArgs
{
    public bool IsSuccess { get; set; }

    public Guid? EmailId { get; set; }

    public string ServerResponse { get; set; }

    public EmailStateEventArgs(bool isSuccess, Guid? emailId)
    {
        IsSuccess = isSuccess;
        EmailId = emailId;
    }
}