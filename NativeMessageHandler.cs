using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoMac.Foundation;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;

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
                    using(var ms = new MemoryStream())
                    {
                        if(request.Content != null)
                        {
                            var t = request.Content.CopyToAsync(ms);
                            t.Wait();
                            headers = headers.Union(request.Content.Headers);
                        }

                        var rq = new NSMutableUrlRequest()
                        {
                            AllowsCellularAccess = true,
                            Body = NSData.FromArray(ms.ToArray()),
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
                        var connection = new NSUrlConnection(rq, connectionDelegate, false);
                        connection.Schedule(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
                        connection.Start();

                        connectionDelegate.wait();

                        return connectionDelegate.Response;
                    }
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

