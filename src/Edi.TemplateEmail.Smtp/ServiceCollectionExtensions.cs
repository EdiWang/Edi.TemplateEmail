using Microsoft.Extensions.DependencyInjection;
using System;

namespace Edi.TemplateEmail.Smtp;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IEmailHelper"/> and <see cref="EmailSettings"/> for use with SMTP sending.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configPath">Path to the XML mail configuration file.</param>
    /// <param name="configureSmtp">Delegate to configure SMTP settings.</param>
    public static IServiceCollection AddTemplateEmail(
        this IServiceCollection services,
        string configPath,
        Action<EmailSettings> configureSmtp)
    {
        services.AddTransient<IEmailHelper>(_ => new EmailHelper(configPath));

        var settings = new EmailSettings();
        configureSmtp(settings);
        services.AddSingleton(settings);

        return services;
    }

    /// <summary>
    /// Registers <see cref="IEmailHelper"/> and <see cref="EmailSettings"/> using an existing <see cref="MailConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mailConfiguration">The mail configuration instance.</param>
    /// <param name="configureSmtp">Delegate to configure SMTP settings.</param>
    public static IServiceCollection AddTemplateEmail(
        this IServiceCollection services,
        MailConfiguration mailConfiguration,
        Action<EmailSettings> configureSmtp)
    {
        services.AddTransient<IEmailHelper>(_ => new EmailHelper(mailConfiguration));

        var settings = new EmailSettings();
        configureSmtp(settings);
        services.AddSingleton(settings);

        return services;
    }
}
