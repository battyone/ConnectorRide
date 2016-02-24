using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.ConnectorRide.Core.ClientModels
{
    public class Table
    {
        public TableTrip[] Trips { get; set; }
        public TableStop[] Stops { get; set; }
    }
}
