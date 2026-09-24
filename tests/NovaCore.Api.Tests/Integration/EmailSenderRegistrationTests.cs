using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.Modules.Identity.Services;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// <see cref="LoggingEmailSender"/> writes confirmation links (tokens) to the log, so it may only be wired in
/// Development. Any other environment must bring a real <see cref="IEmailSender"/> or fail at startup.
/// </summary>
public class EmailSenderRegistrationTests
{
    [Fact]
    public async Task Outside_development_the_host_refuses_to_start_without_an_email_sender()
    {
        await using var factory = new ProductionApiFactory();

        var error = Assert.Throws<InvalidOperationException>(() => factory.Server);

        Assert.Contains(nameof(IEmailSender), error.Message);
    }

    [Fact]
    public async Task Outside_development_the_host_starts_with_a_real_email_sender()
    {
        await using var factory = new ProductionApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<IEmailSender, NoOpEmailSender>()));

        var sender = factory.Services.GetRequiredService<IEmailSender>();

        Assert.IsType<NoOpEmailSender>(sender);
    }

    [Fact]
    public async Task In_development_the_logging_email_sender_is_used()
    {
        await using var factory = new NovaCoreApiFactory();

        var sender = factory.Services.GetRequiredService<IEmailSender>();

        Assert.IsType<LoggingEmailSender>(sender);
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

public sealed class ProductionApiFactory : NovaCoreApiFactory
{
    protected override string EnvironmentName => Environments.Production;
}
