namespace StockFlow.WebAPI;

public static class RuntimeConfigurationGuard
{
    public static void Validate(IHostEnvironment environment, IConfiguration configuration)
    {
        var seedDemo = configuration.GetValue<bool>("SeedData:Demo");
        var exposeResetToken = configuration.GetValue<bool>("PasswordReset:ExposeResetToken");
        var applyMigrations = configuration.GetValue<bool>("Database:ApplyMigrations");

        if (!environment.IsDevelopment() && seedDemo)
        {
            throw new InvalidOperationException(
                "SeedData:Demo hanya boleh diaktifkan di environment Development.");
        }

        if (!environment.IsDevelopment() && exposeResetToken)
        {
            throw new InvalidOperationException(
                "PasswordReset:ExposeResetToken hanya boleh diaktifkan di environment Development.");
        }

        if (environment.IsProduction() && applyMigrations)
        {
            throw new InvalidOperationException(
                "Database:ApplyMigrations harus false di Production. Jalankan migrasi sebagai langkah rilis terkontrol.");
        }
    }
}
