using System;

namespace Knapcode.ConnectorRide.Core.Abstractions
{
    public interface ISystemTime
    {
        DateTimeOffset UtcNow { get; }
    }

    public class SystemTime : ISystemTime
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
