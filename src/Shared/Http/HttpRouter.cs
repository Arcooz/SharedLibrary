namespace Shared.Http;

using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Web;
public class HttpRouter
{
    public const int RESPONSE_NOT_SENT = 777;

    private static ulong requestId = 0;
    private string basePath;
    private List<HttpMiddleware> middlewares;
    private List<(string, string, HttpMiddleware[])> routes;

    public HttpRouter()
    {
        basePath = string.Empty;
        middlewares = [];
        routes = [];
    }

    // <-- Rest of the code below goes here. 

    public HttpRouter Use(params HttpMiddleware[] middlewares)
    {
        this.middlewares.AddRange(middlewares);

        return this;
    }

    public HttpRouter Map(string method, string path,
   params HttpMiddleware[] middlewares)
    {
        routes.Add((method.ToUpperInvariant(), path, middlewares));

        return this;
    }

    public HttpRouter MapGet(string path, params HttpMiddleware[] middlewares)
    {
        return Map("GET", path, middlewares);
    }

    public HttpRouter MapPost(string path, params HttpMiddleware[] middlewares)
    {
        return Map("POST", path, middlewares);
    }

    public HttpRouter MapPut(string path, params HttpMiddleware[] middlewares)
    {
        return Map("PUT", path, middlewares);
    }

    public HttpRouter MapDelete(string path, params HttpMiddleware[] middlewares)
    {
        return Map("DELETE", path, middlewares);
    }

    public async Task HandleContextAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        var props = new Hashtable();
        res.StatusCode = RESPONSE_NOT_SENT;
        props["req.id"] = ++requestId;
        try
        {
            await HandleAsync(req, res, props, () => Task.CompletedTask);
        }
        finally
        {
            if (res.StatusCode == RESPONSE_NOT_SENT)
            {
                res.StatusCode = (int)HttpStatusCode.NotImplemented;
            }
            res.Close();
        }
    }

    private async Task HandleAsync(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
    {
        Func<Task> globalMiddlewarePipeline =
        GenerateMiddlewarePipeline(req, res, props, middlewares);
        await globalMiddlewarePipeline();
        await next();
    }

    public HttpRouter UseRouter(string path, HttpRouter router)
    {
        router.basePath = this.basePath + path;
        return Use(router.HandleAsync);
    }

    private Func<Task> GenerateMiddlewarePipeline(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, List<HttpMiddleware> middlewares)
    {
        int index = -1;
        Func<Task> next = () => Task.CompletedTask;
        next = async () =>
        {
            index++;
            if (index < middlewares.Count && res.StatusCode == RESPONSE_NOT_SENT)
            {
                await middlewares[index](req, res, props, next);
            }
        };
        return next;
    }

    public HttpRouter UseSimpleRouteMatching()
    {
        return Use(SimpleRouteMatching);
    }

    public HttpRouter UseParametrizedRouteMatching()
    {
        return Use(ParametrizedRouteMatching);
    }

    private async Task SimpleRouteMatching(HttpListenerRequest req,
   HttpListenerResponse res, Hashtable props, Func<Task> next)
    {
        foreach (var (method, path, middlewares) in routes)
        {
            if (req.HttpMethod == method &&
              string.Equals(req.Url!.AbsolutePath, basePath + path)) // * 
            {
                Func<Task> routeMiddlewarePipeline =
                  GenerateMiddlewarePipeline(req, res, props, middlewares.ToList());

                await routeMiddlewarePipeline();

                break; // or return; // To short-circuit global pipeline. 
            }
        }

        await next();
    }

    private async Task ParametrizedRouteMatching(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
    {
        foreach (var (method, path, middlewares) in routes)
        {
            NameValueCollection? parameters;

            if (req.HttpMethod == method && (parameters =
              ParseUrlParams(req.Url!.AbsolutePath, basePath + path)) != null) // * 
            {
                props["req.params"] = parameters;

                Func<Task> routeMiddlewarePipeline =
                  GenerateMiddlewarePipeline(req, res, props, middlewares.ToList());

                await routeMiddlewarePipeline();

                break; // or return; // To short-circuit global pipeline. 
            }
        }

        await next();
    }

    public static NameValueCollection? ParseUrlParams(string uPath, string rPath)
    {
        string[] uParts = uPath.Trim('/').Split('/',
          StringSplitOptions.RemoveEmptyEntries);
        string[] rParts = rPath.Trim('/').Split('/',
          StringSplitOptions.RemoveEmptyEntries);

        if (uParts.Length != rParts.Length) { return null; }

        var parameters = new NameValueCollection();

        for (int i = 0; i < rParts.Length; i++)
        {
            string uPart = uParts[i];
            string rPart = rParts[i];

            if (rPart.StartsWith(":"))
            {
                string paramName = rPart.Substring(1);
                parameters[paramName] = HttpUtility.UrlDecode(uPart);
            }
            else if (uPart != rPart)
            {
                return null;
            }
        }
        return parameters;
    }
}