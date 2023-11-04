using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;

var webAppBuilder = WebApplication.CreateBuilder(args);

webAppBuilder.Services.AddRazorPages();
webAppBuilder.Services.AddControllersWithViews();

AddOIDCAuthentication(webAppBuilder);

AddTelemetryAndLogging(webAppBuilder);

webAppBuilder.Services.AddControllers();
webAppBuilder.Services.AddEndpointsApiExplorer();
webAppBuilder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo() { Title = "WebUI8824", Version = "v1" });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = webAppBuilder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", "WebUI8824");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

app.MapControllers();

app.Run();

static void AddTelemetryAndLogging(WebApplicationBuilder webAppBuilder)
{
    // Build a resource configuration action to set service information.
    Action<ResourceBuilder> configureResource = (r) =>
    {

        r.AddService(
            serviceName: "WebUI8824",
            serviceNamespace: "Docker example",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName);

        r.AddAttributes(new Dictionary<string, object>
        {
            ["host.name"] = Environment.MachineName,
            ["deployment.environment"] =
                webAppBuilder.Environment.EnvironmentName.ToLowerInvariant(),
        });
    };

    var useTraceAndMetrics = false; // Seq does not yet support otlp traces and metrics

    if (useTraceAndMetrics)
    {
        // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/AspNetCore/Program.cs
        webAppBuilder.Services.AddOpenTelemetry()
            .ConfigureResource(configureResource)
            .WithTracing(traceProviderBuilder =>
            {
                // Tracing

                // Ensure the TracerProvider subscribes to any custom ActivitySources.
                traceProviderBuilder
                    .AddSource("WebUI8824")
                    .SetSampler(new AlwaysOnSampler())
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                // Use IConfiguration binding for AspNetCore instrumentation options.
                //webAppBuilder.Services.Configure<AspNetCoreInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));

                traceProviderBuilder.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri("http://127.0.0.1:5341/ingest/otlp/v1/traces");
                    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    //otlpOptions.Headers = "X-Seq-ApiKey=abcde12345";
                });

#if DEBUG
                traceProviderBuilder.AddConsoleExporter();
#endif
            })
            .WithMetrics(metricsProvideBuilder =>
            {
                // Metrics

                // Ensure the MeterProvider subscribes to any custom Meters.
                metricsProvideBuilder
                    .AddMeter("WebUI8824")
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                metricsProvideBuilder.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri("http://127.0.0.1:5341/ingest/otlp/v1/metrics");
                    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    //otlpOptions.Headers = "X-Seq-ApiKey=abcde12345";
                });

#if DEBUG
                //metricsProvideBuilder.AddConsoleExporter();
#endif
            });
    }


    webAppBuilder.Logging.ClearProviders();
    webAppBuilder.Logging.AddOpenTelemetry(options =>
    {
        var resourceBuilder = ResourceBuilder.CreateDefault();
        configureResource(resourceBuilder);
        options.SetResourceBuilder(resourceBuilder);

        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;

        options.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://127.0.0.1:5341/ingest/otlp/v1/logs");
            otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            //otlpOptions.Headers = "X-Seq-ApiKey=abcde12345";
        });

#if DEBUG
        options.AddConsoleExporter();
#endif
    });
}

static void AddOIDCAuthentication(WebApplicationBuilder webAppBuilder)
{
    JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

    webAppBuilder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
        .AddCookie("Cookies")
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = "https://localhost:5001";

            options.ClientId = "web";
            options.ClientSecret = "k3pTjtr0DQAO9cteA6eue6DXcyQP6Oh5Q9VWxAxX7F0XcZTMgRbDoW91zotnzH2oYQSEnA9MqERpHTxQygWsV9cDmvHuW3CBagtu";
            options.ResponseType = "code";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("verification");
            options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            options.GetClaimsFromUserInfoEndpoint = true;

            options.SaveTokens = true;
        });
}