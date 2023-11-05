using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using CorrelationId.DependencyInjection;
using CorrelationId;
using Practice.Aspnet;
using System.Data;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using Azure.Storage;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var webAppBuilder = WebApplication.CreateBuilder(args);

var appName = webAppBuilder.Configuration["General:AppName"]!;

webAppBuilder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Index", "/Index.html");
});
webAppBuilder.Services.AddControllersWithViews();

AddOIDCAuthentication(
    webAppBuilder,
    webAppBuilder.Configuration.GetRequiredSection(DuendeIdentityServerConfig.SectionPath).Get<DuendeIdentityServerConfig>()!);

AddTelemetryAndLogging(
    webAppBuilder, 
    appName,
    webAppBuilder.Configuration.GetRequiredSection(SeqIdentityServerConfig.SectionPath).Get<SeqIdentityServerConfig>()!);

webAppBuilder.Services.AddDefaultCorrelationId(options =>
{
    options.AddToLoggingScope = true;
});

webAppBuilder.Services.AddControllers();
webAppBuilder.Services.AddEndpointsApiExplorer();
webAppBuilder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo() { Title = appName, Version = "v1" });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

AddStorageConnections(webAppBuilder);
webAppBuilder.Services.Configure<GeneralConfig>(
    webAppBuilder.Configuration.GetRequiredSection(GeneralConfig.SectionPath)
);

var app = webAppBuilder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", appName);
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages().RequireAuthorization();

app.UseCorrelationId();

app.Run();

static void AddTelemetryAndLogging(
    WebApplicationBuilder webAppBuilder, 
    string appName,
    SeqIdentityServerConfig config)
{
    // Build a resource configuration action to set service information.
    Action<ResourceBuilder> configureResource = (r) =>
    {
        r.AddService(
            serviceName: appName,
            serviceNamespace: "Practice.Aspnet",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName);

        r.AddAttributes(new Dictionary<string, object>
        {
            ["host.name"] = Environment.MachineName,
            ["deployment.environment"] =
                webAppBuilder.Environment.EnvironmentName.ToLowerInvariant(),
        });
    };

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
            otlpOptions.Endpoint = new Uri($"{config.Address}logs");
            otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            //otlpOptions.Headers = "X-Seq-ApiKey=abcde12345"; // you can add a required instrumentation key in UI
        });

#if DEBUG
        options.AddConsoleExporter();
#endif
    });

    var useTraceAndMetrics = false; // Seq does not yet support otlp traces and metrics
    // if you are feeling ambitious - try assembling a full suite of opentelemetry 
    // as described here https://opentelemetry.io/docs/demo/docker-deployment/
    if (!useTraceAndMetrics)
    {
        return;
    }
    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/AspNetCore/Program.cs
    webAppBuilder.Services.AddOpenTelemetry()
        .ConfigureResource(configureResource)
        .WithTracing(traceProviderBuilder =>
        {
            // Tracing

            // Ensure the TracerProvider subscribes to any custom ActivitySources.
            traceProviderBuilder
                .AddSource(appName)
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();

            // Use IConfiguration binding for AspNetCore instrumentation options.
            //webAppBuilder.Services.Configure<AspNetCoreInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));

            traceProviderBuilder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri($"{config.Address}traces");
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
                .AddMeter(appName)
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();

            metricsProvideBuilder.AddOtlpExporter(otlpOptions =>
            {
                // Use IConfiguration directly for Otlp exporter endpoint option.
                otlpOptions.Endpoint = new Uri($"{config.Address}metrics");
                otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                //otlpOptions.Headers = "X-Seq-ApiKey=abcde12345";
            });

#if DEBUG
            //metricsProvideBuilder.AddConsoleExporter();
#endif
        });
}

