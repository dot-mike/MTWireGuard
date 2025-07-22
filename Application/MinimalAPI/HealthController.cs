using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace MTWireGuard.Application.MinimalAPI
{
    public static class HealthController
    {
        public static IResult Health()
        {
            return TypedResults.Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            });
        }

        public static async Task<IResult> Ready(
            [FromServices] MTWireGuard.Application.Repositories.IMikrotikRepository api)
        {
            try
            {
                // Check if we can connect to Mikrotik API
                var canConnect = await api.TryConnectAsync();
                
                if (canConnect)
                {
                    return TypedResults.Ok(new { 
                        status = "ready", 
                        timestamp = DateTime.UtcNow,
                        mikrotik_connection = "ok"
                    });
                }
                else
                {
                    return TypedResults.Problem(
                        detail: "Cannot connect to Mikrotik API",
                        statusCode: 503,
                        title: "Service Unavailable"
                    );
                }
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    detail: ex.Message,
                    statusCode: 503,
                    title: "Service Unavailable"
                );
            }
        }
    }
}
