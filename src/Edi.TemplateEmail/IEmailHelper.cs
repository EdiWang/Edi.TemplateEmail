using System.Collections.Generic;
using System.Threading.Tasks;

namespace Edi.TemplateEmail
{
    public interface IEmailHelper
    {
        event EmailFailedEventHandler EmailFailed;
        event EmailSentEventHandler EmailSent;
        event EmailCompletedEventHandler EmailCompleted;

        EmailSettings Settings { get; }
        TemplateEngine CurrentEngine { get; }

        public EmailHelper WithSettings(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort);
        public EmailHelper WithConfig(string configPath);
        public EmailHelper WithTls();
        public EmailHelper WithSenderName(string name);
        public EmailHelper WithDisplayName(string displayName);
        public EmailHelper ForType(string mailType);
        public EmailHelper Map(string name, object value);
        public Task SendAsync(string toAddress, string ccAddress = null);
        public Task SendAsync(IEnumerable<string> toAddress, string ccAddress = null);
    }
}