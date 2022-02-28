using VinteR.Model;
using GenMocapFrame = VinteR.Model.Gen.MocapFrame;
using VinteR.Serialization;

namespace VinteR.Adapter.Network
{
    public class NetworkEventHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly NetworkAdapter _adapter;
        private readonly INetworkClient _client;
        private readonly Serializer _serializer;

        public NetworkEventHandler(NetworkAdapter adapter, INetworkClient client)
        {
            _adapter = adapter;
            _client = client;
            _serializer = new Serializer();
        }

        //Method to handle frame events
        public void ClientFrameReady(GenMocapFrame data)
        {
            _serializer.FromProtoBuf(data, out MocapFrame frame);
            _adapter.OnFrameAvailable(frame);
        }
    }
}