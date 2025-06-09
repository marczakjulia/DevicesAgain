using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class DeviceAdditionalPropertiesValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DeviceAdditionalPropertiesValidationMiddleware> _logger;
        //parsed json provided in class
        private readonly JsonDocument _validationRules;

        public DeviceAdditionalPropertiesValidationMiddleware(
            RequestDelegate next,
            ILogger<DeviceAdditionalPropertiesValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _logger.LogInformation("Loading validation rules from example_validation_rules.json");
            var json = File.ReadAllText("example_validation_rules.json");
            _validationRules = JsonDocument.Parse(json);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var req = context.Request;
            //this means that it only applies to post and put for devices
            //meaning the rules will apply to create and update
            //otherwise we proceed as earlier
            if (!req.Path.StartsWithSegments("/api/devices") ||
                (req.Method != HttpMethods.Post && req.Method != HttpMethods.Put))
            {
                await _next(context);
                return;
            }
            context.Request.EnableBuffering();
            using var bodyDoc = await JsonDocument.ParseAsync(req.Body);
            // taken from the internet, so it can read the body 
            req.Body.Position = 0;
            var root = bodyDoc.RootElement;
            
            if (!root.TryGetProperty("isEnabled", out JsonElement enabled) || !enabled.GetBoolean())
            {
                _logger.LogWarning("Request blocked: isEnabled=false");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Device creation/update not allowed when isEnabled is false.");
                return;
            }
            
            if (!root.TryGetProperty("deviceTypeName", out JsonElement typeEl) ||
                typeEl.ValueKind != JsonValueKind.String)
            {
                _logger.LogWarning("Request missing deviceTypeName");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("deviceTypeName is required.");
                return;
            }
            var deviceType = typeEl.GetString()!;
            
            if (!root.TryGetProperty("additionalProperties",out JsonElement props) ||
                props.ValueKind != JsonValueKind.Object)
            {
                _logger.LogInformation("No additionalProperties found, skipping validation");
                await _next(context);
                return;
            }
            var validations = _validationRules.RootElement.GetProperty("validations").EnumerateArray();
            var validation = validations.FirstOrDefault(v =>
                v.GetProperty("type").GetString()
                 .Equals(deviceType, StringComparison.OrdinalIgnoreCase));

            if (validation.ValueKind == JsonValueKind.Object)
            {
                _logger.LogInformation("Applying {Count} rules for device type {DeviceType}",
                    validation.GetProperty("rules").GetArrayLength(), deviceType);

                foreach (var rule in validation.GetProperty("rules").EnumerateArray())
                {
                    var name = rule.GetProperty("paramName").GetString()!;
                    if (!props.TryGetProperty(name, out JsonElement valEl))
                    {
                        _logger.LogWarning("Missing required additional property: {Param}", name);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync($"Missing required additional property: {name}");
                        return;
                    }
                    var val = valEl.GetString() ?? string.Empty;
                    
                    if (rule.TryGetProperty("allowedValues", out JsonElement allowed) && allowed.ValueKind == JsonValueKind.Array)
                    {
                        var list = allowed.EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(s => s != null)
                            .ToList()!;
                        if (!list.Contains(val))
                        {
                            _logger.LogWarning("Invalid value for {Param}. Allowed: {Allowed}",
                                name, string.Join(", ", list));
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync($"Invalid value for {name}. Allowed: {string.Join(", ", list)}");
                            return;
                        }
                    }
                    else if (rule.TryGetProperty("regex", out JsonElement regexEl) && regexEl.ValueKind == JsonValueKind.String)
                    {
                        var pattern = regexEl.GetString()!;
                        if (!Regex.IsMatch(val, pattern))
                        {
                            _logger.LogWarning("Regex validation failed for {Param} with pattern {Pattern}", name, pattern);
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync($"Invalid value for {name} (regex: {pattern})");
                            return;
                        }
                    }
                }
            }

            _logger.LogInformation("Validation passed for {Method} {Path}", req.Method, req.Path.Value);
            await _next(context);
        }
    }
