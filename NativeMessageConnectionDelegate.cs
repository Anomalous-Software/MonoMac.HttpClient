using System;
using MonoMac.Foundation;
using System.Threading;

namespace MonoMac.HttpClient
{
    class NativeMessageConnectionDelegate : NSUrlConnectionDelegate
    {
        private NativeMessageHandler handler;
        private ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);

        public NativeMessageConnectionDelegate(NativeMessageHandler handler)
        {
            this.handler = handler;
        }

        protected override void Dispose(bool disposing)
        {
            waitEvent.Dispose();
            base.Dispose(disposing);
        }

        public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
        {
            Console.WriteLine("received");
            //throw new System.NotImplementedException ();
            //waitEvent.Set();
        }

        public override void FailedWithError(NSUrlConnection connection, NSError error)
        {
            waitEvent.Set();
        }

        public override void FinishedLoading(NSUrlConnection connection)
        {
            waitEvent.Set();
        }

        public override void ReceivedAuthenticationChallenge(NSUrlConnection connection, NSUrlAuthenticationChallenge challenge)
        {
            throw new System.NotImplementedException ();
        }

        public void wait()
        {
            waitEvent.Wait();
        }
    }
}

