using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoMac.Foundation;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MonoMac.HttpClient
{
    public class NativeMessageHandler : HttpClientHandler
    {
        readonly Dictionary<string, string> headerSeparators =
            new Dictionary<string, string>()
            { 
                { "User-Agent", " " }
            };

        public NativeMessageHandler()
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            return Task.Run(() =>
                {
                    var headers = request.Headers as IEnumerable<KeyValuePair<string, IEnumerable<string>>>;

                    var rq = new NSMutableUrlRequest()
                    {
                        AllowsCellularAccess = true,
                        CachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringCacheData,
                        Headers = headers.Aggregate(new NSMutableDictionary(), (acc, x) =>
                            {
                                acc.Add(new NSString(x.Key), new NSString(String.Join(getHeaderSeparator(x.Key), x.Value)));
                                return acc;
                            }),
                        HttpMethod = request.Method.ToString().ToUpperInvariant(),
                        Url = NSUrl.FromString(request.RequestUri.AbsoluteUri),
                    };

                    NativeMessageConnectionDelegate connectionDelegate = new NativeMessageConnectionDelegate(request);
                    //var connection = NSUrlConnection.FromRequest(rq, connectionDelegate);
                    var connection = new NSUrlConnection(rq, connectionDelegate, false);
                    connection.Schedule(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
                    connection.Start();

                    connectionDelegate.wait();

                    return connectionDelegate.Response;
                });

            return Task.Run(() =>
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    using (NSUrl url = new NSUrl(request.RequestUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped)))
                    {
                        using (NSUrlRequest nsRequest = new NSUrlRequest(url, MonoMac.Foundation.NSUrlRequestCachePolicy.ReloadIgnoringCacheData, 60))
                        {
                            NSUrlResponse nsResponse;
                            NSError nsError;

                            NSData data = NSUrlConnection.SendSynchronousRequest(nsRequest, out nsResponse, out nsError);

                            if (data != null)
                            {
                                NSHttpUrlResponse nsHttpResponse = nsResponse as NSHttpUrlResponse;
                                if (nsHttpResponse != null)
                                {
                                    response.StatusCode = System.Net.HttpStatusCode.OK;
                                    foreach (var header in nsHttpResponse.AllHeaderFields)
                                    {
                                        if (header.Key != null && header.Value != null)
                                        {
                                            response.Headers.TryAddWithoutValidation(header.Key.ToString(), header.Value.ToString());
                                            //response.Content.Headers.TryAddWithoutValidation(header.Key.ToString(), header.Value.ToString());
                                        }
                                    }
                                }
                            }
                            else
                            {
                                
                            }
                        }
                    }
                    return response;
                });
        }

        string getHeaderSeparator(string name)
        {
            if (headerSeparators.ContainsKey(name))
            {
                return headerSeparators[name];
            }

            return ",";
        }
    }
}

