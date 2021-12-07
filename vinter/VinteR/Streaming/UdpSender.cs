using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using VinteR.Configuration;
using VinteR.Model;
using VinteR.Serialization;

namespace VinteR.Streaming
{
    public class UdpSender : IStreamingServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public IList<UdpReceiver> UdpReceivers { get; set; }

        public int Port { get; }

        public Hrri _systemHrri { get; }

        public bool _sendKeepAlive { get; }

        private ConcurrentQueue<HrriEndPoint> _localEndPoints;

        private ConcurrentQueue<HrriEndPoint> _remoteEndPoints;

        private UdpClient _localUdpClient;

        private UdpClient _remoteUdpClient;

        private readonly ISerializer _serializer;


        private static Model.Gen.MocapFrame makeDummyFrame(string hrri)
        {
            Model.Gen.MocapFrame mocapFrame = new Model.Gen.MocapFrame();
            mocapFrame.AdapterType = HardwareSystems.Peer;
            mocapFrame.SourceId = HardwareSystems.Peer;
            var protoBody = new Model.Gen.MocapFrame.Types.Body()
            {
                BodyType = Model.Gen.MocapFrame.Types.Body.Types.EBodyType.RigidBody,
                Rotation = new Model.Gen.MocapFrame.Types.Body.Types.Quaternion(),
                SideType = Model.Gen.MocapFrame.Types.Body.Types.ESideType.NoSide,
                Centroid = new Model.Gen.MocapFrame.Types.Body.Types.Vector3(),
                Name = hrri
            };
            mocapFrame.Bodies.Add(protoBody);
            return mocapFrame;
        }

        public UdpSender(IConfigurationService configurationService, ISerializer serializer)
        {
            UdpReceivers = configurationService.GetConfiguration().UdpReceivers;
            var config = configurationService.GetConfiguration().Adapters
                .Where(a => a.AdapterType.Equals(HardwareSystems.HoloRoom))
                .DefaultIfEmpty(null)
                .FirstOrDefault();
            if (config == null)
                throw new ApplicationException("No holoroom config with global root given");

            _systemHrri = new Hrri(config.Hrri);
            _sendKeepAlive = config.KeepAlive;

            _serializer = serializer;
        }

        public void Send(MocapFrame mocapFrame)
        {
            Task.Factory.StartNew(() =>
            {
                _serializer.ToProtoBuf(mocapFrame, out var frame);

                string hrriString;
                // TODO: filter out all protocal meta data
                if (mocapFrame.AdapterType == HardwareSystems.Peer)
                {
                    hrriString = frame.Bodies[0].Name;
                }
                else if (mocapFrame.AdapterType == HardwareSystems.OptiTrack)
                {
                    //TODO: No idea if this is a good idea or solves the problem - Marvin
                    hrriString = HardwareSystems.OptiTrack;
                }
                else if (mocapFrame.AdapterType == HardwareSystems.LeapMotion)
                {
                    //TODO: No idea if this is a good idea or solves the problem - Marvin
                    hrriString = HardwareSystems.LeapMotion;
                }
                else
                {
                    // is there a reason Hrris are associated with bodies, not frames?
                    // are frames ever merged together, so bodies of the same frame have different Hrris?
                    var firstBodyWithHrri = frame.Bodies.FirstOrDefault(b => Hrri.IsWellFormedOrigin(b.Name));
                    if (firstBodyWithHrri == null)
                    {
                        Logger.Error("Could not find valid origin Hrri in MocapFrame");
                        Logger.Debug("body count: {0}", frame.Bodies.Count);
                        if (frame.Bodies.Count > 0) Logger.Debug("first body: {0}", frame.Bodies[0].Name);
                        return;
                    }
                    hrriString = firstBodyWithHrri.Name;
                }
                Hrri dataHrri = new Hrri(hrriString);

                if (dataHrri.Location == _systemHrri.Location || dataHrri.Location == "*")
                {
                    // local data origin
                    // send data to all remote endpoints and every local one except the origin
                    _remoteEndPoints.ToList().ForEach(e => SendFrame(frame, e, dataHrri, true));
                    _localEndPoints.Where(e => e.Hrri.Group != dataHrri.Group)
                        .ToList().ForEach(e => SendFrame(frame, e, dataHrri));
                }
                else
                {
                    // remote data origin
                    // send data to all local endpoints
                    _localEndPoints.ToList().ForEach(e => SendFrame(frame, e, dataHrri));
                }
            });
        }

