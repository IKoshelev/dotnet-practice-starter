using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add services to the container.
builder.Services.AddRazorPages();

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(options =>
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

//builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Define attributes for your application
var openTelemetryBuilder = ResourceBuilder.CreateDefault()
                                // add attributes for the name and version of the service
                                .AddService("WebUI8824", "Docker example", "1")
                                // add attributes for the OpenTelemetry SDK version
                                .AddTelemetrySdk()
                                // add custom attributes
                                .AddAttributes(new Dictionary<string, object>
                                {
                                    ["host.name"] = Environment.MachineName,
                                    ["deployment.environment"] =
                                        builder.Environment.EnvironmentName.ToLowerInvariant(),
                                });


// openTelemetryBuilder.AddAspNetCoreInstrumentation();


builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(options =>
{
    options
         // define the resource
         .SetResourceBuilder(openTelemetryBuilder)
         // add custom processor
         //.AddProcessor(new CustomLogProcessor())
         ;


    #if DEBUG
    options.AddConsoleExporter();
    #endif
         
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;

    options.AddOtlpExporter(exporter => 
    {
        exporter.Endpoint = new Uri("http://127.0.0.1:4317"); //jaeger-open-telemetry-logs
    });
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(c =>
    {
        c.AddService("WebUI8824", "Docker example", "1");
        c.AddAttributes(new Dictionary<string, object>
        {
            ["host.name"] = Environment.MachineName,
            ["deployment.environment"] =
                builder.Environment.EnvironmentName.ToLowerInvariant(),
        });
    })
    .WithTracing(builder => 
    {
        builder.AddAspNetCoreInstrumentation();

        #if DEBUG
        builder.AddConsoleExporter();
        #endif
    
        builder.AddOtlpExporter(exporter => 
        {
            exporter.Endpoint = new Uri("http://127.0.0.1:4317"); //jaeger-open-telemetry-logs
        });
    });

// builder.Services.AddOpenTelemetry()
//             .ConfigureResource(c =>
//             {
//                 c.AddService("WebUI8824", "Docker example", "1");
//                 c.AddAttributes(new Dictionary<string, object>
//                 {
//                     ["host.name"] = Environment.MachineName,
//                     ["deployment.environment"] =
//                         builder.Environment.EnvironmentName.ToLowerInvariant(),
//                 });
//             })
//             .WithTracing(builder => 
//             {
//                 builder.AddAspNetCoreInstrumentation();
//                 #if DEBUG
//                 builder.AddConsoleExporter();
//                 #endif

//                 builder.AddOtlpExporter(exporter => 
//                 {
//                     exporter.Endpoint = new Uri("http://127.0.0.1:4317"); //jaeger-open-telemetry-logs
//                 });
//             });

var app = builder.Build();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
