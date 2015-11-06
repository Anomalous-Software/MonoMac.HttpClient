using System;
using MonoMac.Foundation;
using System.Threading;
using System.Net.Http;
using System.Net;
using ModernHttpClient;

namespace MonoMac.HttpClient
{
    class NativeMessageConnectionDelegate : NSUrlConnectionDelegate
    {
        private HttpResponseMessage responseMessage;
        private CancellableStreamContent content;
        private ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);
        private ByteArrayListStream stream;
        private bool isComplete = false;

        public NativeMessageConnectionDelegate(HttpRequestMessage request)
        {
            this.responseMessage = new HttpResponseMessage();
            responseMessage.RequestMessage = request; //This may not be right since it won't handle redirects
        }

        protected override void Dispose(bool disposing)
        {
            waitEvent.Dispose();
            base.Dispose(disposing);
        }

        public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
        {
            try
            {
                responseMessage.RequestMessage.RequestUri = new Uri(response.Url.ToString()); //At least attempt to get the url right if we redirected
                var httpResponse = response as NSHttpUrlResponse;
                if (httpResponse != null)
                {
                    stream = new ByteArrayListStream();
                    content = new CancellableStreamContent(stream, () =>
                    {
                        isComplete = true;
                        stream.SetException(new OperationCanceledException());
                    });
                    responseMessage.StatusCode = (HttpStatusCode)httpResponse.StatusCode;
                    responseMessage.Content = content;

                    foreach (var header in httpResponse.AllHeaderFields)
                    {
                        if (header.Key != null && header.Value != null)
                        {
                            responseMessage.Headers.TryAddWithoutValidation(header.Key.ToString(), header.Value.ToString());
                            responseMessage.Content.Headers.TryAddWithoutValidation(header.Key.ToString(), header.Value.ToString());
                        }
                    }
                }
            }
            finally
            {
                waitEvent.Set();
            }
        }

        public override void ReceivedData(NSUrlConnection connection, NSData data)
        {
            using (var dataStream = data.AsStream())
            {
                byte[] bytes = new byte[dataStream.Length];
                int offset = 0;
                int read = 0;
                do
                {
                    read = dataStream.Read(bytes, offset, bytes.Length - offset);
                }
                while(read > 0);

                stream.AddByteArray(bytes);
            }
        }

        public override void FailedWithError(NSUrlConnection connection, NSError error)
        {
            if (stream != null)
            {
                stream.Complete();
            }
            waitEvent.Set();
        }

        public override void FinishedLoading(NSUrlConnection connection)
        {
            if (stream != null)
            {
                stream.Complete();
            }
            waitEvent.Set();
        }

        public void wait()
        {
            waitEvent.Wait();
        }

        public HttpResponseMessage Response
        {
            get
            {
                return responseMessage;
            }
        }
    }
}

