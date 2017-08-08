using System.Collections.Generic;
using System.Threading.Tasks;
using MeetingSdk.SdkWrapper.MeetingDataModel;

namespace St.Common
{
    public interface IRecord
    {
        int RecordId { get; }
        string RecordDirectory { get; }
        RecordParameter RecordParam { get; }

        void ResetStatus();

        bool GetRecordParam();

        Task<AsyncCallbackMsg> StartRecord(List<LiveVideoStream> liveVideoStreamInfos);
        Task<AsyncCallbackMsg> StopRecord();
        AsyncCallbackMsg RefreshLiveStream(List<LiveVideoStream> liveVideoStreamInfos);
    }
}
