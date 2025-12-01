namespace Shared.Http;

using Shared.Config;
using System.Net;

public abstract class HttpServer
{
    protected HttpRouter router;
    protected HttpListener server;

    public HttpServer()
    {
        router = new HttpRouter();

        Init();

        string host = Configuration.Get<string>("HOST", "http://127.0.0.1");
        string port = Configuration.Get<string>("PORT", "5000");
        string authority = $"{host}:{port}/";

        server = new HttpListener();
        server.Prefixes.Add(authority);

        Console.WriteLine("Server started at " + authority);
    }

    public abstract void Init();

    public async Task Start()
    {
        server.Start();

        while (server.IsListening)
        {
            HttpListenerContext ctx = await server.GetContextAsync();

            _ = router.HandleContextAsync(ctx);
        }
    }

    public void Stop()
    {
        if (server.IsListening)
        {
            server.Stop();
            server.Close();
            Console.WriteLine("Server stopped.");
        }
    }
} 
