using System;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Core
{
    public interface IThrottledRecorder
    {
        Task<UploadResult> RecordLatestAsync(ThrottledRecordRequest request);
    }

    public class ThrottledRecorder : IThrottledRecorder
    {
        public static SemaphoreSlim UpdateLock = new SemaphoreSlim(1);
        public static DateTimeOffset LastUpdate = DateTimeOffset.MinValue;

        private readonly ISystemTime _systemTime;
        private readonly IRecorder _innerRecorder;

        public ThrottledRecorder(ISystemTime systemTime, IRecorder innerRecorder)
        {
            _systemTime = systemTime;
            _innerRecorder = innerRecorder;
        }

        public async Task<UploadResult> RecordLatestAsync(ThrottledRecordRequest request)
        {
            // mutex
            var acquired = await UpdateLock.WaitAsync(0).ConfigureAwait(false);
            if (!acquired)
            {
                throw new ThrottlingException("An update is already running.");
            }

            try
            {
                // throttling
                if (_systemTime.UtcNow - LastUpdate < request.MaximumFrequency)
                {
                    throw new ThrottlingException("An update occurred too recently.");
                }

                var result = await _innerRecorder.RecordLatestAsync(request.Request).ConfigureAwait(false);
                LastUpdate = _systemTime.UtcNow;
                return result;
            }
            finally
            {
                UpdateLock.Release();
            }
        }
    }
}
