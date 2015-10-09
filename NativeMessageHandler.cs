using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoMac.Foundation;

namespace MonoMac.HttpClient
{
    public class NativeMessageHandler : HttpClientHandler
    {
        public NativeMessageHandler()
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = System.Net.HttpStatusCode.NotFound;
                    using(NSUrl url = new NSUrl(request.RequestUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped)))
                    {
                        using(NSUrlRequest nsRequest = new NSUrlRequest(url, MonoMac.Foundation.NSUrlRequestCachePolicy.ReloadIgnoringCacheData, 60))
                        {
                            NSUrlResponse nsResponse;
                            NSError nsError;

                            NSData data = NSUrlConnection.SendSynchronousRequest(nsRequest, out nsResponse, out nsError);

                            if(data != null)
                            {
                                NSHttpUrlResponse nsHttpResponse = nsResponse as NSHttpUrlResponse;
                                if(nsHttpResponse != null)
                                {
                                    response.StatusCode = System.Net.HttpStatusCode.OK;
                                    foreach(var header in nsHttpResponse.AllHeaderFields)
                                    {
                                        if(header.Key != null && header.Value != null)
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
    }
}

