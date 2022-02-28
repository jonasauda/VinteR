using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VinteR.Model;

namespace VinteR.Datamerge
{
    class NetworkMerger : IDataMerger
    {
        public string MergerType => HardwareSystems.HoloRoom;

        public MocapFrame HandleFrame(MocapFrame frame)
        {
            return frame;
        }
    }
}
