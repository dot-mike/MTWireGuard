using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MTWireGuard.Application.Models;
using MTWireGuard.Application.Repositories;
using MTWireGuard.Application.Utils;
using Newtonsoft.Json.Schema.Generation;
using Serilog;
using System.Net.Sockets;

namespace MTWireGuard.Application
{
    public class SetupValidator(IServiceProvider serviceProvider)
    {
        private IMikrotikRepository? api;
        private ILogger? logger;

        public static bool IsValid { get; private set; }
        public static string? Title { get; private set; }
        public static string? Description { get; private set; }

        public async Task<bool> Validate()
        {
            if (!Directory.Exists(Constants.DataPath()))
            {
                Directory.CreateDirectory(Constants.DataPath());
            }

            InitializeServices();

            Constants.IPApiSchema = new JSchemaGenerator().Generate(typeof(IPAPIResponse));

            if (ValidateEnvironmentVariables())
            {
                LogAndDisplayError("Environment variables are not set!", "Please set \"MT_IP\", \"MT_USER\", \"MT_PASS\", \"MT_PUBLIC_IP\" variables in container environment. Optional: Set \"GUI_ADMIN_USER\" and \"GUI_ADMIN_PASS\" for separate web interface login (defaults to admin/admin).");
                IsValid = false;
                return false;
            }

            var (apiConnection, apiConnectionMessage) = await ValidateAPIConnection();
            if (!apiConnection)
            {
                LogAndDisplayError("Error connecting to the router api!", apiConnectionMessage ?? "Unknown error");
                IsValid = false;
                return false;
            }

            var ip = GetIPAddress();
            if (string.IsNullOrEmpty(ip))
            {
                LogAndDisplayError("Error getting container IP address!", "Invalid container IP address.");
                IsValid = false;
                return false;
            }

            if (api != null && !await api.TryConnectAsync())
            {
                LogAndDisplayError("Error connecting to the router api!", "Connecting to API failed.");
                IsValid = false;
                return false;
            }

            await EnsureTrafficScripts(ip);

            IsValid = true;
            return true;
        }

        private static bool ValidateEnvironmentVariables()
        {
            string? IP = Environment.GetEnvironmentVariable("MT_IP");
            string? USER = Environment.GetEnvironmentVariable("MT_USER");
            string? PASS = Environment.GetEnvironmentVariable("MT_PASS");
            string? PUBLICIP = Environment.GetEnvironmentVariable("MT_PUBLIC_IP");
            string? GUI_USER = Environment.GetEnvironmentVariable("GUI_ADMIN_USER");
            string? GUI_PASS = Environment.GetEnvironmentVariable("GUI_ADMIN_PASS");

            bool missingMikrotikVars = string.IsNullOrEmpty(IP) || string.IsNullOrEmpty(USER) || string.IsNullOrEmpty(PUBLICIP);
            bool missingGuiVars = string.IsNullOrEmpty(GUI_USER) || string.IsNullOrEmpty(GUI_PASS);
            
            if (missingGuiVars)
            {
                Console.WriteLine("Warning: GUI_ADMIN_USER and GUI_ADMIN_PASS not set. Using defaults: admin/admin");
            }
            
            return missingMikrotikVars;
        }

        private async Task<(bool status, string? message)> ValidateAPIConnection()
        {
            try
            {
                return api != null ? (await api.TryConnectAsync(), string.Empty) : (false, "API not initialized");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private string GetIPAddress()
        {
            try
            {
                var name = System.Net.Dns.GetHostName();
                var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
                var address = System.Net.Dns.GetHostEntry(name).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                return address != null ? $"{address}:{port}" : string.Empty;
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "Error getting container IP address.");
                return string.Empty;
            }
        }

        private void LogAndDisplayError(string title, string description)
        {
            Title = title;
            Description = description;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[-] {Title}");
            Console.WriteLine($"[!] {Description}");
            Console.ResetColor();
            logger?.Error("Error in container configuration", new { Error = Title, Description });
        }

        private void InitializeServices()
        {
            var dbContext = serviceProvider.GetService<DBContext>();
            dbContext?.Database.Migrate();
            dbContext?.Database.EnsureCreated();
            api = serviceProvider.GetService<IMikrotikRepository>();
            logger = serviceProvider.GetService<ILogger>();
        }

        private async Task EnsureTrafficScripts(string ip)
        {
            if (api == null) return;
            
            var scripts = await api.GetScripts();
            var schedulers = await api.GetSchedulers();

            if (schedulers.Find(x => x.Name == "TrafficUsage") == null)
            {
                var create = await api.CreateScheduler(new()
                {
                    Name = "TrafficUsage",
                    Interval = new TimeSpan(0, 5, 0),
                    OnEvent = Constants.PeersTrafficUsageScript($"http://{ip}/api/usage"),
                    Policies = ["write", "read", "test", "ftp"],
                    Comment = "Update wireguard peers traffic usage"
                });
                var result = create.Code;
                logger?.Information("Created TrafficUsage Scheduler", new
                {
                    result = create
                });
            }
        }
    }
}
