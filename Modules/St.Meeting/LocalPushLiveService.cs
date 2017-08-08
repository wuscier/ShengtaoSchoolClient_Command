using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Serilog;
using St.Common;

namespace St.Meeting
{
    public class LocalPushLiveService : IPushLive
    {
        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory,
            GlobalResources.ConfigPath);

        private readonly IMeeting _sdkService;

        public LocalPushLiveService()
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
            // get live configuration from local xml file
            if (!File.Exists(ConfigFile))
            {
                LiveParam = new LiveVideoParameter();
                return new LiveVideoParameter();
            }
            try
            {
                LiveVideoParameter liveParam = new LiveVideoParameter
                {
                    AudioBitrate = 64,
                    BitsPerSample = 16,
                    Channels = 1,
                    IsLive = true,
                    IsRecord = false,
                    SampleRate = 8000,
                    RecordFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),

                    Url1 = GlobalData.Instance.AggregatedConfig.LocalLiveConfig.PushLiveStreamUrl,
                    VideoBitrate = int.Parse(GlobalData.Instance.AggregatedConfig.LocalLiveConfig.CodeRate)
                };

                string[] resolutionStrings =
                    GlobalData.Instance.AggregatedConfig.LocalLiveConfig.Resolution.Split(new[] {'*'},
                        StringSplitOptions.RemoveEmptyEntries);

                liveParam.Width = int.Parse(resolutionStrings[0]);
                liveParam.Height = int.Parse(resolutionStrings[1]);


                LiveParam = liveParam;
                return liveParam;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get local push live param exception】：{ex}");
                LiveParam = new LiveVideoParameter();
                return new LiveVideoParameter();
            }
        }

        public AsyncCallbackMsg RefreshLiveStream(List<LiveVideoStream> openedStreamInfos)
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【local push live refresh begins】：liveId={LiveId}, videos={openedStreamInfos.Count}");
                for (int i = 0; i < openedStreamInfos.Count; i++)
                {
                    Log.Logger.Debug(
                        $"video{i + 1}：x={openedStreamInfos[i].X}, y={openedStreamInfos[i].Y}, width={openedStreamInfos[i].Width}, height={openedStreamInfos[i].Height}");
                }

                AsyncCallbackMsg updateAsynCallResult = _sdkService.UpdateLiveVideoStreams(LiveId,
                    openedStreamInfos.ToArray(), openedStreamInfos.Count);
                Log.Logger.Debug(
                    $"【local push live refresh result】：result={updateAsynCallResult.Status}, msg={updateAsynCallResult.Message}");
                return updateAsynCallResult;
            }

            return AsyncCallbackMsg.GenerateMsg(Messages.WarningNoLiveToRefresh);
        }

        public async Task<AsyncCallbackMsg> StartPushLiveStream(List<LiveVideoStream> liveVideoStreamInfos,
            string pushLiveUrl)
        {
            if (string.IsNullOrEmpty(LiveParam.Url1))
            {
                return AsyncCallbackMsg.GenerateMsg(Messages.WarningLivePushLiveUrlNotSet);
            }

            if (LiveParam.Width == 0 || LiveParam.Height == 0 || LiveParam.VideoBitrate == 0)
            {
                return AsyncCallbackMsg.GenerateMsg(Messages.WarningLiveResolutionNotSet);
            }

            Log.Logger.Debug(
                $"【local push live begins】：width={LiveParam.Width}, height={LiveParam.Height}, bitrate={LiveParam.VideoBitrate}, url={LiveParam.Url1}, videos={liveVideoStreamInfos.Count}");

            for (int i = 0; i < liveVideoStreamInfos.Count; i++)
            {
                Log.Logger.Debug(
                    $"video{i + 1}：x={liveVideoStreamInfos[i].X}, y={liveVideoStreamInfos[i].Y}, width={liveVideoStreamInfos[i].Width}, height={liveVideoStreamInfos[i].Height}");
            }


            AsyncCallbackMsg startLiveStreamResult =
                await _sdkService.StartLiveStream(LiveParam, liveVideoStreamInfos.ToArray(), liveVideoStreamInfos.Count);


            if (startLiveStreamResult.Status == 0)
            {
                LiveId = int.Parse(startLiveStreamResult.Data.ToString());

                HasPushLiveSuccessfully = true;
                Log.Logger.Debug($"【local push live succeeded】：liveId={LiveId}");
            }
            else
            {
                Log.Logger.Error($"【local push live failed】：{startLiveStreamResult.Message}");
            }

            return startLiveStreamResult;
        }

        public async Task<AsyncCallbackMsg> StopPushLiveStream()
        {
            if (LiveId != 0)
            {
                Log.Logger.Debug($"【local push live stop begins】：liveId={LiveId}");
                AsyncCallbackMsg stopAsynCallResult = await _sdkService.StopLiveStream(LiveId);
                LiveId = 0;

                Log.Logger.Debug(
                    $"【local push live stop result】：result={stopAsynCallResult}, msg={stopAsynCallResult.Message}");
                return stopAsynCallResult;
            }

            return AsyncCallbackMsg.GenerateMsg(Messages.WarningNoLiveToStop);
        }
    }
}
