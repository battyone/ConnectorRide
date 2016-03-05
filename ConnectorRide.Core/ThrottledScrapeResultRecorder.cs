using System;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Core
{
    public interface IThrottledScrapeResultRecorder
    {
        Task<UploadResult> RecordLatestAsync(TimeSpan maximumFrequency, RecordRequest request);
    }

    public class ThrottledScrapeResultRecorder : IThrottledScrapeResultRecorder
    {
        public static SemaphoreSlim UpdateLock = new SemaphoreSlim(1);
        public static DateTimeOffset LastUpdate = DateTimeOffset.MinValue;

        private readonly ISystemTime _systemTime;
        private readonly IScrapeResultRecorder _innerRecorder;

        public ThrottledScrapeResultRecorder(ISystemTime systemTime, IScrapeResultRecorder innerRecorder)
        {
            _systemTime = systemTime;
            _innerRecorder = innerRecorder;
        }

        public async Task<UploadResult> RecordLatestAsync(TimeSpan maximumFrequency, RecordRequest request)
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
                if (_systemTime.UtcNow - LastUpdate < maximumFrequency)
                {
                    throw new ThrottlingException("An update occurred too recently.");
                }

                var result = await _innerRecorder.RecordAsync(request).ConfigureAwait(false);
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
