using System.Text;

namespace ReverseProxy
{
    public class ReverseProxyMiddleware
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly RequestDelegate _nextMiddleware;
        private readonly IConfiguration _configuration;

        public ReverseProxyMiddleware(RequestDelegate nextMiddleware,
            IConfiguration configuration)
        {
            _nextMiddleware = nextMiddleware;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var targetUri = BuildTargetUri(context.Request);

            if (targetUri != null)
            {
                var targetRequestMessage = CreateTargetMessage(context, targetUri);

                using (var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                {
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    CopyFromTargetResponseHeaders(context, responseMessage);
                    await ProcessResponseContent(context, responseMessage);
                }
                return;
            }
            await _nextMiddleware(context);
        }
        private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsByteArrayAsync();
            if (TryBuildResponseContent(responseMessage, content, out var newContent))
            {
                await context.Response.WriteAsync(newContent, Encoding.UTF8);
            }
            else
            {
                await context.Response.Body.WriteAsync(content);
            }
        }

        private bool TryBuildResponseContent(HttpResponseMessage responseMessage, byte[] content, out string newContent)
        {

            Dictionary<string, string?> hostsUrls = new();
            Dictionary<string, string?> contentOfType = new();
            _configuration.GetSection("HostsUrls").Bind(hostsUrls);
            _configuration.GetSection("ContentOfType").Bind(contentOfType);

            newContent = "";
            var stringContent = Encoding.UTF8.GetString(content);
            foreach (var item in contentOfType)
            {
                if (IsContentOfType(responseMessage, item.Value ?? ""))
                {
                    foreach (var host in hostsUrls)
                    {
                        if (!string.IsNullOrWhiteSpace(host.Value))
                        {
                            newContent = stringContent.Replace(host.Value, "/" + host.Key);
                        }
                    }
                    break;
                }
            }

            return !string.IsNullOrWhiteSpace(newContent);
        }

        private bool IsContentOfType(HttpResponseMessage responseMessage, string type)
        {
            var result = false;

            if (responseMessage.Content?.Headers?.ContentType != null)
            {
                result = responseMessage.Content.Headers.ContentType.MediaType == type;
            }

            return result;
        }
        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();

            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            //targetUri = new Uri(QueryHelpers.AddQueryString(targetUri.OriginalString,
            //           new Dictionary<string, string?>() { { "entry.1884265043", "John Doe" } }));

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }

        private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
              !HttpMethods.IsHead(requestMethod) &&
              !HttpMethods.IsDelete(requestMethod) &&
              !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }
            if (requestMessage.Content is null)
            {
                requestMessage.Content = new StringContent("");
            }
            foreach (var header in context.Request.Headers)
            {
                var res = requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            context.Response.Headers.Remove("transfer-encoding");
        }
        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }
        private Uri? BuildTargetUri(HttpRequest request)
        {
            var hostsUrls = new Dictionary<string, string?>();
            _configuration.GetSection("HostsUrls").Bind(hostsUrls);

            Uri? targetUri = null;
            PathString remainingPath;

            foreach (var item in hostsUrls)
            {
                if (request.Path.StartsWithSegments($"/{item.Key}", out remainingPath))
                {
                    var queryString = request.QueryString.HasValue ? request.QueryString.Value : "";
                    targetUri = new Uri((item.Value ?? "") + remainingPath + queryString);
                    break;
                }
            }

            return targetUri;
        }
    }
}
}
