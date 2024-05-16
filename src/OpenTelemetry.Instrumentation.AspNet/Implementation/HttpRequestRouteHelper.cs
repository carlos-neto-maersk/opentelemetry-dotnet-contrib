// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Routing;

namespace OpenTelemetry.Instrumentation.AspNet.Implementation;

/// <summary>
/// Helper class for processing http requests.
/// </summary>
internal sealed class HttpRequestRouteHelper
{
    private readonly PropertyFetcher<object> routeFetcher = new("Route");
    private readonly PropertyFetcher<string> routeTemplateFetcher = new("RouteTemplate");

    /// <summary>
    /// Extracts the route template from the <see cref="HttpRequest"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> being processed.</param>
    /// <returns>The route template or <see langword="null"/>.</returns>
    internal string? GetRouteTemplate(HttpRequest request)
    {
        var routeData = request.RequestContext.RouteData;

        string? template = null;
        if (routeData.Values.TryGetValue("MS_SubRoutes", out object msSubRoutes))
        {
            // WebAPI attribute routing flows here. Use reflection to not take a dependency on microsoft.aspnet.webapi.core\[version]\lib\[framework]\System.Web.Http.

            if (msSubRoutes is not Array subRoutes)
            {
                return template;
            }

            // HashSet allows to filter duplicates (for different HTTP methods)
            HashSet<string> templates = new();
            foreach (var subRoute in subRoutes)
            {
                _ = this.routeFetcher.TryFetch(subRoute, out var route);
                _ = this.routeTemplateFetcher.TryFetch(route, out var subTemplate);
                if (subTemplate != null)
                {
                    templates.Add(subTemplate);
                }
            }

            // Different routes matched - path parameters are englobing several routes
            if (templates.Count > 1)
            {
                return template;
            }

            // To avoid linq, that instantiates a new enumerator instead of using hashset one as foreach
            foreach (var x in templates)
            {
                template = x;
                break;
            }
        }
        else if (routeData.Route is Route route)
        {
            // MVC + WebAPI traditional routing & MVC attribute routing flow here.
            template = route.Url;
        }

        return template;
    }
}
