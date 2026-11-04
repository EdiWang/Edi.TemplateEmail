using System.Net;
using System.Net.Security;
using System.Text;
using Edi.TemplateEmail.Smtp;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Xunit;

namespace Edi.TemplateEmail.Tests;

public class SmtpDeliveryTests
{
    private readonly EmailSettings _settings = new()
    {
        FromAddress = "from@example.com",
        SmtpSettings = new("smtp.example.com", "test-user", "test-password", 587)
    };

    private readonly CommonMailMessage _message = new()
    {
        Subject = "Subject",
        Body = "Body",
        Receipts = ["to@example.com"]
    };

    [Fact]
    public async Task SendAsync_ShouldAwaitSendingAndPreserveConnectionSettingsAndCancellation()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smtp = new TestSmtpClient { SendTask = completion.Task };

        // Act
        var sending = _message.SendAsync(_settings, smtp, cancellation.Token);

        // Assert
        Assert.False(sending.IsCompleted);
        Assert.Equal(["connect", "authenticate", "send"], smtp.Calls);
        Assert.Equal("smtp.example.com", smtp.Host);
        Assert.Equal(587, smtp.Port);
        Assert.Equal(SecureSocketOptions.StartTls, smtp.SocketOptions);
        Assert.Equal(cancellation.Token, smtp.ConnectToken);
        Assert.Equal(cancellation.Token, smtp.AuthenticateToken);
        Assert.Equal(cancellation.Token, smtp.SendToken);
        Assert.Null(smtp.ServerCertificateValidationCallback);

