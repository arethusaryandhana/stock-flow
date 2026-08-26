using StockFlow.Infrastructure;
using StockFlow.Worker;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ReportWorker>();
await builder.Build().RunAsync();
