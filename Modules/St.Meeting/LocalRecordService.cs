using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using St.Common;
using System.IO;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Serilog;

namespace St.Meeting
{
    public class LocalRecordService : IRecord
    {
        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory,
            GlobalResources.ConfigPath);

        private readonly IMeeting _sdkService;

        public LocalRecordService()
        {
            _sdkService = IoC.Get<IMeeting>();
        }

        public string RecordDirectory { get; private set; }

        public int RecordId { get; set; }

        public RecordParameter RecordParam { get; private set; }

        public void ResetStatus()
        {
            RecordDirectory = string.Empty;
            RecordId = 0;
        }

        public bool GetRecordParam()
        {
            if (!File.Exists(ConfigFile))
            {
                return false;
            }

            try
            {
                RecordParameter recordParam = new RecordParameter()
                {
                    AudioBitrate = 64,
                    BitsPerSample = 16,
                    Channels = 1,
                    SampleRate = 8000,
                    VideoBitrate = int.Parse(GlobalData.Instance.AggregatedConfig.RecordConfig.CodeRate)
                };

                string[] resolutionStrings =
                    GlobalData.Instance.AggregatedConfig.RecordConfig.Resolution.Split(new[] {'*'},
                        StringSplitOptions.RemoveEmptyEntries);

                recordParam.Width = int.Parse(resolutionStrings[0]);
                recordParam.Height = int.Parse(resolutionStrings[1]);

                RecordParam = recordParam;
                RecordDirectory = GlobalData.Instance.AggregatedConfig.RecordConfig.RecordPath;

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get record param exception】：{ex}");
                return false;
            }
        }

        public AsyncCallbackMsg RefreshLiveStream(List<LiveVideoStream> openedStreamInfos)
        {
            if (RecordId != 0)
            {
                Log.Logger.Debug(
                    $"【local record live refresh begins】：liveId={RecordId}, videos={openedStreamInfos.Count}");
                for (int i = 0; i < openedStreamInfos.Count; i++)
                {
                    Log.Logger.Debug(
                        $"video{i + 1}：x={openedStreamInfos[i].X}, y={openedStreamInfos[i].Y}, width={openedStreamInfos[i].Width}, height={openedStreamInfos[i].Height}");
                }

                AsyncCallbackMsg updateAsynCallResult =
                    _sdkService.UpdateLiveVideoStreams(RecordId, openedStreamInfos.ToArray(),
                        openedStreamInfos.Count);
                Log.Logger.Debug(
                    $"【local record live refresh result】：result={updateAsynCallResult.Status}, msg={updateAsynCallResult.Message}");
                return updateAsynCallResult;
            }

            return AsyncCallbackMsg.GenerateMsg(Messages.WarningNoLiveToRefresh);
        }

        public async Task<AsyncCallbackMsg> StartRecord(List<LiveVideoStream> liveVideoStreamInfos)
        {
            if (string.IsNullOrEmpty(RecordDirectory))
            {
                return AsyncCallbackMsg.GenerateMsg(Messages.WarningRecordDirectoryNotSet);
            }


            if (RecordParam.Width == 0 || RecordParam.Height == 0 || RecordParam.VideoBitrate == 0)
            {
                return AsyncCallbackMsg.GenerateMsg(Messages.WarningRecordResolutionNotSet);
            }

            AsyncCallbackMsg result = await _sdkService.SetRecordDirectory(RecordDirectory);
            AsyncCallbackMsg setRecordParamResult = await _sdkService.SetRecordParameter(RecordParam);

            string recordFileName = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.mp4";

            Log.Logger.Debug(
                $"【local record live begins】：width={RecordParam.Width}, height={RecordParam.Height}, bitrate={RecordParam.VideoBitrate}, path={Path.Combine(RecordDirectory, recordFileName)}, videos={liveVideoStreamInfos.Count}");

            for (int i = 0; i < liveVideoStreamInfos.Count; i++)
            {
                Log.Logger.Debug(
                    $"video{i + 1}：x={liveVideoStreamInfos[i].X}, y={liveVideoStreamInfos[i].Y}, width={liveVideoStreamInfos[i].Width}, height={liveVideoStreamInfos[i].Height}");
            }

            AsyncCallbackMsg localRecordResult =
                await
                    _sdkService.StartRecord(recordFileName, liveVideoStreamInfos.ToArray(), liveVideoStreamInfos.Count);

            if (localRecordResult.Status == 0)
            {
                RecordId = int.Parse(localRecordResult.Data.ToString());

                Log.Logger.Debug($"【local record live succeeded】：liveId={RecordId}");
            }
            else
            {
                Log.Logger.Error($"【local record live failed】：{localRecordResult.Message}");
            }

            return localRecordResult;
        }

        public async Task<AsyncCallbackMsg> StopRecord()
        {
            if (RecordId != 0)
            {
                Log.Logger.Debug($"【local record live stop begins】：liveId={RecordId}");
                AsyncCallbackMsg stopAsynCallResult = await _sdkService.StopRecord();
                RecordId = 0;

                Log.Logger.Debug(
                    $"【local record live stop result】：result={stopAsynCallResult.Status}, msg={stopAsynCallResult.Message}");
                return stopAsynCallResult;
            }

            return AsyncCallbackMsg.GenerateMsg(Messages.WarningNoLiveToStop);
        }
    }
}
