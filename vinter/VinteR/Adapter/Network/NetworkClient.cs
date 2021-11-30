using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GenMocapFrame = VinteR.Model.Gen.MocapFrame;
using System.Text;

namespace VinteR.Adapter.Network
{
    public delegate void NetworkFrameReadyEventHandler(GenMocapFrame mocapData);

    public interface INetworkClient
    {
        event NetworkFrameReadyEventHandler OnFrameReady;

        void Start(IPEndPoint clientEndPoint, IPEndPoint remoteEndPoint);

        void Stop();
    }

    public class NetworkClient : INetworkClient
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public event NetworkFrameReadyEventHandler OnFrameReady;
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private Thread UdpClientThread;

        public void Start(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            _udpClient = new UdpClient(localEndPoint);
            _remoteEndPoint = remoteEndPoint;

            UdpClientThread = new Thread(new ThreadStart(ListenForMocapData));
            UdpClientThread.IsBackground = true;
            UdpClientThread.Start();

            Logger.Info("Started NetworkClient on port {0}", localEndPoint.Port);
        }

        public void Stop()
        {
            Logger.Debug("Stopping NetworkClient on {0} listening to {1}", _udpClient.Client.LocalEndPoint, _remoteEndPoint);
            _udpClient.Close();
        }

        void ListenForMocapData()
        {
            while (true)
            {
                byte[] data = null;
                try
                {
                    data = _udpClient.Receive(ref _remoteEndPoint);
                    var genMocapFrame = GenMocapFrame.Parser.ParseFrom(data);
                    //Logger.Info("Received data from {0}", genMocapFrame.Bodies[0].Name.Split('-')[0]);
                    OnFrameReady(genMocapFrame);
                }
                catch (InvalidOperationException e)
                {
                    Logger.Info("Could not decode message: {0}", Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    Logger.Debug(e);
                    _udpClient.Close();
                    return;
                }
            }

        }
    }
}