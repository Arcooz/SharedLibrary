namespace Shared.Http;

using System.Collections;
using System.Net;
public delegate Task HttpMiddleware(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next);