using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace ITBees.UserManager.Services;

public class FasStartup
{
    /// <summary>
    /// If You want to track ip addresses from incoming requests You need to register web application with XForwardedFor marks. Run this method inside program.cs 
    /// </summary>
    /// <param name="app"></param>
    public static void EnableClientIpResolver(WebApplication app)
    {
        var forwardOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        forwardOptions.KnownNetworks.Clear();
        forwardOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardOptions);
    }
}