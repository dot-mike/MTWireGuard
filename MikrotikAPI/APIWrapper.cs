using MikrotikAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MikrotikAPI
{
    public class APIWrapper
    {
        private string MT_IP { get; set; }
        private string MT_USER { get; set; }
        private string MT_PASS { get; set; }
        private bool MT_API_SSL { get; set; }
        private bool IsVerboseLoggingEnabled { get; set; }

        public APIWrapper(string IP, string User, string Password, bool useSSL = false)
        {
            MT_IP = IP;
            MT_USER = User;
            MT_PASS = Password;
            MT_API_SSL = useSSL;
            
            // Check if verbose or debug logging is enabled
            var loggingMode = Environment.GetEnvironmentVariable("LOGGING_MODE")?.ToLowerInvariant() ?? "info";
            IsVerboseLoggingEnabled = loggingMode == "verbose" || loggingMode == "debug";
        }

        public async Task<List<Log>> GetLogsAsync()
        {
            string json = await SendGetRequestAsync(Endpoints.Log);
            return json.ToModel<List<Log>>();
        }

        public async Task<List<WGServer>> GetServersAsync()
        {
            string json = await SendGetRequestAsync(Endpoints.Wireguard);
            return json.ToModel<List<WGServer>>();
        }

        public async Task<WGServer> GetServer(string Name)
        {
            var json = await SendGetRequestAsync(Endpoints.Wireguard + "?name=" + Name);
            return json.ToModel<WGServer[]>().FirstOrDefault();
        }

        public async Task<WGServer> GetServerById(string id)
        {
            var json = await SendGetRequestAsync(Endpoints.Wireguard + "/" + id);
            return json.ToModel<WGServer>();
        }

        public async Task<List<ServerTraffic>> GetServersTraffic()
        {
            var json = await SendRequestBase(RequestMethod.GET, Endpoints.Interface, "{\"stats\", {\".proplist\":\"name, type, rx-byte, tx-byte\"}}");
            return json.ToModel<List<ServerTraffic>>();
        }

        public async Task<string> GetIPAddresses()
        {
            var json = await SendGetRequestAsync(Endpoints.IPAddress);
            return json;
        }

        public async Task<CreationStatus> CreateIPAddress(IPAddressCreateModel ipAddress)
        {
            return await CreateItem<IPAddress>(Endpoints.IPAddress, ipAddress);
        }

        public async Task<CreationStatus> UpdateIPAddress(IPAddressUpdateModel ipAddress)
        {
            var itemJson = JObject.FromObject(ipAddress, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.IPAddress, itemJson, ipAddress.Id);
        }

        public async Task<CreationStatus> DeleteIP(string id)
        {
            return await DeleteItem(Endpoints.IPAddress, id);
        }

        public async Task<List<IPAddress>> GetServerIPAddress(string Interface)
        {
            var json = await SendGetRequestAsync(Endpoints.IPAddress + "?interface=" + Interface);
            return json.ToModel<List<IPAddress>>();
        }

        public async Task<List<WGPeer>> GetUsersAsync()
        {
            string json = await SendGetRequestAsync(Endpoints.WireguardPeers);
            return json.ToModel<List<WGPeer>>();
        }

        public async Task<WGPeer> GetUser(string id)
        {
            var users = await GetUsersAsync();
            return users.Find(u => u.Id == id);
        }

        public async Task<WGPeer> GetUserByPublicKey(string key)
        {
            var json = await SendGetRequestAsync(Endpoints.WireguardPeers + "?public-key=" + key);
            return json.ToModel<WGPeer[]>().FirstOrDefault();
        }

        public async Task<WGPeerLastHandshake> GetUserHandshake(string id)
        {
            var json = await SendRequestBase(RequestMethod.GET, Endpoints.WireguardPeers + $"/{id}?.proplist=last-handshake");
            return json.ToModel<WGPeerLastHandshake>();
        }

        public async Task<List<WGPeerLastHandshake>> GetUsersWithHandshake()
        {
            var json = await SendRequestBase(RequestMethod.GET, Endpoints.WireguardPeers + $"?.proplist=last-handshake,name");
            var model = json.ToModel<List<WGPeerLastHandshake>>();
            return model
                .Where(u => !string.IsNullOrWhiteSpace(u.LastHandshake))
                .ToList();
        }

        public async Task<MTInfo> GetInfo()
        {
            var json = await SendGetRequestAsync(Endpoints.SystemResource);
            return json.ToModel<MTInfo>();
        }

        public async Task<MTIdentity> GetName()
        {
            var json = await SendGetRequestAsync(Endpoints.SystemIdentity);
            return json.ToModel<MTIdentity>();
        }

        public async Task<CreationStatus> SetName(MTIdentityUpdateModel identity) // Create Model
        {
            var itemJson = JObject.FromObject(identity, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }).ToString();
            var json = await SendPostRequestAsync(Endpoints.SystemIdentity + "/set", itemJson);
            return json == "[]" ? new()
            {
                Success = true,
                Item = await GetName()
            } : json.ToModel<CreationStatus>();
        }

        public async Task<LoginStatus> TryConnectAsync()
        {
            var connection = await SendGetRequestAsync(Endpoints.Empty, true);
            return connection.ToModel<LoginStatus>();
        }

        public async Task<(bool success, string message)> ValidateAuthenticationAsync()
        {
            try
            {
                if (IsVerboseLoggingEnabled)
                {
                    Console.WriteLine("[MikrotikAPI] Validating MikroTik connection and authentication...");
                }
                
                // Use shorter timeout for startup validation (10 seconds)
                var validationTimeout = TimeSpan.FromSeconds(10);
                
                // First test basic connectivity without authentication
                var connectionTest = await SendGetRequestAsync(Endpoints.Empty, true, validationTimeout);
                var loginStatus = connectionTest.ToModel<LoginStatus>();
                
                if (loginStatus?.Error == 404)
                {
                    return (false, $"MikroTik router not found at {MT_IP}. Please check the MT_IP environment variable.");
                }
                
                // Now test with authentication using system/resource endpoint (requires authentication)
                var authTest = await SendGetRequestAsync(Endpoints.SystemResource, false, validationTimeout);
                
                // Check if response contains an error
                if (authTest.Contains("\"error\":401") && authTest.Contains("\"message\":\"Unauthorized\""))
                {
                    return (false, $"Authentication failed. Username '{MT_USER}' or password is incorrect. Please check MT_USER and MT_PASS environment variables.");
                }
                
                if (authTest.Contains("\"error\":"))
                {
                    var errorStatus = authTest.ToModel<LoginStatus>();
                    return (false, $"MikroTik API error: [{errorStatus?.Error}] {errorStatus?.Message}");
                }
                
                // If we get here and the response doesn't contain an error, authentication was successful
                if (IsVerboseLoggingEnabled)
                {
                    Console.WriteLine("[MikrotikAPI] MikroTik connection and authentication validated successfully");
                }
                return (true, "Authentication successful");
            }
            catch (HttpRequestException ex)
            {
                string protocol = MT_API_SSL ? "HTTPS" : "HTTP";
                string port = MT_API_SSL ? "443" : "80";
                return (false, $"Cannot connect to MikroTik router at {MT_IP}:{port} using {protocol}. Error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                string protocol = MT_API_SSL ? "HTTPS" : "HTTP";
                return (false, $"Connection to MikroTik router at {MT_IP} using {protocol} timed out. Please check if the router is reachable and the correct protocol is being used.");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error while validating MikroTik connection: {ex.Message}");
            }
        }

        public async Task<List<ActiveUser>> GetActiveSessions()
        {
            var json = await SendGetRequestAsync($"{Endpoints.ActiveUsers}?name=" + MT_USER);
            return json.ToModel<List<ActiveUser>>();
        }

        public async Task<List<Job>> GetJobs()
        {
            var json = await SendGetRequestAsync(Endpoints.Jobs);
            return json.ToModel<List<Job>>();
        }

        public async Task<DNS> GetDNS()
        {
            var json = await SendGetRequestAsync(Endpoints.DNS);
            return json.ToModel<DNS>();
        }

        public async Task<CreationStatus> SetDNS(MTDNSUpdateModel dns) // Create Model
        {
            var itemJson = JObject.FromObject(dns, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }).ToString();
            var json = await SendPostRequestAsync(Endpoints.DNS + "/set", itemJson);
            return json == "[]" ? new()
            {
                Success = true,
                Item = await GetDNS()
            } : json.ToModel<CreationStatus>();
        }

        public async Task<string> KillJob(string JobID)
        {
            return await SendDeleteRequestAsync($"{Endpoints.Jobs}/" + JobID);
        }

        public async Task<CreationStatus> CreateServer(WGServerCreateModel server)
        {
            return await CreateItem<WGServer>(Endpoints.Wireguard, server);
        }

        public async Task<CreationStatus> CreateUser(WGPeerCreateModel user)
        {
            return await CreateItem<WGPeer>(Endpoints.WireguardPeers, user);
        }

        public async Task<CreationStatus> UpdateServer(WGServerUpdateModel server)
        {
            var itemJson = JObject.FromObject(server, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.Wireguard, itemJson, server.Id);
        }

        public async Task<CreationStatus> UpdateUser(WGPeerUpdateModel user)
        {
            var itemJson = JObject.FromObject(user, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.WireguardPeers, itemJson, user.Id);
        }

        public async Task<CreationStatus> SetServerEnabled(WGEnability enability)
        {
            return await UpdateItem(Endpoints.Wireguard, new { disabled = enability.Disabled }, enability.ID);
        }

        public async Task<CreationStatus> SetUserEnabled(WGEnability enability)
        {
            return await UpdateItem(Endpoints.WireguardPeers, new { disabled = enability.Disabled }, enability.ID);
        }

        public async Task<CreationStatus> DeleteServer(string id)
        {
            return await DeleteItem(Endpoints.Wireguard, id);
        }

        public async Task<CreationStatus> DeleteUser(string id)
        {
            return await DeleteItem(Endpoints.WireguardPeers, id);
        }

        public async Task<string> GetTrafficSpeed()
        {
            return await SendPostRequestAsync(Endpoints.MonitorTraffic, "{\"interface\":\"ether1\",\"duration\":\"3s\"}");
        }

        public async Task<List<Script>> GetScripts()
        {
            var json = await SendGetRequestAsync(Endpoints.Scripts);
            return json.ToModel<List<Script>>();
        }

        public async Task<CreationStatus> CreateScript(ScriptCreateModel script)
        {
            return await CreateItem<Script>(Endpoints.Scripts, script);
        }

        public async Task<CreationStatus> DeleteScript(string id)
        {
            return await DeleteItem(Endpoints.Scripts, id);
        }

        public async Task<CreationStatus> UpdateScript(ScriptUpdateModel script)
        {
            var itemJson = JObject.FromObject(script, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.Scripts, itemJson, script.Id);
        }

        public async Task<string> RunScript(string name)
        {
            return await SendPostRequestAsync(Endpoints.Execute, "{\"script\":\"" + name + "\"}");
        }

        public async Task<List<Scheduler>> GetSchedulers()
        {
            var json = await SendGetRequestAsync(Endpoints.Scheduler);
            return json.ToModel<List<Scheduler>>();
        }

        public async Task<Scheduler> GetScheduler(string id)
        {
            var json = await SendGetRequestAsync($"{Endpoints.Scheduler}/{id}");
            return json.ToModel<Scheduler>();
        }

        public async Task<Scheduler> GetSchedulerByName(string name)
        {
            var json = await SendGetRequestAsync($"{Endpoints.Scheduler}/{name}");
            return json.ToModel<Scheduler>();
        }

        public async Task<CreationStatus> CreateScheduler(SchedulerCreateModel scheduler)
        {
            return await CreateItem<Scheduler>(Endpoints.Scheduler, scheduler);
        }

        public async Task<CreationStatus> UpdateScheduler(SchedulerUpdateModel scheduler)
        {
            var itemJson = JObject.FromObject(scheduler, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.Scheduler, itemJson, scheduler.Id);
        }

        public async Task<CreationStatus> DeleteScheduler(string id)
        {
            return await DeleteItem(Endpoints.Scheduler, id);
        }

        public async Task<List<IPPool>> GetIPPools()
        {
            var json = await SendGetRequestAsync(Endpoints.IPPool);
            return json.ToModel<List<IPPool>>();
        }

        public async Task<CreationStatus> CreateIPPool(IPPoolCreateModel ipPool)
        {
            return await CreateItem<IPPool>(Endpoints.IPPool, ipPool);
        }

        public async Task<CreationStatus> UpdateIPPool(IPPoolUpdateModel ipPool)
        {
            var itemJson = JObject.FromObject(ipPool, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.IPPool, itemJson, ipPool.Id);
        }

        public async Task<CreationStatus> DeleteIPPool(string id)
        {
            return await DeleteItem(Endpoints.IPPool, id);
        }

        // Simple Queue
        public async Task<List<SimpleQueue>> GetSimpleQueues()
        {
            var json = await SendGetRequestAsync(Endpoints.Queue);
            return json.ToModel<List<SimpleQueue>>();
        }

        public async Task<SimpleQueue> GetSimpleQueueByName(string name)
        {
            var json = await SendGetRequestAsync($"{Endpoints.Queue}/{name}");
            return json.ToModel<SimpleQueue>();
        }

        public async Task<CreationStatus> CreateSimpleQueue(SimpleQueueCreateModel simpleQueue)
        {
            return await CreateItem<SimpleQueue>(Endpoints.Queue, simpleQueue);
        }

        public async Task<CreationStatus> UpdateSimpleQueue(SimpleQueueUpdateModel simpleQueue)
        {
            var itemJson = JObject.FromObject(simpleQueue, new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            return await UpdateItem(Endpoints.Queue, itemJson, simpleQueue.Id);
        }

        public async Task<CreationStatus> DeleteSimpleQueue(string id)
        {
            return await DeleteItem(Endpoints.Queue, id);
        }

        public async Task<string> ResetSimpleQueue(string id)
        {
            return await SendPostRequestAsync($"{Endpoints.Queue}/reset-counters", $"{{\".id\":\"{id}\"}}");
        }

        // Base Methods
        private async Task<CreationStatus> CreateItem<T>(string Endpoint, object ItemCreateModel)
        {
            var jsonData = JObject.Parse(JsonConvert.SerializeObject(ItemCreateModel, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }));
            var json = await SendPutRequestAsync(Endpoint, jsonData);
            var obj = JObject.Parse(json);
            bool success = false;
            string code = string.Empty, message = string.Empty, detail = string.Empty;
            T item = default;
            if (obj.TryGetValue(".id", out var Id))
            {
                success = true;
                item = JsonConvert.DeserializeObject<T>(json);
            }
            else if (obj.TryGetValue("error", out var Error))
            {
                var error = JsonConvert.DeserializeObject<CreationStatus>(json);
                success = false;
                code = Error.Value<string>();
                message = error.Message;
                detail = error.Detail;
            }
            else
            {
                success = false;
                message = "Failed";
                detail = json;
            };
            return new()
            {
                Code = code,
                Message = message,
                Detail = detail,
                Success = success,
                Item = item ?? default
            };
        }

        private async Task<CreationStatus> DeleteItem(string Endpoint, string ItemID)
        {
            var json = await SendDeleteRequestAsync($"{Endpoint}/{ItemID}");
            if (string.IsNullOrWhiteSpace(json))
            {
                return new()
                {
                    Success = true
                };
            }
            else
            {
                return new()
                {
                    Success = false,
                    Item = json
                };
            }
        }

        private async Task<CreationStatus> UpdateItem<T>(string Endpoint, T item, string itemId)
        {
            var json = await SendPatchRequestAsync($"{Endpoint}/{itemId}", item);
            var obj = JObject.Parse(json);
            bool success = false;
            string code = string.Empty, message = string.Empty, detail = string.Empty;
            T itemType = default;
            if (obj.TryGetValue(".id", out var Id))
            {
                success = true;
                itemType = JsonConvert.DeserializeObject<T>(json);
            }
            else if (obj.TryGetValue("error", out var Error))
            {
                var error = JsonConvert.DeserializeObject<CreationStatus>(json);
                success = false;
                code = Error.Value<string>();
                message = error.Message;
                detail = error.Detail;
            }
            else
            {
                success = false;
                message = "Failed";
                detail = json;
            };
            return new()
            {
                Code = code,
                Message = message,
                Detail = detail,
                Success = success,
                Item = itemType
            };
        }

        private async Task<string> SendRequestBase(RequestMethod Method, string Endpoint, object? Data = null, bool IsTest = false, TimeSpan? timeout = null)
        {
            var stopwatch = Stopwatch.StartNew();
            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true
            };
            
            try
            {
                using HttpClient httpClient = new(handler);
                
                // Set timeout for startup validation (shorter) or use default
                if (timeout.HasValue)
                {
                    httpClient.Timeout = timeout.Value;
                }
                
                // Use SSL (https) on port 443 if MT_API_SSL is true, otherwise use http on port 80
                string protocol = MT_API_SSL ? "https" : "http";
                string port = MT_API_SSL ? ":443" : ":80";
                
                // Don't add port if it's already specified in MT_IP
                string baseUrl = MT_IP.Contains(":") ? $"{protocol}://{MT_IP}" : $"{protocol}://{MT_IP}{port}";
                string fullUrl = $"{baseUrl}/rest/{Endpoint}";
                
                // Log the request if verbose logging is enabled
                if (IsVerboseLoggingEnabled)
                {
                    if (Data != null)
                    {
                        string dataStr = (Data is string @string) ? @string : JsonConvert.SerializeObject(Data);
                        Console.WriteLine($"[MikrotikAPI] {Method} request to {fullUrl} with data: {dataStr}");
                    }
                    else
                    {
                        Console.WriteLine($"[MikrotikAPI] {Method} request to {fullUrl}");
                    }
                }
                
                using var request = new HttpRequestMessage(new HttpMethod(Method.ToString()), fullUrl);
                string base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{MT_USER}:{MT_PASS}"));
                if (!IsTest) request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                if (Data != null)
                {
                    string content = (Data is string @string) ? @string : JsonConvert.SerializeObject(Data);
                    request.Content = new StringContent(content);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                }

                HttpResponseMessage response = await httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                stopwatch.Stop();
                
                // Log the response if verbose logging is enabled
                if (IsVerboseLoggingEnabled)
                {
                    var truncatedResponse = responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent;
                    Console.WriteLine($"[MikrotikAPI] {Method} response from {fullUrl} ({stopwatch.ElapsedMilliseconds}ms): {truncatedResponse}");
                }
                
                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                string sslInfo = MT_API_SSL ? "with SSL (HTTPS)" : "without SSL (HTTP)";
                
                if (IsVerboseLoggingEnabled)
                {
                    Console.WriteLine($"[MikrotikAPI] {Method} request to {MT_IP} failed after {stopwatch.ElapsedMilliseconds}ms - {ex.Message}");
                }
                
                throw new HttpRequestException($"Failed to connect to Mikrotik API at {MT_IP} {sslInfo}. " +
                    $"Error: {ex.Message}. " +
                    $"Inner Exception: {ex.InnerException?.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                string sslInfo = MT_API_SSL ? "with SSL (HTTPS)" : "without SSL (HTTP)";
                
                if (IsVerboseLoggingEnabled)
                {
                    Console.WriteLine($"[MikrotikAPI] {Method} request to {MT_IP} timed out after {stopwatch.ElapsedMilliseconds}ms");
                }
                
                throw new TaskCanceledException($"Connection to Mikrotik API at {MT_IP} {sslInfo} timed out. " +
                    $"Error: {ex.Message}. " +
                    $"Inner Exception: {ex.InnerException?.Message}", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                string sslInfo = MT_API_SSL ? "with SSL (HTTPS)" : "without SSL (HTTP)";
                
                if (IsVerboseLoggingEnabled)
                {
                    Console.WriteLine($"[MikrotikAPI] {Method} request to {MT_IP} failed after {stopwatch.ElapsedMilliseconds}ms - {ex.GetType().Name}: {ex.Message}");
                }
                
                throw new Exception($"Unexpected error connecting to Mikrotik API at {MT_IP} {sslInfo}. " +
                    $"Error Type: {ex.GetType().Name}. " +
                    $"Error: {ex.Message}. " +
                    $"Inner Exception: {ex.InnerException?.Message}. " +
                    $"Stack Trace: {ex.StackTrace}", ex);
            }
        }

        private async Task<string> SendGetRequestAsync(string URL, bool IsTest = false, TimeSpan? timeout = null)
        {
            return await SendRequestBase(RequestMethod.GET, URL, IsTest: IsTest, timeout: timeout);
        }

        private async Task<string> SendPostRequestAsync(string URL, string Data)
        {
            return await SendRequestBase(RequestMethod.POST, URL, Data);
        }

        private async Task<string> SendDeleteRequestAsync(string URL)
        {
            return await SendRequestBase(RequestMethod.DELETE, URL);
        }

        private async Task<string> SendPutRequestAsync(string URL, object Data)
        {
            return await SendRequestBase(RequestMethod.PUT, URL, Data);
        }

        private async Task<string> SendPatchRequestAsync(string URL, object Data)
        {
            return await SendRequestBase(RequestMethod.PATCH, URL, Data);
        }
    }
}
