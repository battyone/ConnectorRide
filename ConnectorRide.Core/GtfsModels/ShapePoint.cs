using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public class ShapePoint
    {
        public string ShapeId { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public uint Sequence { get; set; }
    }
}
