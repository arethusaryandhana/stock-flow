using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StockFlow.WebAPI;

namespace StockFlow.Tests;

public sealed class RuntimeConfigurationGuardTests
{
    [Theory]
    [InlineData("Production", "SeedData:Demo", "true")]
    [InlineData("Production", "PasswordReset:ExposeResetToken", "true")]
    [InlineData("Production", "Database:ApplyMigrations", "true")]
    [InlineData("Staging", "SeedData:Demo", "true")]
    [InlineData("Staging", "PasswordReset:ExposeResetToken", "true")]
    public void Validate_rejects_unsafe_environment_settings(
        string environmentName,
        string setting,
        string value)
    {
        var environment = CreateEnvironment(environmentName);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [setting] = value })
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => RuntimeConfigurationGuard.Validate(environment, configuration));
    }

    [Fact]
    public void Validate_allows_demo_seed_and_reset_token_exposure_in_development()
    {
        var environment = CreateEnvironment(Environments.Development);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedData:Demo"] = "true",
                ["PasswordReset:ExposeResetToken"] = "true",
                ["Database:ApplyMigrations"] = "true"
            })
            .Build();

        RuntimeConfigurationGuard.Validate(environment, configuration);
    }

    [Fact]
    public void Validate_allows_controlled_migration_setting_outside_production()
    {
        var environment = CreateEnvironment("Staging");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "true"
            })
            .Build();

        RuntimeConfigurationGuard.Validate(environment, configuration);
    }

    private static IHostEnvironment CreateEnvironment(string environmentName) => new TestHostEnvironment
    {
        EnvironmentName = environmentName,
        ContentRootFileProvider = new NullFileProvider()
    };

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "StockFlow.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
