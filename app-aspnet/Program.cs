using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Instrumentation.AspNetCore;

var webAppBuilder = WebApplication.CreateBuilder(args);

webAppBuilder.Services.AddControllersWithViews();
webAppBuilder.Services.AddRazorPages();

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

// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/AspNetCore/Program.cs
webAppBuilder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(traceProviderBuilder =>
    {
        // Tracing

        // Ensure the TracerProvider subscribes to any custom ActivitySources.
        traceProviderBuilder
            //.AddSource(Instrumentation.ActivitySourceName)
            .SetSampler(new AlwaysOnSampler())
            //.AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        // Use IConfiguration binding for AspNetCore instrumentation options.
        //webAppBuilder.Services.Configure<AspNetCoreInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));

        traceProviderBuilder.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://127.0.0.1:4317");
        });

#if DEBUG
        traceProviderBuilder.AddConsoleExporter();
#endif
    });

webAppBuilder.Logging.ClearProviders();
//builder.Logging.AddConsole();
webAppBuilder.Logging.AddOpenTelemetry(options =>
{
    var resourceBuilder = ResourceBuilder.CreateDefault();
    configureResource(resourceBuilder);
    options.SetResourceBuilder(resourceBuilder);

    //options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;

    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://127.0.0.1:4317");
    });

#if DEBUG
    options.AddConsoleExporter();
#endif
});

webAppBuilder.Services.AddControllers();
webAppBuilder.Services.AddEndpointsApiExplorer();
webAppBuilder.Services.AddSwaggerGen(options =>
{
    // using System.Reflection;
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
    app.UseSwaggerUI();
}

//app.MapControllers();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

//app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
