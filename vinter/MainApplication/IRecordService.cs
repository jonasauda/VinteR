using VinteR.Model;
using VinteR.Streaming;
using System.Net;

namespace VinteR.MainApplication
{
    public interface IRecordService
    {
        event PlayMocapFrameEventHandler FrameAvailable;

        Session Session { get; }

        Session Start();

        void Stop();

        void StartPeerAdapter(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint);
    }
}