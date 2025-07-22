using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MTWireGuard.Application.Utils;

namespace MTWireGuard.Application
{
    public class ApplicationLifetimeService(IHostApplicationLifetime hostApplicationLifetime, ILogger<ApplicationLifetimeService> logger) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private void OnStarted()
        {
            logger.LogInformation("Application started successfully - User: {UserName}, Machine: {MachineName}", 
                Environment.UserName, Environment.MachineName);
        }

        private void OnStopping()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Shutting down...");
            Console.ResetColor();
            logger.LogWarning("Application is stopping - Machine: {MachineName}", Environment.MachineName);
        }

        private void OnStopped()
        {
            logger.LogInformation("Application stopped");
        }
    }
}
