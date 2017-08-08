using System;
using System.IO;
using System.Threading.Tasks;
using St.Common;
using System.Collections.Generic;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Serilog;

namespace St.Meeting
{
    public class ServerPushLiveService : IPushLive
    {
        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory,
            GlobalResources.ConfigPath);

        private readonly IMeeting _sdkService;

        public ServerPushLiveService()
        {
            _sdkService = IoC.Get<IMeeting>();
        }

        public bool HasPushLiveSuccessfully { get; set; }

        public int LiveId { get; set; }

        public LiveVideoParameter LiveParam { get; private set; }

        public void ResetStatus()
        {
            LiveId = 0;
            HasPushLiveSuccessfully = false;
        }

        public LiveVideoParameter GetLiveParam()
        {
            if (!File.Exists(ConfigFile))
            {
                LiveParam = new LiveVideoParameter();
                return new LiveVideoParameter();
            }

            try
            {
                LiveVideoParameter liveParam = new LiveVideoParameter()
                {
                    AudioBitrate = 64,
                    BitsPerSample = 16,
                    Channels = 1,
                    IsLive = true,
                    IsRecord = false,
                    SampleRate = 8000,
                    RecordFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),

                    VideoBitrate = int.Parse(GlobalData.Instance.AggregatedConfig.RemoteLiveConfig.CodeRate)
                };

                string[] resolutionStrings =
                    GlobalData.Instance.AggregatedConfig.RemoteLiveConfig.Resolution.Split(new[] {'*'},
                        StringSplitOptions.RemoveEmptyEntries);

                liveParam.Width = int.Parse(resolutionStrings[0]);
                liveParam.Height = int.Parse(resolutionStrings[1]);

                LiveParam = liveParam;
                return liveParam;

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get server push live param exception】：{ex}");
                LiveParam = new LiveVideoParameter();
                return new LiveVideoParameter();
            }
        }

        public async Task<AsyncCallbackMsg> StartPushLiveStream(List<LiveVideoStream> liveVideoStreamInfos,
            string pushLiveUrl)
        {
            if (string.IsNullOrEmpty(pushLiveUrl))
            {
                return AsyncCallbackMsg.GenerateMsg(Messages.WarningLivePushLiveUrlNotSet);
            }

            LiveVideoParameter liveParam = LiveParam;
            liveParam.Url1 = pushLiveUrl;

            if (liveParam.Width == 0 || liveParam.Height == 0 || liveParam.VideoBitrate == 0)
            {
                return AsyncCallbackMsg.GenerateMsg(Messages.WarningLiveResolutionNotSet);
            }

            Log.Logger.Debug(
                $"【server push live begins】：width={liveParam.Width}, height={liveParam.Height}, bitrate={liveParam.VideoBitrate}, url={liveParam.Url1}, videos={liveVideoStreamInfos.Count}");

            for (int i = 0; i < liveVideoStreamInfos.Count; i++)
            {
                Log.Logger.Debug(
                    $"video{i + 1}：x={liveVideoStreamInfos[i].X}, y={liveVideoStreamInfos[i].Y}, width={liveVideoStreamInfos[i].Width}, height={liveVideoStreamInfos[i].Height}");
            }

            AsyncCallbackMsg startLiveStreamResult =
                await _sdkService.StartLiveStream(liveParam, liveVideoStreamInfos.ToArray(), liveVideoStreamInfos.Count);

            if (startLiveStreamResult.Status == 0)
            {
                LiveId = int.Parse(startLiveStreamResult.Data.ToString());

                HasPushLiveSuccessfully = true;
                Log.Logger.Debug($"【server push live succeeded】：liveId={LiveId}");
            }
            else
            {
                HasPushLiveSuccessfully = false;
                Log.Logger.Error($"【server push live failed】：{startLiveStreamResult.Message}");
            }

            return startLiveStreamResult;
        }

        public AsyncCallbackMsg RefreshLiveStream(List<LiveVideoStream> openedStreamInfos)
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【server refresh live begins】：liveId={LiveId}, videos={openedStreamInfos.Count}");
                for (int i = 0; i < openedStreamInfos.Count; i++)
                {
                    Log.Logger.Debug(
                        $"video{i + 1}：x={openedStreamInfos[i].X}, y={openedStreamInfos[i].Y}, width={openedStreamInfos[i].Width}, height={openedStreamInfos[i].Height}");
                }

                AsyncCallbackMsg updateAsynCallResult = _sdkService.UpdateLiveVideoStreams(LiveId,
                    openedStreamInfos.ToArray(), openedStreamInfos.Count);
                Log.Logger.Debug(
                    $"【server refresh live result】：result={updateAsynCallResult.Status}, msg={updateAsynCallResult.Message}");
                return updateAsynCallResult;
            }
            return AsyncCallbackMsg.GenerateMsg(Messages.WarningNoLiveToRefresh);
        }

        public async Task<AsyncCallbackMsg> StopPushLiveStream()
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【server push live stop begins】：liveId={LiveId}");
                AsyncCallbackMsg stopAsynCallResult = await _sdkService.StopLiveStream(LiveId);
                LiveId = 0;

                Log.Logger.Debug(
                    $"【server push live stop result】：result={stopAsynCallResult.Status}, msg={stopAsynCallResult.Message}");

                return stopAsynCallResult;
            }

            return AsyncCallbackMsg.GenerateMsg(Messages.WarningNoLiveToStop);
        }
    }
}