static void AddOIDCAuthentication(
    WebApplicationBuilder webAppBuilder,
    DuendeIdentityServerConfig config)
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
            // TODO find way to specify separate URL for login
            // or experiment with container network mode "host"
            options.Authority = config.Address;
            //options.MetadataAddress = "https://127.0.0.1:5001/connect/authorize";
            options.ClientId = "web";
            options.ClientSecret = File.ReadAllText(config.ClientSecretFile);
            options.RequireHttpsMetadata = config.RequireHttpsMetadata;
            options.ResponseType = "code";
            //options.AuthorizationEndpoint = "https://127.0.0.1:5001/connect/authorize";

            //Handle the certificate checks yourself to allow self-signed certificates
            //https://stackoverflow.com/questions/60346955/how-do-i-allow-self-signed-certificates-to-be-accepted-by-kestrel-by-using-nativ
            options.BackchannelHttpHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => {
                    return true;
                    // TODO check actual thumbpint
                    // if (cert.Thumbprint == Configuration["TrustedCertificateThumbprint"])
                    // {
                    //         return true;
                    // }                     
                }
            };

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("verification");
            options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            options.GetClaimsFromUserInfoEndpoint = true;

            options.SaveTokens = true;
        });
}

static void AddStorageConnections(WebApplicationBuilder webAppBuilder)
{
    var msSqlDbConfig = webAppBuilder.Configuration.GetRequiredSection(MsSqlDbConfig.SectionPath).Get<MsSqlDbConfig>()!;
    var msSqlDbPassword = File.ReadAllText(msSqlDbConfig.PasswordFile);

    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

    builder.DataSource = msSqlDbConfig.Address;
    builder.UserID = msSqlDbConfig.UserName;
    builder.Password = msSqlDbPassword;
    builder.InitialCatalog = "master";
    builder.MultipleActiveResultSets = true;
    builder.Encrypt = true;
    builder.TrustServerCertificate = true;

    webAppBuilder.Services.AddScoped<IDbConnection>(services => new SqlConnection(builder.ConnectionString));

    var mongoDocumentDbConfig = webAppBuilder.Configuration.GetRequiredSection(MongoDocumentDbConfig.SectionPath).Get<MongoDocumentDbConfig>()!;
    var mongoDocumentPassword = File.ReadAllText(mongoDocumentDbConfig.PasswordFile);

     var mongoClientSetting = new MongoClientSettings()
    {
        Credential = MongoCredential.CreateCredential("admin", mongoDocumentDbConfig.UserName, mongoDocumentPassword),
        Server = new MongoServerAddress(mongoDocumentDbConfig.Host, mongoDocumentDbConfig.Port),
    };

    webAppBuilder.Services.AddScoped<IMongoDatabase>(services => new MongoClient(mongoClientSetting).GetDatabase("development"));

    
    var azuriteStorageConfig = webAppBuilder.Configuration.GetRequiredSection(AzuriteStorageConfig.SectionPath).Get<AzuriteStorageConfig>()!;
    var azuriteStoragePassword = File.ReadAllText(azuriteStorageConfig.PasswordFile);

    var blobUri = new Uri($"http://{azuriteStorageConfig.Host}:{azuriteStorageConfig.BlobPort}/{azuriteStorageConfig.UserName}");
    webAppBuilder.Services.AddScoped<BlobServiceClient>(services => new BlobServiceClient(
            blobUri,
            new StorageSharedKeyCredential(azuriteStorageConfig.UserName, azuriteStoragePassword)));

    var queueUri = new Uri($"http://{azuriteStorageConfig.Host}:{azuriteStorageConfig.QueuePort}/{azuriteStorageConfig.UserName}");
    webAppBuilder.Services.AddScoped<QueueServiceClient>(services => new QueueServiceClient(
            queueUri,
            new StorageSharedKeyCredential(azuriteStorageConfig.UserName, azuriteStoragePassword)));


    var tableUri =  new Uri($"http://{azuriteStorageConfig.Host}:{azuriteStorageConfig.TablePort}/{azuriteStorageConfig.UserName}");
    webAppBuilder.Services.AddScoped<TableServiceClient>(services => new TableServiceClient(
        tableUri, 
        new TableSharedKeyCredential(azuriteStorageConfig.UserName, azuriteStoragePassword)));

}