using Serilog;
using Abm.Requesting.Proxy.Services;
using Serilog.Core;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(path: "./application-start-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    Log.Information("Starting up application {Environment}", builder.Environment.IsDevelopment() ? "(Is Development Environment)" : string.Empty); 

    //Setup log provider
    Logger serilogConfiguration = new LoggerConfiguration()
        .WriteTo.Console()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
    
    builder.Services.AddSerilog(serilogConfiguration);
    
    
    builder.Services.AddScoped<IRequestClaimService, RequestClaimService>();

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
    }

    app.MapReverseProxy(proxyPipeline =>
    {
        proxyPipeline.Use(async (
            httpContext,
            next) =>
        {
            var requestClaimService = httpContext.RequestServices.GetRequiredService<IRequestClaimService>();
            await requestClaimService.Proces(httpContext);

            if (!httpContext.Response.HasStarted)
            {
                await next();
            }
        });
    });

    app.MapGet("/test", () => "Hello")
        .WithName("GetWeatherForecast")
        .WithOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information($"Shut down complete");
    Log.CloseAndFlush();
}