using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using VinteR.Configuration;
using System.IO;

namespace VinteR.ConnectionBroker
{
    public delegate void ConnectionEstablishedHandler(Tuple<IPEndPoint, IPEndPoint> tx, Tuple<IPEndPoint, IPEndPoint> rx);

    public interface IConnectionBroker
    {
        event ConnectionEstablishedHandler OnPeerConnectionEstablished;
        void Start();
    }

    class ConnectionBroker : IConnectionBroker
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public event ConnectionEstablishedHandler OnPeerConnectionEstablished;

        public string AdapterType => HardwareSystems.HoloRoom;

        private static ASCIIEncoding encoding = new ASCIIEncoding();

        // TODO: get server endpoint, client port from config
        private string _brokerHostName;
        private int _brokerPort;
        private int _clientTxPort;

        public ConnectionBroker(IConfigurationService configurationService)
        {
            var config = configurationService.GetConfiguration().Adapters
                .Where(a => a.AdapterType.Equals(HardwareSystems.Peer))
                .DefaultIfEmpty(null)
                .FirstOrDefault();

            _clientTxPort = config.ClientTxPort;
            _brokerHostName = config.BrokerHostName;
            _brokerPort = config.BrokerPort;
        }

        public async void Start()
        {
            string externalIp = await GetPublicIp();
            if (externalIp == null)
            {
                return;
            }

            Tuple<IPEndPoint, IPEndPoint> peerEndPoints = await GetPeerEndPointFromBroker(externalIp);
            if (peerEndPoints == null) {
                return;
            }

            IPEndPoint clientTxEndPoint = new IPEndPoint(IPAddress.Any, _clientTxPort);
            IPEndPoint clientRxEndPoint = new IPEndPoint(IPAddress.Any, _clientTxPort + 1);
            await Task.WhenAll(
                EstablishConnection(clientTxEndPoint, peerEndPoints.Item2),
                EstablishConnection(clientRxEndPoint, peerEndPoints.Item1)
            );

            OnPeerConnectionEstablished(
                new Tuple<IPEndPoint, IPEndPoint>(clientTxEndPoint, peerEndPoints.Item2),
                new Tuple<IPEndPoint, IPEndPoint>(clientRxEndPoint, peerEndPoints.Item1)
            );
        }

        private async Task<string> GetPublicIp()
        {

            var result = await Task.Run(() =>
            {
                try
                {

                string externalIp = new WebClient().DownloadString("https://ident.me");
                Logger.Debug("externalIp: {0}", externalIp);
                return externalIp;
                }
                catch(WebException e)
                {
                    Logger.Error("Could not retrieve external IP.");
                }
                return null;
            });
            return result;
        }

        private async Task<Tuple<IPEndPoint, IPEndPoint>> GetPeerEndPointFromBroker(string externalIp)
        {

            return await Task.Run(() =>
            {
                IPEndPoint peerTxEndPoint = null;

                byte[] messageBytes = EncodeAddress(externalIp, _clientTxPort);

                IPAddress brokerIp = GetEndpointByHostName(_brokerHostName);
                IPEndPoint brokerEndPoint = new IPEndPoint(brokerIp, _brokerPort);

                IPEndPoint clientEndPointBroker = new IPEndPoint(IPAddress.Any, _clientTxPort);
                TcpClient brokerClient = new TcpClient();

                try
                {
                    brokerClient.Connect(brokerEndPoint);
                    NetworkStream brokerStream = brokerClient.GetStream();

                    byte[] dataBuffer = new byte[2048];

                    while (peerTxEndPoint == null)
                    {
                        try
                        {
                            brokerStream.Write(messageBytes, 0, messageBytes.Length);
                            brokerStream.Read(dataBuffer, 0, dataBuffer.Length);
                            var addressTuple = DecodeAddress(dataBuffer);
                            peerTxEndPoint = new IPEndPoint(IPAddress.Parse(addressTuple.Item1), addressTuple.Item2);
                        }
                        catch (SocketException)
                        {
                        }
                        catch (IOException e)
                        {
                            Logger.Warn("Could not write to TCP connection with broker.");
                        }
                        catch (Exception e)
                        {
                            Logger.Warn(e);
                        }
                    }
                    Logger.Info("Received peer address: {0}", peerTxEndPoint.ToString());

                    IPEndPoint peerRxEndPoint = new IPEndPoint(peerTxEndPoint.Address, peerTxEndPoint.Port + 1);

                    return new Tuple<IPEndPoint, IPEndPoint>(peerTxEndPoint, peerRxEndPoint);

                }
                catch (SocketException)
                {
                    Logger.Error("Could not connect to broker server.");
                    return null;
                }
                finally
                {
                    brokerClient.Close();
                }
            });
        }