        void SendFrame(Model.Gen.MocapFrame frame, HrriEndPoint hrriEndPoint, Hrri dataHrri, bool remote = false)
        {
            Task.Factory.StartNew(() =>
            {
                var data = frame.ToByteArray();
                try
                {
                    if (remote)
                    {
                        //Logger.Debug("Sending remote frame | from {0} to {1}", _remoteUdpClient.Client.LocalEndPoint, hrriEndPoint.IpEndPoint);
                        //Logger.Debug("Sending remote frame");
                        _remoteUdpClient.Send(data, data.Length, hrriEndPoint.IpEndPoint);
                    }
                    else
                    {
                        //Logger.Debug("Sending local frame  | to {0}", hrriEndPoint.IpEndPoint);
                        _localUdpClient.Send(data, data.Length, hrriEndPoint.IpEndPoint);
                    }
                }
                catch (Exception)
                {
                    Logger.Warn("Could not send frame {0,8} to {1}", frame.ElapsedMillis, hrriEndPoint.IpEndPoint.Address);
                }
            });
        }

        void SendDataAfterLongPause(HrriEndPoint hrriEndPoint)
        {
            Task.Factory.StartNew(async () =>
            {
                var dummyFrame = makeDummyFrame("DUMMYFRAME");
                while (true)
                {
                    await Task.Delay(120000);
                    try
                    {
                        while (true)
                        {
                            await Task.Delay(1000);
                            Logger.Debug("Sending dummy data");
                            _remoteEndPoints.ToList().ForEach(e => SendFrame(dummyFrame, e, hrriEndPoint.Hrri, true));
                        }
                    }
                    catch (Exception)
                    {
                        Logger.Warn("Could not send keep alive frame");
                    }
                }
            });
        }

        public void AddReceiver(IPEndPoint receiverEndPoint, IPEndPoint clientEndPoint = null)
        {
            Logger.Debug("Adding receiverEndPoint {0} with clientEndPoint {1}", receiverEndPoint, clientEndPoint);
            if (clientEndPoint != null)
            {
                // TODO: refactor
                _remoteUdpClient = new UdpClient(clientEndPoint);
                HrriEndPoint hrriEndPoint = new HrriEndPoint(receiverEndPoint, _systemHrri.Location == "MUC" ? new Hrri("ESS") : new Hrri("MUC"));
                _remoteEndPoints.Enqueue(hrriEndPoint);

                return;
            }
            else if (_localEndPoints.ToList().Where(e => e.IpEndPoint.Equals(receiverEndPoint)).Count() == 0)
            {
                HrriEndPoint hrriEndPoint = new HrriEndPoint(receiverEndPoint, new Hrri(""));
                _localEndPoints.Enqueue(hrriEndPoint);
            }
        }

        public void Start()
        {
            _localUdpClient = new UdpClient();
            _localEndPoints = new ConcurrentQueue<HrriEndPoint>();
            _remoteEndPoints = new ConcurrentQueue<HrriEndPoint>();
            foreach (var udpReceiver in UdpReceivers)
            {
                var ip = udpReceiver.Ip;
                var port = udpReceiver.Port;
                var hrri = new Hrri(udpReceiver.Hrri);
                try
                {
                    HrriEndPoint hrriEndPoint = new HrriEndPoint(new IPEndPoint(IPAddress.Parse(ip), port), hrri);
                    _localEndPoints.Enqueue(hrriEndPoint);
                }
                catch (Exception e)
                {
                    Logger.Warn("Could not add endpoint {0}:{1}, hrri: {2}, cause = {3}", ip, port, hrri, e.Message);
                }
            }
        }

        public void Stop()
        {
            _localUdpClient?.Close();
        }

        public class HrriEndPoint
        {
            public IPEndPoint IpEndPoint;
            public Hrri Hrri;

            public HrriEndPoint(IPEndPoint iPEndPoint, Hrri hrri)
            {
                this.IpEndPoint = iPEndPoint;
                this.Hrri = hrri;
            }
        }
    }
}