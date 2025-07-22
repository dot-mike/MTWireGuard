using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MTWireGuard.Application.Models.Requests;
using MTWireGuard.Application.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MTWireGuard.Application.MinimalAPI
{
    internal class AuthController
    {
        public static Results<SignInHttpResult, UnauthorizedHttpResult, ProblemHttpResult> Login([FromBody] LoginRequest login)
        {
            try
            {
                // GUI Admin credentials (separate from MikroTik API)
                string GUI_ADMIN_USER = Environment.GetEnvironmentVariable("GUI_ADMIN_USER") ?? "admin";
                string GUI_ADMIN_PASS = Environment.GetEnvironmentVariable("GUI_ADMIN_PASS") ?? "admin";

                // Validate GUI admin credentials only
                if (login.Username == GUI_ADMIN_USER && login.Password == GUI_ADMIN_PASS)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Role, "Administrator"),
                        new(ClaimTypes.Name, login.Username),
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        AllowRefresh = true,
                        IsPersistent = true
                    };

                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    return TypedResults.SignIn(claimsPrincipal, authProperties, CookieAuthenticationDefaults.AuthenticationScheme);
                }
                else
                {
                    return TypedResults.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return TypedResults.Problem(
                    detail: ex.Message,
                    type: ex.GetType().Name);
            }
        }

        public static async Task<SignOutHttpResult> Logout(
            [FromServices] IMikrotikRepository API,
            HttpContext context)
        {
            // Clear the existing external cookie
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var sessionId = await API.GetCurrentSessionID();
            var kill = await API.KillJob(sessionId);
            return TypedResults.SignOut();
        }
    }
}
