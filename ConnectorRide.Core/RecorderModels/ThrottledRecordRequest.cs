using System;

namespace Knapcode.ConnectorRide.Core.RecorderModels
{
    public class ThrottledRecordRequest
    {
        public RecordRequest Request { get; set; }
        public TimeSpan MaximumFrequency { get; set; }
    }
}