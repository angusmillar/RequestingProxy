namespace Abm.Requesting.Proxy.Services;

public interface IRequestClaimService
{
    Task Proces(Microsoft.AspNetCore.Http.HttpContext httpContent);
}