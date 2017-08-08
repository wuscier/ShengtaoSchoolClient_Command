using System.ComponentModel;

namespace St.Common
{
    public enum MeetingMode
    {
        [Description("互动")]
        Interaction,
        [Description("主讲")]
        Speaker,
        [Description("共享")]
        Sharing
    }

    public enum SpecialViewType
    {
        [Description("大画面")]
        Big,
        [Description("特写画面")]
        FullScreen
    }

    public enum ViewMode
    {
        [Description("默认")]
        Auto,
        [Description("平均")]
        Average,
        [Description("一大多小")]
        BigSmalls,
        [Description("特写")]
        Closeup
    }

    public delegate void MeetingModeChanged(MeetingMode meetingMode);
    public delegate void ViewModeChanged(ViewMode viewMode);
}
