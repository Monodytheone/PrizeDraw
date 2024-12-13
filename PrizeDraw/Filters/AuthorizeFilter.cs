using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrizeDraw.Filters;

public class AuthorizeFilter : IAuthorizationFilter
{
    private readonly string _actionToken;

    public AuthorizeFilter(IConfiguration configuration)
    {
        _actionToken = configuration["ActionToken"] ?? throw new InvalidOperationException("actionToken configuration is missing.");
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // 检查是否标记了 AllowAnonymousAttribute
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint != null && endpoint.Metadata.Any(metadata => metadata is AllowAnonymousAttribute))
        {
            return; // 允许匿名访问
        }

        // 检查请求头中是否包含 "Token"
        if (!context.HttpContext.Request.Headers.TryGetValue("Token", out var tokenValue))
        {
            //context.Result = new UnauthorizedResult(); // 请求未授权
            context.Result = new ContentResult()
            {
                Content = "没有Token请求头",
                ContentType = "application/json",
                StatusCode = 401
            };
            return;
        }

        // 验证 Token 是否与配置的 actionToken 匹配
        if (!string.Equals(tokenValue, _actionToken, StringComparison.Ordinal))
        {
            //context.Result = new UnauthorizedResult(); // 请求未授权
            context.Result = new ContentResult()
            {
                Content = "Token不正确",
                ContentType = "application/json",
                StatusCode = 401
            };
            return;
        }
    }
}
