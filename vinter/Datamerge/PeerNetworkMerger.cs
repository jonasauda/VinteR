using VinteR.Model;

namespace VinteR.Datamerge
{
    class PeerNetworkMerger : IDataMerger
    {
        public string MergerType => HardwareSystems.Peer;

        public MocapFrame HandleFrame(MocapFrame frame)
        {
            return frame;
        }
    }
}
