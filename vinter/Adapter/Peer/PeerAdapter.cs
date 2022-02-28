using System;
using VinteR.Model;
using System.Collections.Concurrent;
using GenMocapFrame = VinteR.Model.Gen.MocapFrame;
using VinteR.Serialization;
using System.Net;
using System.Threading.Tasks;
using VinteR.Adapter.Network;
using System.Threading;

namespace VinteR.Adapter.Peer
{
    public class PeerAdapter : IInputAdapter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Serializer _serializer = new Serializer();
        private ConcurrentQueue<NetworkClient> clients = new ConcurrentQueue<NetworkClient>();

        public bool Enabled => Config.Enabled;

        public string Name => Config?.Name;

        public bool SendKeepAlive => Config.KeepAlive;

        private Configuration.Adapter _config;

        public string AdapterType => HardwareSystems.Peer;

        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;

        private long _keepAliveTimestamp;

        private static int KEEP_ALIVE_INTERVAL = 5000;
        private static string KEEP_ALIVE_HRRI = "*-KEEP_ALIVE";

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Configuration.Adapter Config
        {
            get => _config;
            set
            {
                if (value.AdapterType.Equals(AdapterType))
                {
                    _config = value;
                }
                else
                    OnError(new ApplicationException("Accepting only peer configuration"));
            }
        }

        public event MocapFrameAvailableEventHandler FrameAvailable;
        public event ErrorEventHandler ErrorEvent;

        public void AddClient(IPEndPoint clientEndPoint, IPEndPoint remoteEndPoint)
        {
            Logger.Debug("Adding NetworkClient in PeerAdapter on {0} listening to {1}", clientEndPoint, remoteEndPoint);
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
            Logger.Info("Starting NetworkAdapter");
            AddClient(_localEndPoint, _remoteEndPoint);

            if (SendKeepAlive)
            {
                StartSendKeepAliveTask(_cancellationTokenSource.Token);
                StartCheckKeepAliveTask(_cancellationTokenSource.Token);
            }
        }

        public void Stop()
        {
            foreach (NetworkClient client in clients)
            {
                client.Stop();
                _cancellationTokenSource.Cancel();
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

            if (data.AdapterType == HardwareSystems.Peer)
            {
                if (data.Bodies.Count == 0)
                {
                    return;
                }

                if (data.Bodies[0].Name == KEEP_ALIVE_HRRI)
                {
                    _keepAliveTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    return;
                }

                Logger.Warn("Received unknown meta frame");

                return;
            }

            _serializer.FromProtoBuf(data, out MocapFrame frame);
            OnFrameAvailable(frame);
        }

        public void SetEndPoints(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
        }

        private void StartCheckKeepAliveTask(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await Task.Delay(2 * KEEP_ALIVE_INTERVAL);
                    var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (now - _keepAliveTimestamp > 2 * KEEP_ALIVE_INTERVAL)
                    {
                        while (true)
                        {
                            await Task.Delay(1000);
                            Logger.Warn("Connection to Peer is gone. No incoming keep alive signals.");
                        }
                    }
                }
            });
        }

        private void StartSendKeepAliveTask(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                var keepAliveFrame = MakeKeepAliveFrame();
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await Task.Delay(KEEP_ALIVE_INTERVAL);
                    try
                    {
                        OnFrameAvailable(keepAliveFrame);
                    }
                    catch (Exception)
                    {
                        Logger.Warn("Could not send keep alive frame");
                    }
                }
            });
        }

        private MocapFrame MakeKeepAliveFrame()
        {
            GenMocapFrame genMocapFrame = new GenMocapFrame();
            genMocapFrame.AdapterType = HardwareSystems.Peer;
            genMocapFrame.SourceId = HardwareSystems.Peer;
            var protoBody = new GenMocapFrame.Types.Body()
            {
                BodyType = GenMocapFrame.Types.Body.Types.EBodyType.RigidBody,
                Rotation = new GenMocapFrame.Types.Body.Types.Quaternion(),
                SideType = GenMocapFrame.Types.Body.Types.ESideType.NoSide,
                Centroid = new GenMocapFrame.Types.Body.Types.Vector3(),
                Name = KEEP_ALIVE_HRRI
            };
            genMocapFrame.Bodies.Add(protoBody);
            _serializer.FromProtoBuf(genMocapFrame, out MocapFrame mocapFrame);
            return mocapFrame;
        }
    }
}
