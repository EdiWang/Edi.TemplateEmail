﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Edi.TemplateEmail.NetStd
{
    public interface IEmailHelper
    {
        event EmailHelper.EmailFailedEventHandler EmailFailed;
        event EmailHelper.EmailSentEventHandler EmailSent;
        event EmailHelper.EmailCompletedEventHandler EmailCompleted;
        EmailSettings Settings { get; }
        TemplateEngine CurrentEngine { get; }
        EmailHelper ApplyTemplate(string mailType, TemplatePipeline pipeline);

        Task SendMailAsync(string toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null);

        Task SendMailAsync(IEnumerable<string> toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null);
    }
}