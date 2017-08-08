namespace St.Common
{
    public static class Messages
    {
        public const string WarningUserCenterIpNotConfigured = "服务器地址未配置！";
        public const string WarningUserNameEmpty = "用户名不能为空！";
        public const string WarningPasswordEmpty = "密码不能为空！";
        public const string WarningAuthenticationFailure = "登录失败！";
        public const string WarningDeviceNotRegistered = "设备未注册！";
        public const string InfoLogging = "正在登录中，请稍后......";
        public const string ErrorLoginFailed = "登录失败！";
        public const string WarningLoginTimeout = "登录超时！";
        public const string WarningRefreshTokenFailed = "重新自动获取令牌失败！";
        public const string WarningDeviceExpires = "设备已过期！";
        public const string WarningEmptyDevice = "获取设备信息失败！";
        public const string WarningLockedDevice = "设备已经被锁定！";
        public const string WarningYouAreSignedOut = "请重新登录。可能的原因：\r\n1.您的账号已在别的设备上登录。\r\n2.登录已过期。";

        public const string WarningMeetingServerAlreadyStarted = "服务已经是启动状态！";
        public const string WarningMeetingServerNotStarted = "服务未启动！";

        public const string WarningBigSmallLayoutNeedsTwoAboveViews = "一大多小布局至少需要两个视图！";
        public const string WarningNoSpeaderView = "没有主讲视图，无法设置主讲模式！";
        public const string WarningNoSharingView = "没有共享视图，无法设置共享模式！";
        public const string WarningYouAreNotSpeaking = "你不在发言状态！";
        public const string WarningUserNotSpeaking = "指定参会方不在发言状态！";

        public const string WarningInvalidMeetingNo = "无效的会议号！";
        public const string WarningMeetingNoDoesNotExist = "会议号不存在！";
        public const string WarningMeetingHasBeenEnded = "该课程由于长时间未进入已被清理！";

        public const string WarningGetMeetingListFailed = "加载可参会列表失败！";

        public const string WarningNoMicrophone = "麦克风未设置！";
        public const string WarningNoSpeaker = "扬声器未设置！";
        public const string WarningNoCamera = "视频采集未设置！";

        public const string WarningSetFillModeFailed = "设置视频窗口绘制填充模式失败！";

        public const string ErrorGetRecordConfigFailed = "读取本地录制配置失败！";
        public const string ErrorGetRemoteLiveConfigFailed = "读取服务器推流配置失败！";
        public const string ErrorGetLocalLiveConfigFailed = "读取本地推流配置失败！";
        public const string ErrorGetConfigFailed = "读取直播和录制配置信息失败！";
        public const string ErrorSetWorkingDirectoryFailed = "设置SDK的工作路径失败！";

        public const string WarningRecordDirectoryNotSet = "录制路径未设置！";
        public const string WarningRecordResolutionNotSet = "录制分辨率或码率未设置！";
        public const string WarningRecordParamWrongFormat = "录制参数格式设置有误！";

        public const string WarningLivePushLiveUrlNotSet = "推流地址未设置！";
        public const string WarningLiveResolutionNotSet = "推流分辨率或码率未设置！";
        public const string WarningLiveParamWrongFormat = "推流分辨率或码率格式设置有误！";

        public const string WarningYouNeedCreateAMeeting = "正在创建课堂，请稍后......";
        public const string WarningSomeoneIsCreatingAMeeting = "等待主讲人创建课堂，请稍后......";

        public const string InfoStartingMeetingSdk = "服务正在启动中，请稍后......";
        public const string InfoMeetingSdkStarted = "服务启动成功。";
        public const string InfoNubeNotRegistered = "尚未开通视讯号！";
        public const string InfoMeetingSdkStartedFailed = "服务启动失败！";
        public const string InfoMeetingSdkNotStarted = "服务未启动！";


        public const string ErrorCopyConfigFile = "复制配置文件时出错！";

        public const string ErrorConfigFileLost = "配置文件丢失！";
        public const string ErrorReadConfigFailed = "无法正确读取配置文件！";
        public const string ErrorReadEmptyConfig = "无法读取空配置文件！";
        public const string ErrorWriteEmptyConfig = "无法写入空配置文件！";
        public const string ErrorWriteConfigFailed = "无法正确写入配置文件！";

        public const string WarningBmsAddressEmpty = "后台地址不能为空！";
        public const string WarningNpsAddressEmpty = "NPS地址不能为空！";
        public const string WarningRtsAddressEmpty = "Rts地址不能为空！";
        public const string WarningUpdateAddressEmpty = "升级地址不能为空！";

        public const string ErrorReadNpsConfigError = "读取NPS地址时出错！";
        public const string ErrorWriteNpsConfigError = "写入NPS地址时出错！";
        public const string InfoSaveConfigSucceeded = "保存配置信息成功！";

        public const string WarningPushUrlExplanation = "如果该设备没有推流地址，此推流地址将被启用！";
        public const string WarningNullDataFromServer = "服务器返回空数据！";

        public const string ErrorClearUserDataFailed = "清理SDK缓存数据时出错！";
        public const string WarningNoLiveToStop = "没有可停止的流！";
        public const string WarningNoLiveToRefresh = "没有可更新的流！";

        public const string InfoGotoListenerMode = "当前切换为听课模式，听讲教室处于禁言状态！";
        public const string InfoGotoDiscussionMode = "当前切换为评课模式，点击申请发言参与评课！";

        public const string DocumentAlreadyOpened = "当前课件已经开启,请勿重复操作";
        public const string DocumentAlreadyClosed = "当前课件已经关闭,请勿重复操作";
    }
}
