using System.Collections.Generic;
using System.Threading.Tasks;
using MeetingSdk.SdkWrapper.MeetingDataModel;

namespace St.Common
{
    public interface IPushLive
    {
        void ResetStatus();

        bool HasPushLiveSuccessfully { get; set; }

        int LiveId { get; }

        LiveVideoParameter LiveParam { get; }

        LiveVideoParameter GetLiveParam();

        Task<AsyncCallbackMsg> StartPushLiveStream(List<LiveVideoStream> liveVideoStreamInfos,
            string pushLiveUrl = "");

        AsyncCallbackMsg RefreshLiveStream(List<LiveVideoStream> liveVideoStreamInfos);
        Task<AsyncCallbackMsg> StopPushLiveStream();
    }
}
