using System.Net;
using VinteR.Model;
using VinteR.Rest;

namespace VinteR.Streaming
{
    public delegate void PlayMocapFrameEventHandler(MocapFrame frame);

    public interface IStreamingServer : IRestServer
    {
        void Send(MocapFrame mocapFrame);

        void AddReceiver(IPEndPoint receiverEndPoint, IPEndPoint clientEndPoint = null);

        int Port { get; }
    }
}