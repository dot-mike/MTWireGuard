using Newtonsoft.Json;

namespace MikrotikAPI
{
    public static class Extensions
    {
        public static T ToModel<T>(this string str)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(str)) return default;
                
                // Check if this is an authentication error response
                if (str.Contains("\"error\":401") && str.Contains("\"message\":\"Unauthorized\""))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[-] Authentication failed: Invalid username or password");
                    Console.WriteLine("[!] Please check your MT_USER and MT_PASS environment variables");
                    Console.ResetColor();
                }
                else if (str.Contains("\"error\":") && str.Contains("\"message\":"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[!] MikroTik API returned an error: {str}");
                    Console.ResetColor();
                }
                
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch(JsonSerializationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] JSON Deserialization Error: {ex.Message}");
                Console.WriteLine($"[!] Raw response: {str}");
                Console.ResetColor();
                return default;
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] Unexpected error in ToModel: {ex.Message}");
                Console.WriteLine($"[!] Raw response: {str}");
                Console.ResetColor();
                return default;
            }
        }
    }
}
