using LocalSmtpCapture.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LocalSmtpCapture.Tests.Hosting;

public sealed class ProgramTests
{
    [Fact]
    public void CreateBuilder_LoadsDefaultOptions()
    {
        HostApplicationBuilder builder = Program.CreateBuilder([]);

        using IHost host = builder.Build();
        LocalSmtpCaptureOptions options = host.Services.GetRequiredService<IOptions<LocalSmtpCaptureOptions>>().Value;

        Assert.Equal("127.0.0.1", options.Smtp.Host);
        Assert.Equal(2525, options.Smtp.Port);
        Assert.Equal("./emails", options.Storage.OutputFolder);
        Assert.True(options.Smtp.Authentication.Enabled);
    }

    [Fact]
    public async Task CreateBuilder_ValidatesOptionsOnStart()
    {
        HostApplicationBuilder builder = Program.CreateBuilder([]);
        builder.Configuration["Smtp:Port"] = "70000";

        using IHost host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }
}
