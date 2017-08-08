using System.Threading.Tasks;
using System.Collections.Generic;
using MeetingSdk.SdkWrapper.MeetingDataModel;

namespace St.Common
{
    public interface IViewLayout
    {
        List<ViewFrame> ViewFrameList { get; set; }

        event MeetingModeChanged MeetingModeChangedEvent;
        event ViewModeChanged ViewModeChangedEvent;

        MeetingMode MeetingMode { get; }
        ViewMode ViewMode { get; }
        ViewFrame FullScreenView { get; }

        List<LiveVideoStream> GetStreamLayout(int resolutionWidth, int resolutionHeight);

        Task LaunchLayout();
        void SetSpecialView(ViewFrame view, SpecialViewType type);
        void ChangeMeetingMode(MeetingMode targetMeetingMode);
        void ChangeViewMode(ViewMode targetViewMode);
        void ResetAsAutoLayout();
        void ResetAsInitialStatus();

        Task ShowViewAsync(ParticipantView view);
        Task HideViewAsync(ParticipantView view);
    }
}
