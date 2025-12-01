namespace Shared.Http;

using System.Collections;
using System.Net;

public class Result<T>
{
    public bool IsError { get; }
    public Exception? Error { get; }
    public T? Payload { get; }
    public int StatusCode { get; }

    public Result(Exception error, int statusCode =
     (int)HttpStatusCode.InternalServerError)
    {
        IsError = true;
        Error = error;
        Payload = default(T);
        StatusCode = statusCode;
    }

    public Result(T payload, int statusCode = (int)HttpStatusCode.OK)
    {
        IsError = false;
        Error = null;
        Payload = payload;
        StatusCode = statusCode;
    }

#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
    public static async Task SendResultResponse<T>(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Result<T> result)
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
    {
        if (result.IsError)
        {
            res.Headers["Cache-Control"] = "no-store";

            await HttpUtils.SendResponse(req, res, props, result.StatusCode,
              result.Error!.ToString()!);
        }
        else
        {
            await HttpUtils.SendResponse(req, res, props, result.StatusCode,
              result.Payload!.ToString()!);
        }
    }
} 
