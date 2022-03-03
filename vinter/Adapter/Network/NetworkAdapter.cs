using System;
using VinteR.Model;
using System.Collections.Concurrent;
using GenMocapFrame = VinteR.Model.Gen.MocapFrame;
using VinteR.Serialization;
using System.Net;
using VinteR.Adapter.Peer;

namespace VinteR.Adapter.Network
{
    public class NetworkAdapter : IInputAdapter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Serializer _serializer = new Serializer();
        private ConcurrentQueue<NetworkClient> clients = new ConcurrentQueue<NetworkClient>();

        public bool Enabled => Config.Enabled;

        public string Name => Config?.Name;

        private Configuration.Adapter _config;

        public string AdapterType => HardwareSystems.HoloRoom;

        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;

        public Configuration.Adapter Config
        {
            get => _config;
            set
            {
                if (value.AdapterType.Equals(AdapterType))
                    _config = value;
                else
                    OnError(new ApplicationException("Accepting only holo room configuration"));
            }
        }

        public event MocapFrameAvailableEventHandler FrameAvailable;
        public event ErrorEventHandler ErrorEvent;
        
        public void AddClient(IPEndPoint clientEndPoint, IPEndPoint remoteEndPoint)
        {
            Logger.Debug("Adding NetworkClient in NetworkAdapter on {0} listening to {1}", clientEndPoint, remoteEndPoint);
            NetworkClient client = new NetworkClient();
            try
            {
                client.Start(clientEndPoint, remoteEndPoint);
                client.OnFrameReady += OnClientFrameReady;
            }
            catch (ApplicationException e)
            {
                client.OnFrameReady -= OnClientFrameReady;
                OnError(e);
            }
            clients.Enqueue(client);
        }

        public void Run()
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, _config.ClientPort);
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            Logger.Info("Starting NetworkAdapter");
            AddClient(_localEndPoint, _remoteEndPoint);
        }

        public void Stop()
        {
            foreach (NetworkClient client in clients)
            {
                client.Stop();
            }
        }

        public virtual void OnFrameAvailable(MocapFrame frame)
        {
            FrameAvailable?.Invoke(this, frame);
        }

        public virtual void OnError(Exception e)
        {
            // Raise an Error Event
            ErrorEvent?.Invoke(this, e);
        }

        public void OnClientFrameReady(GenMocapFrame data)
        {
            _serializer.FromProtoBuf(data, out MocapFrame frame);
            OnFrameAvailable(frame);
        }
    }
}