        private enum ConnectionState { Initial, SYN, ACK }

        private static async Task EstablishConnection(IPEndPoint clientEndPoint, IPEndPoint peerEndPoint)
        {
            await Task.Run(() =>
            {
                UdpClient udpClient = new UdpClient(clientEndPoint);
                udpClient.Client.ReceiveTimeout = 500;

                ConnectionState state = ConnectionState.Initial;

                string synMessageString = "SYN";
                string ackMessageString = "ACK";
                byte[] synMessageBytes = encoding.GetBytes(synMessageString);
                byte[] ackMessageBytes = encoding.GetBytes(ackMessageString);

                int ackCounter = 0;

                for (var i = 0; i < 50; i++)
                {
                    try
                    {
                        byte[] outMessageBytes = new byte[0];

                        switch (state)
                        {
                            case ConnectionState.Initial:
                                outMessageBytes = synMessageBytes;
                                break;
                            case ConnectionState.SYN:
                                outMessageBytes = ackMessageBytes;
                                break;
                            case ConnectionState.ACK:
                                outMessageBytes = ackMessageBytes;
                                if (ackCounter >= 5)
                                {
                                    Logger.Info("Done sending ACKs");
                                    udpClient.Close();
                                    return;
                                }
                                ackCounter++;
                                break;
                        }

                        udpClient.Send(outMessageBytes, outMessageBytes.Length, peerEndPoint);
                        Logger.Debug("Sent {0} to {1} from {2}", encoding.GetString(outMessageBytes), peerEndPoint, clientEndPoint);

                        byte[] inMessageBytes = udpClient.Receive(ref peerEndPoint);
                        var inMessage = encoding.GetString(inMessageBytes);
                        Logger.Debug("Received {0} from {1} on {2}", inMessage, peerEndPoint, clientEndPoint);

                        if (inMessage == synMessageString)
                        {
                            state = ConnectionState.SYN;
                            Logger.Info("Received SYN");
                        }
                        else if (inMessage == ackMessageString)
                        {
                            state = ConnectionState.ACK;
                            Logger.Info("Received ACK");
                        }
                    }
                    catch (SocketException e)
                    {
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e);
                    }
                }
                udpClient.Close();

                Logger.Warn("Could not establish connection from endpoint {0} to endpoint {1}", clientEndPoint, peerEndPoint);
            });
        }

        private static IPAddress GetEndpointByHostName(string hostname)
        {
            IPAddress[] ips = Dns.GetHostAddresses(hostname);
            if (ips.Length == 0)
            {
                throw new ArgumentException("Could not resolve IP address for host " + hostname);
            }
            return ips[0];
        }

        private static byte[] EncodeAddress(string ip, int port)
        {
            string messageString = string.Format("{0}:{1}", ip, port);
            byte[] messageBytes = encoding.GetBytes(messageString);
            return messageBytes;
        }

        private static Tuple<string, int> DecodeAddress(byte[] message)
        {
            string messageString = encoding.GetString(message);
            string[] segments = messageString.Split(':');
            return new Tuple<string, int>(segments[0], int.Parse(segments[1]));
        }

        public static byte[] CombineByteArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
