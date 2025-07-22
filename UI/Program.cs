using MTWireGuard.Application;
using MTWireGuard.Application.MinimalAPI;
using MTWireGuard.Application.Utils;
using MTWireGuard.Middlewares;
using Serilog;
using Serilog.Ui.Web.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

internal class Program
{
    public static bool isValid { get; private set; }
    public static string? validationMessage { get; private set; }

    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("MTWireGuard - Starting...");

            // Load .env file before any other initialization
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            MTWireGuard.Application.Utils.DotEnv.Load(dotenv);

            var builder = WebApplication.CreateBuilder(args);
            
            // Explicitly add environment variables to configuration
            builder.Configuration.AddEnvironmentVariables();
            
            // Configure services
            ConfigureServices(builder);

            var app = builder.Build();

            // Initialize and validate using SetupValidator
            var serviceScope = app.Services.CreateScope().ServiceProvider;
            var validator = new SetupValidator(serviceScope);
            
            // Validate environment variables and Mikrotik connection
            isValid = await validator.Validate();
            
            if (!isValid)
            {
                Console.WriteLine("Application startup failed!");
                return 1; // Exit with error code
            }

            // Configure middleware pipeline
            ConfigureMiddleware(app);

            // Display startup complete message
            var urls = app.Configuration["ASPNETCORE_HTTP_PORTS"]?.Split(';')
                .Select(port => $"http://localhost:{port}")
                .FirstOrDefault() ?? "http://localhost:8080";
                
            Console.WriteLine($"Application started successfully at: {urls}");

            // Start the application
            await app.RunAsync();
            
            return 0; // Success
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Critical startup error: {ex.Message}");
            Console.ResetColor();
            
            // Try to log the error if possible
            try
            {
                Log.Fatal(ex, "Application terminated unexpectedly during startup");
            }
            catch
            {
                // If logging fails, just continue
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
            return 1; // Exit with error code
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddApplicationServices();

        builder.Host.UseSerilog(CoreUtil.LoggerConfiguration());
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseForwardedHeaders();

        // Only redirect to HTTPS if not behind a reverse proxy
        var shouldUseHttpsRedirection = !app.Environment.IsDevelopment() && 
            app.Configuration.GetValue<bool>("UseHttpsRedirection", true);
            
        if (shouldUseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseStaticFiles();
            app.UseHsts();
        }
        else
            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                    context.Context.Response.Headers.Append("Expires", "-1");
                }
            });

        app.UseExceptionHandler();
        app.UseClientReporting();
        //app.UseAntiForgery();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSession();

        app.MapRazorPages();

        // Health check endpoints (outside /api for standard compliance)
        app.MapGet("/health", MTWireGuard.Application.MinimalAPI.HealthController.Health);
        app.MapGet("/ready", MTWireGuard.Application.MinimalAPI.HealthController.Ready);

        app.UseWebSockets();

        app.
            MapGroup("/api/").
            MapGeneralApi();

        app.UseCors(options =>
        {
            options.AllowAnyHeader();
            options.AllowAnyMethod();
            options.AllowAnyOrigin();
        });

        app.UseSerilogRequestLogging();

        app.UseSerilogUi(options =>
        {
            options.HideSerilogUiBrand();
            options.InjectJavascript("/assets/js/serilogui.js");
            options.WithRoutePrefix("Debug");
            options.WithAuthenticationType(Serilog.Ui.Web.Models.AuthenticationType.Custom);
            options.EnableAuthorizationOnAppRoutes();
        });

        app.Run();
    }
}