        completion.SetResult("250 accepted");
        Assert.Equal("250 accepted", await sending);
        Assert.Equal(["connect", "authenticate", "send", "disconnect", "dispose"], smtp.Calls);
        Assert.True(smtp.QuitRequested);
        Assert.Equal(CancellationToken.None, smtp.DisconnectToken);
    }

    [Fact]
    public async Task SendAsync_WhenAcceptedButDisconnectAndDisposeFail_ShouldReturnAcceptanceResponse()
    {
        // Arrange
        var smtp = new TestSmtpClient
        {
            DisconnectException = new IOException("Disconnect failed"),
            DisposeException = new IOException("Dispose failed")
        };

        // Act
        var response = await _message.SendAsync(_settings, smtp, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("250 accepted", response);
        Assert.Equal(["connect", "authenticate", "send", "disconnect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WhenCanceledAfterAcceptance_ShouldReturnAcceptanceResponseAndDisconnect()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var smtp = new TestSmtpClient { OnSend = cancellation.Cancel };

        // Act
        var response = await _message.SendAsync(_settings, smtp, cancellation.Token);

        // Assert
        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal("250 accepted", response);
        Assert.Equal(CancellationToken.None, smtp.DisconnectToken);
        Assert.Equal(["connect", "authenticate", "send", "disconnect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WhenSendingAndCleanupFail_ShouldPreserveSendException()
    {
        // Arrange
        var failure = new IOException("Send failed");
        var smtp = new TestSmtpClient
        {
            SendTask = Task.FromException<string>(failure),
            DisconnectException = new IOException("Disconnect failed"),
            DisposeException = new IOException("Dispose failed")
        };

        // Act
        var exception = await Assert.ThrowsAsync<IOException>(() => _message.SendAsync(_settings, smtp, TestContext.Current.CancellationToken));

        // Assert
        Assert.Same(failure, exception);
        Assert.Equal(["connect", "authenticate", "send", "disconnect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WhenAuthenticationAndCleanupFail_ShouldPreserveAuthenticationException()
    {
        // Arrange
        var failure = new AuthenticationException("Authentication failed");
        var smtp = new TestSmtpClient
        {
            AuthenticateException = failure,
            DisconnectException = new IOException("Disconnect failed"),
            DisposeException = new IOException("Dispose failed")
        };

        // Act
        var exception = await Assert.ThrowsAsync<AuthenticationException>(() => _message.SendAsync(_settings, smtp, TestContext.Current.CancellationToken));

        // Assert
        Assert.Same(failure, exception);
        Assert.Equal(["connect", "authenticate", "disconnect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WhenCanceledDuringSending_ShouldPropagateCancellationDespiteCleanupFailures()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var smtp = new TestSmtpClient
        {
            DisconnectException = new IOException("Disconnect failed"),
            DisposeException = new IOException("Dispose failed")
        };
        smtp.OnSend = () =>
        {
            cancellation.Cancel();
            smtp.SendTask = Task.FromCanceled<string>(cancellation.Token);
        };

        // Act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _message.SendAsync(_settings, smtp, cancellation.Token));

        // Assert
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(CancellationToken.None, smtp.DisconnectToken);
        Assert.Equal(["connect", "authenticate", "send", "disconnect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WhenConnectionFails_ShouldDisposeWithoutDisconnectingOrHidingFailure()
    {
        // Arrange
        var failure = new IOException("Connection failed");
        var smtp = new TestSmtpClient
        {
            ConnectException = failure,
            DisposeException = new IOException("Dispose failed")
        };

        // Act
        var exception = await Assert.ThrowsAsync<IOException>(() => _message.SendAsync(_settings, smtp, TestContext.Current.CancellationToken));

        // Assert
        Assert.Same(failure, exception);
        Assert.Equal(["connect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WithAnonymousSmtp_ShouldSkipAuthentication()
    {
        // Arrange
        _settings.SmtpSettings = new("smtp.example.com", null, null, 25);
        var smtp = new TestSmtpClient();

        // Act
        var response = await _message.SendAsync(_settings, smtp, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("250 accepted", response);
        Assert.Equal(["connect", "send", "disconnect", "dispose"], smtp.Calls);
    }

    [Fact]
    public async Task SendAsync_WithCertificateValidationBypass_ShouldApplyOnlyWhenEnabled()
    {
        // Arrange
        _settings.SmtpSettings.SkipCertificateValidation = true;
        var smtp = new TestSmtpClient();

        // Act
        await _message.SendAsync(_settings, smtp, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(smtp.ServerCertificateValidationCallback);
        Assert.True(smtp.ServerCertificateValidationCallback(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    private sealed class TestSmtpClient : SmtpClient
    {
        private bool _connected;

        public List<string> Calls { get; } = [];
        public Exception ConnectException { get; init; }
        public Exception AuthenticateException { get; init; }
        public Exception DisconnectException { get; init; }
        public Exception DisposeException { get; init; }
        public Task<string> SendTask { get; set; } = Task.FromResult("250 accepted");
        public Action OnSend { get; set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public SecureSocketOptions SocketOptions { get; private set; }
        public CancellationToken ConnectToken { get; private set; }
        public CancellationToken AuthenticateToken { get; private set; }
        public CancellationToken SendToken { get; private set; }
        public CancellationToken DisconnectToken { get; private set; }
        public bool QuitRequested { get; private set; }
        public override bool IsConnected => _connected;

        public override Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken cancellationToken = default)
        {
            Calls.Add("connect");
            Host = host;
            Port = port;
            SocketOptions = options;
            ConnectToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (ConnectException is not null) return Task.FromException(ConnectException);
            _connected = true;
            return Task.CompletedTask;
        }

        public override Task AuthenticateAsync(Encoding encoding, ICredentials credentials, CancellationToken cancellationToken = default)
        {
            Calls.Add("authenticate");
            AuthenticateToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return AuthenticateException is null ? Task.CompletedTask : Task.FromException(AuthenticateException);
        }

        public override Task<string> SendAsync(FormatOptions options, MimeMessage message, CancellationToken cancellationToken = default, ITransferProgress progress = null)
        {
            Calls.Add("send");
            SendToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            OnSend?.Invoke();
            return SendTask;
        }

        public override Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
        {
            Calls.Add("disconnect");
            QuitRequested = quit;
            DisconnectToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (DisconnectException is not null) return Task.FromException(DisconnectException);
            _connected = false;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            Calls.Add("dispose");
            base.Dispose(disposing);
            if (DisposeException is not null) throw DisposeException;
        }
    }
}
