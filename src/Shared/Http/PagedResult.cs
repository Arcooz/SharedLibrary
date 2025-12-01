using System.Collections;
using System.Net;

namespace Shared.Http;

public class PagedResult<T>
{
  public int TotalCount { get; }
  public List<T> Values { get; }

  public PagedResult(int totalCount, List<T> values)
  {
    TotalCount = totalCount;
    Values = values;
  }

  public static async Task SendPagedResultResponse<T>(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Result<PagedResult<T>> result, int page, int size)
  {
    if (result.IsError)
    {
      res.Headers["Cache-Control"] = "no-store";

      await HttpUtils.SendResponse(req, res, props, result.StatusCode,
        result.Error!.ToString()!);
    }
    else
    {
      var pagedResult = result.Payload!;

      HttpUtils.AddPaginationHeaders(req, res, props, pagedResult, page, size);

      await HttpUtils.SendResponse(req, res, props, result.StatusCode,
        result.Payload!.ToString()!);
    }
  }

  public static void AddPaginationHeaders<T>(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, PagedResult<T> pagedResult, int page, int size)
  {
    var baseUrl =
      $"{req.Url!.Scheme}://{req.Url!.Authority}{req.Url!.AbsolutePath}";
    int totalPages =
      Math.Max(1, (int)Math.Ceiling((double)pagedResult.TotalCount / size));

    string self =
      $"{baseUrl}?page={page}&size={size}";
    string? first =
      page == 1 ? null : $"{baseUrl}?page={1}&size={size}";
    string? last =
      page == totalPages ? null : $"{baseUrl}?page={totalPages}&size={size}";
    string? prev =
      page > 1 ? $"{baseUrl}?page={page - 1}&size={size}" : null;
    string? next =
      page < totalPages ? $"{baseUrl}?page={page + 1}&size={size}" : null;

    res.Headers["Content-Type"] = "application/json; charset=utf-8";
    res.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString();
    res.Headers["X-Page"] = page.ToString();
    res.Headers["X-Page-Size"] = size.ToString();
    res.Headers["X-Total-Pages"] = totalPages.ToString();

    // Optional RFC 5988 Link header for discoverability 

    var linkParts = new List<string>();


    if (prev != null) { linkParts.Add($"<{prev}>;  rel=\"prev\""); }
    if (next != null) { linkParts.Add($"<{next}>;  rel=\"next\""); }
    if (first != null) { linkParts.Add($"<{first}>; rel=\"first\""); }
    if (last != null) { linkParts.Add($"<{last}>;  rel=\"last\""); }

    if (linkParts.Count > 0)
    {
      res.Headers["Link"] = string.Join(", ", linkParts);
    }
  }
}
