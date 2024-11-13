using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Yarp.ReverseProxy.Model;
using Task = System.Threading.Tasks.Task;

namespace Abm.Requesting.Proxy.Services;

public class RequestClaimService(
    ILogger<RequestClaimService> logger) : IRequestClaimService
{
    private HttpContext? _httpContext;
    private string? _requestBody;
    private Resource? _requestResource;
    

    public async Task Proces(HttpContext httpContext)
    {
        _httpContext = httpContext;
        
        try
        {
            InspectRequestFeatures(httpContext);

            httpContext.Request.EnableBuffering();
            
            string[] httpMethodsWithBodies = [HttpMethod.Put.Method, HttpMethod.Post.Method];
            if (httpMethodsWithBodies.Contains(httpContext.Request.Method, StringComparer.OrdinalIgnoreCase))
            {
                _requestBody = await GetRequestBody(httpContext);
            }

            httpContext.Request.Body.Position = 0;
            
            if (string.IsNullOrEmpty(_requestBody))
            {
                return;
            }
            
            _requestResource = JsonSerializer.Deserialize<Resource>(_requestBody, GetFhirOptions());
            ArgumentNullException.ThrowIfNull(_requestResource?.TypeName);
            
            logger.LogInformation("Resource: {Stuff}", _requestResource.TypeName ?? "[None]");
            
            ResourceType denyResourceType = ResourceType.Observation;
            if (_requestResource.TypeName!.Equals(denyResourceType.GetLiteral()))
            {
                await SetWrongResourceTypeResponse(denyResourceType);
                return;
            }
            
            await ResetRequestBody();
        }
        catch (Exception e)
        {
            await SetUnhandledExceptionResponse(e.Message);
            throw;
        }
    }

    private async Task ResetRequestBody()
    {
        if (string.IsNullOrEmpty(_requestBody))
        {
            return;
        }
        
        ArgumentNullException.ThrowIfNull(_httpContext);
        var requestContent = new StringContent(_requestBody, Encoding.UTF8);
        _httpContext.Request.Body = await requestContent.ReadAsStreamAsync();
    }

    private async Task SetUnhandledExceptionResponse(string message)
    {
        ArgumentNullException.ThrowIfNull(_httpContext);
        int statusCode = StatusCodes.Status400BadRequest;
        await SetOperationOutComeResponse($"RequestingProxy Unhandled Exception: {message}", statusCode);
    }
    
    private async Task SetWrongResourceTypeResponse(ResourceType resourceType)
    {
        ArgumentNullException.ThrowIfNull(_httpContext);
        string message = $"Proxy intercept: Can receive {resourceType.GetLiteral()} resources";
        int statusCode = StatusCodes.Status401Unauthorized;
        await SetOperationOutComeResponse(message, statusCode);
    }
    
    private async Task SetOperationOutComeResponse(string message, int statusCodeInt)
    {
        ArgumentNullException.ThrowIfNull(_httpContext);
        _httpContext.Response.StatusCode = statusCodeInt;
        _httpContext.Response.Headers.Append("Content-Type", Hl7.Fhir.Rest.ContentType.JSON_CONTENT_HEADER);
        await _httpContext.Response.WriteAsync(await GetOperationOutcome(message));
    }

    private async Task<string> GetOperationOutcome(string message)
    {
        var operationOutcome = new OperationOutcome()
        {
            Issue = new List<OperationOutcome.IssueComponent>()
            {
                new OperationOutcome.IssueComponent()
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Exception,
                    Diagnostics = message
                }
            }
            
        };
        var opt = FhirJsonSerializationSettings.CreateDefault();
        opt.Pretty = true;

        return await operationOutcome.ToJsonAsync(opt);
    }
    
    private static void InspectRequestFeatures(
        HttpContext httpContext)
    {
        IReverseProxyFeature proxyFeature = httpContext.GetReverseProxyFeature();

        var route = proxyFeature.Route;
        var cluster = proxyFeature.Cluster;
        foreach (var destination in proxyFeature.AllDestinations)
        {
        }
    }

    private async Task<string> GetRequestBody(
        HttpContext httpContext)
    {
        using StreamReader sr = new StreamReader(httpContext.Request.Body, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await sr.ReadToEndAsync();
    }


    private JsonSerializerOptions GetFhirOptions()
    {
        return new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);
        ;
    }
}