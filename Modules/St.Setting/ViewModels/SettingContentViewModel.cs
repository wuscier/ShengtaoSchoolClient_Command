using Prism.Commands;
using St.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Windows.Forms;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Serilog;
using Action = System.Action;

namespace St.Setting
{
    public class SettingContentViewModel : ViewModelBase
    {
        public SettingContentViewModel(SettingContentView meetingConfigView)
        {
            _meetingConfigView = meetingConfigView;
            _sdkService = IoC.Get<IMeeting>();

            LoadSettingCommand = DelegateCommand.FromAsyncHandler(LoadSettingAsync);
            ConfigItemChangedCommand = DelegateCommand<ConfigChangedItem>.FromAsyncHandler(ConfigItemChangedAsync);

            SelectRecordPathCommand = new DelegateCommand(SelectRecordPath);
            LiveUrlChangedCommand = new DelegateCommand(LiveUrlChangedHander);

            InitializeBindingDataSource();
        }

        //private fields
        private readonly SettingContentView _meetingConfigView;
        private readonly IMeeting _sdkService;
        private static readonly string NonExclusiveItem = "空";

        //properties
        public List<string> CachedCameras { get; set; }
        public ObservableCollection<string> MainCameras { get; set; }
        public ObservableCollection<string> SecondaryCameras { get; set; }

        public List<string> CachedMicrophones { get; set; }
        public ObservableCollection<string> MainMicrophones { get; set; }
        public ObservableCollection<string> SecondaryMicrophones { get; set; }

        public List<string> CachedSpeakers { get; set; }
        public List<string> Speakers { get; set; }

        private MeetingSetting meetingConfigParameter;

        public MeetingSetting MeetingConfigParameter
        {
            get { return meetingConfigParameter; }
            set { SetProperty(ref meetingConfigParameter, value); }
        }

        private AggregatedConfig meetingConfigResult;

        public AggregatedConfig MeetingConfigResult
        {
            get { return meetingConfigResult; }
            set { SetProperty(ref meetingConfigResult, value); }
        }

        private ConfigItemTag configItemTag;

        public ConfigItemTag ConfigItemTag
        {
            get { return configItemTag; }
            set { SetProperty(ref configItemTag, value); }
        }

        private bool isMainCameraExpanded;

        public bool IsMainCameraExpanded
        {
            get { return isMainCameraExpanded; }
            set
            {
                ManageExpanderStatue(value);
                SetProperty(ref isMainCameraExpanded, value);
            }
        }

        private bool isSecondaryCameraExpanded;

        public bool IsSecondaryCameraExpanded
        {
            get { return isSecondaryCameraExpanded; }
            set
            {
                ManageExpanderStatue(value);
                SetProperty(ref isSecondaryCameraExpanded, value);
            }
        }

        private bool isAudioExpanded;

        public bool IsAudioExpanded
        {
            get { return isAudioExpanded; }
            set
            {
                ManageExpanderStatue(value);
                SetProperty(ref isAudioExpanded, value);
            }
        }

        private bool isLiveExpanded;

        public bool IsLiveExpanded
        {
            get { return isLiveExpanded; }
            set
            {
                ManageExpanderStatue(value);
                SetProperty(ref isLiveExpanded, value);
            }
        }
        private bool isServerLiveExpanded;

        public bool IsServerLiveExpanded
        {
            get { return isServerLiveExpanded; }
            set
            {
                ManageExpanderStatue(value);
                SetProperty(ref isServerLiveExpanded, value);
            }
        }

        private bool isRecordExpanded;

        public bool IsRecordExpanded
        {
            get { return isRecordExpanded; }
            set
            {
                ManageExpanderStatue(value);
                SetProperty(ref isRecordExpanded, value);
            }
        }

        private string liveUrlColor = "White";

        public string LiveUrlColor
        {
            get { return liveUrlColor; }
            set { SetProperty(ref liveUrlColor, value); }
        }

        //private string _pushUrlExplanation;
        //public string PushUrlExplanation
        //{
        //    get { return _pushUrlExplanation; }
        //    set { SetProperty(ref _pushUrlExplanation, value); }
        //}

        //commands
        public ICommand LoadSettingCommand { get; set; }
        public ICommand ConfigItemChangedCommand { get; set; }
        public ICommand SelectRecordPathCommand { get; set; }
        public ICommand LiveUrlChangedCommand { get; set; }

        //command handlers
        private async Task LoadSettingAsync()
        {
            try
            {
                await CacheDeviceListAsync();

                await GetParametersAsync();

                await SetupDeviceListAsync();

                await GetConfigFromGlobalConfigAsync();

                await SetDefaultConfigResultsAsync();

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【load setting page exception】：{ex}");
                string errorMsg = ex.InnerException?.Message.Replace("\r\n", string.Empty) ??
                                  ex.Message.Replace("\r\n", string.Empty);
                HasErrorMsg("-1", errorMsg);
            }
        }

        private async Task ConfigItemChangedAsync(ConfigChangedItem configChangedItem)
        {
            if (string.IsNullOrEmpty(configChangedItem.value))
            {
                return;
            }

            await Task.Run(async () =>
            {

                switch (configChangedItem.key)
                {
                    case ConfigItemKey.MainCamera:
                        await RefreshExclusiveDataSourceAsync(ConfigItemKey.MainCamera, configChangedItem.value);
                        _sdkService.SetDefaultDevice(1, configChangedItem.value);


                        await _meetingConfigView.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RefreshResolutionsAsync(configChangedItem);
                        }));
                        break;

                    case ConfigItemKey.MainCameraResolution:
                        string[] mainResolution = configChangedItem.value.Split('*');
                        _sdkService.SetVideoResolution(1, int.Parse(mainResolution[0]), int.Parse(mainResolution[1]));
                        break;

                    case ConfigItemKey.MainCameraCodeRate:
                        _sdkService.SetVideoBitRate(1, int.Parse(configChangedItem.value));

                        break;
                    case ConfigItemKey.SecondaryCamera:
                        await RefreshExclusiveDataSourceAsync(ConfigItemKey.SecondaryCamera, configChangedItem.value);
                        _sdkService.SetDefaultDevice(2, configChangedItem.value);

                        await _meetingConfigView.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RefreshResolutionsAsync(configChangedItem);
                        }));
                        break;
                    case ConfigItemKey.SecondaryCameraResolution:
                        string[] secondaryResolution = configChangedItem.value.Split('*');
                        _sdkService.SetVideoResolution(2, int.Parse(secondaryResolution[0]),
                            int.Parse(secondaryResolution[1]));
                        break;
                    case ConfigItemKey.SecondaryCameraCodeRate:
                        _sdkService.SetVideoBitRate(2, int.Parse(configChangedItem.value));
                        break;
                    case ConfigItemKey.MainMicrophone:
                        await RefreshExclusiveDataSourceAsync(ConfigItemKey.MainMicrophone, configChangedItem.value);
                        _sdkService.SetDefaultDevice(3, configChangedItem.value);

                        break;
                    case ConfigItemKey.SecondaryMicrophone:
                        await
                            RefreshExclusiveDataSourceAsync(ConfigItemKey.SecondaryMicrophone, configChangedItem.value);
                        _sdkService.SetDefaultDevice(5, configChangedItem.value);

                        break;
                    case ConfigItemKey.Speaker:
                        _sdkService.SetDefaultDevice(4, configChangedItem.value);
                        break;
                    case ConfigItemKey.AudioSampleRate:
                        _sdkService.SetAudioSampleRate(int.Parse(configChangedItem.value));
                        break;
                    case ConfigItemKey.AudioCodeRate:
                        _sdkService.SetAudioBitRate(int.Parse(configChangedItem.value));
                        break;
                    case ConfigItemKey.LiveResolution:

                        break;
                    case ConfigItemKey.LiveCodeRate:

                        break;
                    case ConfigItemKey.Unknown:
                    default:
                        break;
                }

                SaveConfig();
            });
        }

        private void RefreshResolutionsAsync(ConfigChangedItem configChangedItem)
        {
            if (configChangedItem.value == NonExclusiveItem)
            {
                return;
            }

            Camera videoDeviceInfo = _sdkService.GetCameraInfo(configChangedItem.value);


            if (configChangedItem.key == ConfigItemKey.MainCamera)
            {
                if (videoDeviceInfo.CameraParameters.Count > 0)
                {
                    MeetingConfigParameter.UserCameraSetting.ResolutionList.Clear();

                    CameraParameter cameraParameter = videoDeviceInfo.CameraParameters[0];

                    foreach (Size t in cameraParameter.VideoSizes)
                    {
                        string resolution = $"{t.Width}*{t.Height}";

                        if (!MeetingConfigParameter.UserCameraSetting.ResolutionList.Contains(resolution))
                        {
                            MeetingConfigParameter.UserCameraSetting.ResolutionList.Add(resolution);
                        }
                    }

                }

                if (
                    MeetingConfigParameter.UserCameraSetting.ResolutionList.Contains(
                        GlobalData.Instance.AggregatedConfig.MainCamera.Resolution))
                {
                    MeetingConfigResult.MainCamera.Resolution =
                        GlobalData.Instance.AggregatedConfig.MainCamera.Resolution;
                }
                else if (MeetingConfigParameter.UserCameraSetting.ResolutionList.Count > 0)
                {
                    MeetingConfigResult.MainCamera.Resolution =
                        MeetingConfigParameter.UserCameraSetting.ResolutionList[0];
                }
            }

            if (configChangedItem.key == ConfigItemKey.SecondaryCamera)
            {
                if (videoDeviceInfo.CameraParameters.Count > 0)
                {
                    MeetingConfigParameter.DataCameraSetting.ResolutionList.Clear();

                    CameraParameter cameraParameter = videoDeviceInfo.CameraParameters[0];

                    foreach (Size t in cameraParameter.VideoSizes)
                    {
                        string resolution = $"{t.Width}*{t.Height}";

                        if (!MeetingConfigParameter.DataCameraSetting.ResolutionList.Contains(resolution))
                        {
                            MeetingConfigParameter.DataCameraSetting.ResolutionList.Add(resolution);
                        }
                    }
                }

                if (
                    MeetingConfigParameter.DataCameraSetting.ResolutionList.Contains(
                        GlobalData.Instance.AggregatedConfig.SecondaryCamera.Resolution))
                {
                    MeetingConfigResult.SecondaryCamera.Resolution =
                        GlobalData.Instance.AggregatedConfig.SecondaryCamera.Resolution;
                }
                else if (MeetingConfigParameter.DataCameraSetting.ResolutionList.Count > 0)
                {
                    MeetingConfigResult.SecondaryCamera.Resolution =
                        MeetingConfigParameter.DataCameraSetting.ResolutionList[0];
                }
            }
        }

        private void SelectRecordPath()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog {SelectedPath = Environment.CurrentDirectory};
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                MeetingConfigResult.RecordConfig.RecordPath = fbd.SelectedPath;
                SaveConfig();
            }
        }

        private void LiveUrlChangedHander()
        {
            if (
                !string.IsNullOrEmpty(
                    MeetingConfigResult.LocalLiveConfig.PushLiveStreamUrl) &&
                Uri.IsWellFormedUriString(MeetingConfigResult.LocalLiveConfig.PushLiveStreamUrl,
                    UriKind.Absolute))
            {
                LiveUrlColor = "White";
                SaveConfig();
            }
            else
            {
                LiveUrlColor = "Red";
            }
        }


        //methods
        public void InitializeBindingDataSource()
        {
            CachedCameras = new List<string>();
            CachedMicrophones = new List<string>();
            CachedSpeakers = new List<string>();

            MainCameras = new ObservableCollection<string>();
            SecondaryCameras = new ObservableCollection<string>();

            MainMicrophones = new ObservableCollection<string>();
            SecondaryMicrophones = new ObservableCollection<string>();
            Speakers = new List<string>();
            MeetingConfigResult = new AggregatedConfig();
            MeetingConfigParameter = new MeetingSetting();

            ConfigItemTag = new ConfigItemTag();
        }

        private async Task CacheDeviceListAsync()
        {
            await Task.Run(() =>
            {
                var cameraList = _sdkService.GetDevices(1);
                CachedCameras.Clear();
                foreach (var camera in cameraList)
                {
                    if (!string.IsNullOrEmpty(camera.Name))
                    {
                        CachedCameras.Add(camera.Name);
                    }
                }
                CachedCameras.Add(NonExclusiveItem);

                var micList = _sdkService.GetDevices(3);
                CachedMicrophones.Clear();
                foreach (var mic in micList)
                {
                    if (!string.IsNullOrEmpty(mic.Name))
                    {
                        CachedMicrophones.Add(mic.Name);
                    }
                }
                CachedMicrophones.Add(NonExclusiveItem);

                var speakerList = _sdkService.GetDevices(4);
                CachedSpeakers.Clear();
                foreach (var speaer in speakerList)
                {
                    if (!string.IsNullOrEmpty(speaer.Name))
                    {
                        CachedSpeakers.Add(speaer.Name);
                    }
                }
            });
        }

        private async Task GetParametersAsync()
        {
            await Task.Run(async () =>
            {
                string configParameterPath = Path.Combine(Environment.CurrentDirectory, Common.GlobalResources.SettingPath);

                if (!File.Exists(configParameterPath))
                {
                    MeetingConfigParameter = GetDefaultConfigParameters();
                    string json = JsonConvert.SerializeObject(MeetingConfigParameter, Formatting.Indented);

                    File.WriteAllText(configParameterPath, json, Encoding.UTF8);
                }
                else
                {
                    string json = File.ReadAllText(configParameterPath, Encoding.UTF8);

                    MeetingSetting tempMeetingSetting = JsonConvert.DeserializeObject<MeetingSetting>(json);

                    await CloneParameters(tempMeetingSetting);
                }
            });
        }

        private async Task SetupDeviceListAsync()
        {
            await _meetingConfigView.Dispatcher.BeginInvoke(new Action(() =>
            {
                ClearDeviceList();
                CachedCameras.ForEach((camera) =>
                {
                    MainCameras.Add(camera);
                    SecondaryCameras.Add(camera);
                });

                CachedMicrophones.ForEach((microphone) =>
                {
                    MainMicrophones.Add(microphone);
                    SecondaryMicrophones.Add(microphone);
                });

                CachedSpeakers.ForEach((speaker) =>
                {
                    Speakers.Add(speaker);
                });
            }));
        }

        private async Task GetConfigFromGlobalConfigAsync()
        {
            MeetingConfigResult.CloneConfig(GlobalData.Instance.AggregatedConfig);

            await AutoSelectConfigAsync();
        }

        private MeetingSetting GetDefaultConfigParameters()
        {
            MeetingSetting defaultConfigParameter = new MeetingSetting();

            defaultConfigParameter.Audio.BitRateList.Add("64000");
            defaultConfigParameter.Audio.SampleRateList.Add("16000");

            defaultConfigParameter.UserCameraSetting.BitRateList.Add("500");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("600");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("800");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("1000");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("1200");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("1600");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("2000");
            defaultConfigParameter.UserCameraSetting.BitRateList.Add("3000");

            defaultConfigParameter.UserCameraSetting.ResolutionList.Add("640*360");
            defaultConfigParameter.UserCameraSetting.ResolutionList.Add("640*480");
            defaultConfigParameter.UserCameraSetting.ResolutionList.Add("1024*768");
            defaultConfigParameter.UserCameraSetting.ResolutionList.Add("1280*720");
            defaultConfigParameter.UserCameraSetting.ResolutionList.Add("1920*1080");


            defaultConfigParameter.DataCameraSetting.BitRateList.Add("500");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("600");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("800");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("1000");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("1200");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("1600");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("2000");
            defaultConfigParameter.DataCameraSetting.BitRateList.Add("3000");

            defaultConfigParameter.DataCameraSetting.ResolutionList.Add("640*360");
            defaultConfigParameter.DataCameraSetting.ResolutionList.Add("640*480");
            defaultConfigParameter.DataCameraSetting.ResolutionList.Add("1024*768");
            defaultConfigParameter.DataCameraSetting.ResolutionList.Add("1280*720");
            defaultConfigParameter.DataCameraSetting.ResolutionList.Add("1920*1080");

            defaultConfigParameter.Live.BitRateList.Add("500");
            defaultConfigParameter.Live.BitRateList.Add("600");
            defaultConfigParameter.Live.BitRateList.Add("700");
            defaultConfigParameter.Live.BitRateList.Add("900");
            defaultConfigParameter.Live.BitRateList.Add("1200");
            defaultConfigParameter.Live.BitRateList.Add("1500");
            defaultConfigParameter.Live.BitRateList.Add("1800");
            defaultConfigParameter.Live.BitRateList.Add("2000");
            defaultConfigParameter.Live.BitRateList.Add("2500");
            defaultConfigParameter.Live.BitRateList.Add("3000");

            defaultConfigParameter.Live.ResolutionList.Add("640*360");
            defaultConfigParameter.Live.ResolutionList.Add("640*480");
            defaultConfigParameter.Live.ResolutionList.Add("1024*768");
            defaultConfigParameter.Live.ResolutionList.Add("1280*720");
            defaultConfigParameter.Live.ResolutionList.Add("1920*1080");

            return defaultConfigParameter;
        }

        private async Task AutoSelectConfigAsync()
        {
            await _meetingConfigView.Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (string.IsNullOrEmpty(MeetingConfigResult.MainCamera.Resolution))
                {
                    MeetingConfigResult.MainCamera.Resolution =
                        MeetingConfigParameter.UserCameraSetting.ResolutionList.Count > 0
                            ? MeetingConfigParameter.UserCameraSetting.ResolutionList[0]
                            : null;
                }

                if (string.IsNullOrEmpty(MeetingConfigResult.MainCamera.CodeRate))
                {
                    MeetingConfigResult.MainCamera.CodeRate =
                        MeetingConfigParameter.UserCameraSetting.BitRateList.Count > 0
                            ? MeetingConfigParameter.UserCameraSetting.BitRateList[0]
                            : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.SecondaryCamera.Resolution))
                {
                    MeetingConfigResult.SecondaryCamera.Resolution =
                        MeetingConfigParameter.DataCameraSetting.ResolutionList.Count > 0
                            ? MeetingConfigParameter.DataCameraSetting.ResolutionList[0]
                            : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.SecondaryCamera.CodeRate))
                {
                    MeetingConfigResult.SecondaryCamera.CodeRate =
                        MeetingConfigParameter.DataCameraSetting.BitRateList.Count > 0
                            ? MeetingConfigParameter.DataCameraSetting.BitRateList[0]
                            : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.AudioConfig.Speaker))
                {
                    MeetingConfigResult.AudioConfig.Speaker = Speakers.Count > 0 ? Speakers[0] : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.AudioConfig.SampleRate))
                {
                    MeetingConfigResult.AudioConfig.SampleRate = MeetingConfigParameter.Audio.SampleRateList.Count > 0
                        ? MeetingConfigParameter.Audio.SampleRateList[0]
                        : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.AudioConfig.CodeRate))
                {
                    MeetingConfigResult.AudioConfig.CodeRate = MeetingConfigParameter.Audio.BitRateList.Count > 0
                        ? MeetingConfigParameter.Audio.BitRateList[0]
                        : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.LocalLiveConfig.Resolution))
                {
                    MeetingConfigResult.LocalLiveConfig.Resolution = MeetingConfigParameter.Live.ResolutionList.Count >
                                                                     0
                        ? MeetingConfigParameter.Live.ResolutionList[0]
                        : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.LocalLiveConfig.CodeRate))
                {
                    MeetingConfigResult.LocalLiveConfig.CodeRate = MeetingConfigParameter.Live.BitRateList.Count > 0
                        ? MeetingConfigParameter.Live.BitRateList[0]
                        : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.RemoteLiveConfig.Resolution))
                {
                    MeetingConfigResult.RemoteLiveConfig.Resolution = MeetingConfigParameter.Live.ResolutionList.Count >
                                                                      0
                        ? MeetingConfigParameter.Live.ResolutionList[0]
                        : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.RemoteLiveConfig.CodeRate))
                {
                    MeetingConfigResult.RemoteLiveConfig.CodeRate = MeetingConfigParameter.Live.BitRateList.Count > 0
                        ? MeetingConfigParameter.Live.BitRateList[0]
                        : null;

                }

                if (string.IsNullOrEmpty(MeetingConfigResult.RecordConfig.Resolution))
                {
                    MeetingConfigResult.RecordConfig.Resolution = MeetingConfigParameter.Live.ResolutionList.Count > 0
                        ? MeetingConfigParameter.Live.ResolutionList[0]
                        : null;

                }
                if (string.IsNullOrEmpty(MeetingConfigResult.RecordConfig.CodeRate))
                {
                    MeetingConfigResult.RecordConfig.CodeRate = MeetingConfigParameter.Live.BitRateList.Count > 0
                        ? MeetingConfigParameter.Live.BitRateList[0]
                        : null;

                }


                //exclusive1
                if (string.IsNullOrEmpty(MeetingConfigResult.MainCamera.Name) && MainCameras.Count > 0)
                {
                    MeetingConfigResult.MainCamera.Name = MainCameras[0];

                    RefreshResolutionsAsync(new ConfigChangedItem()
                    {
                        key = ConfigItemKey.MainCamera,
                        value = MainCameras[0]
                    });

                    if (MainCameras[0] != NonExclusiveItem)
                    {
                        SecondaryCameras.Remove(MainCameras[0]);
                    }
                }

                //exclusive1
                if (string.IsNullOrEmpty(MeetingConfigResult.SecondaryCamera.Name) && SecondaryCameras.Count > 0)
                {
                    MeetingConfigResult.SecondaryCamera.Name = SecondaryCameras[0];

                    RefreshResolutionsAsync(new ConfigChangedItem()
                    {
                        key = ConfigItemKey.SecondaryCamera,
                        value = SecondaryCameras[0]
                    });

                    if (SecondaryCameras[0] != NonExclusiveItem)
                    {
                        MainCameras.Remove(SecondaryCameras[0]);
                    }
                }

                //exclusive2
                if (string.IsNullOrEmpty(MeetingConfigResult.AudioConfig.MainMicrophone) && MainMicrophones.Count > 0)
                {
                    MeetingConfigResult.AudioConfig.MainMicrophone = MainMicrophones[0];

                    if (MainMicrophones[0] != NonExclusiveItem)
                    {
                        SecondaryMicrophones.Remove(MainMicrophones[0]);
                    }
                }

                //exclusive2
                if (string.IsNullOrEmpty(MeetingConfigResult.AudioConfig.SecondaryMicrophone) &&
                    SecondaryMicrophones.Count > 0)
                {
                    MeetingConfigResult.AudioConfig.SecondaryMicrophone = SecondaryMicrophones[0];

                    if (SecondaryMicrophones[0] != NonExclusiveItem)
                    {
                        MainMicrophones.Remove(SecondaryMicrophones[0]);
                    }
                }


                if (MainCameras.Contains(MeetingConfigResult.MainCamera.Name))
                {
                    await
                        RefreshExclusiveDataSourceAsync(ConfigItemKey.MainCamera, MeetingConfigResult.MainCamera.Name);

                    RefreshResolutionsAsync(new ConfigChangedItem()
                    {
                        key = ConfigItemKey.MainCamera,
                        value = MeetingConfigResult.MainCamera.Name
                    });
                }

                if (SecondaryCameras.Contains(MeetingConfigResult.SecondaryCamera.Name))
                {
                    await
                        RefreshExclusiveDataSourceAsync(ConfigItemKey.SecondaryCamera,
                            MeetingConfigResult.SecondaryCamera.Name);

                    RefreshResolutionsAsync(new ConfigChangedItem()
                    {
                        key = ConfigItemKey.SecondaryCamera,
                        value = MeetingConfigResult.SecondaryCamera.Name
                    });
                }

                if (MainMicrophones.Contains(MeetingConfigResult.AudioConfig.MainMicrophone))
                {
                    await
                        RefreshExclusiveDataSourceAsync(ConfigItemKey.MainMicrophone,
                            MeetingConfigResult.AudioConfig.MainMicrophone);
                }

                if (SecondaryMicrophones.Contains(MeetingConfigResult.AudioConfig.SecondaryMicrophone))
                {
                    await
                        RefreshExclusiveDataSourceAsync(ConfigItemKey.SecondaryMicrophone,
                            MeetingConfigResult.AudioConfig.SecondaryMicrophone);
                }

                if (string.IsNullOrEmpty(MeetingConfigResult.RecordConfig.RecordPath))
                {
                    MeetingConfigResult.RecordConfig.RecordPath =
                        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                }

                SaveConfig();
            }));
        }

        private async Task SetDefaultConfigResultsAsync()
        {
            await Task.Run(() =>
            {
                if (MeetingConfigResult.MainCamera.Name != NonExclusiveItem)
                {
                    _sdkService.SetDefaultDevice(1, MeetingConfigResult.MainCamera.Name);
                    string[] mainResolution = MeetingConfigResult.MainCamera.Resolution.Split('*');
                    _sdkService.SetVideoResolution(1, int.Parse(mainResolution[0]), int.Parse(mainResolution[1]));
                    _sdkService.SetVideoBitRate(1, int.Parse(MeetingConfigResult.MainCamera.CodeRate));
                }

                if (MeetingConfigResult.SecondaryCamera.Name != NonExclusiveItem)
                {
                    _sdkService.SetDefaultDevice(2, MeetingConfigResult.SecondaryCamera.Name);
                    string[] secondaryResolution = MeetingConfigResult.SecondaryCamera.Resolution.Split('*');
                    _sdkService.SetVideoResolution(2, int.Parse(secondaryResolution[0]),
                        int.Parse(secondaryResolution[1]));
                    _sdkService.SetVideoBitRate(2, int.Parse(MeetingConfigResult.SecondaryCamera.CodeRate));
                }

                if (MeetingConfigResult.AudioConfig.MainMicrophone != NonExclusiveItem)
                {
                    _sdkService.SetDefaultDevice(3, MeetingConfigResult.AudioConfig.MainMicrophone);
                }

                if (MeetingConfigResult.AudioConfig.SecondaryMicrophone != NonExclusiveItem)
                {
                    _sdkService.SetDefaultDevice(5, MeetingConfigResult.AudioConfig.SecondaryMicrophone);
                }

                _sdkService.SetDefaultDevice(4, MeetingConfigResult.AudioConfig.Speaker);

                _sdkService.SetAudioSampleRate(int.Parse(MeetingConfigResult.AudioConfig.SampleRate));
                _sdkService.SetAudioBitRate(int.Parse(MeetingConfigResult.AudioConfig.CodeRate));
            });
        }

        private void SaveConfig()
        {
            try
            {
                string configResultPath = Path.Combine(Environment.CurrentDirectory, Common.GlobalResources.ConfigPath);

                GlobalData.Instance.AggregatedConfig.CloneConfig(MeetingConfigResult);

                string json = JsonConvert.SerializeObject(MeetingConfigResult, Formatting.Indented);

                File.WriteAllText(configResultPath, json, Encoding.UTF8);
                SscUpdateManager.WriteConfigToVersionFolder(json);
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【save config exception in setting page】：{ex}");
            }
        }

        private async Task RefreshExclusiveDataSourceAsync(ConfigItemKey configItemKey, string exclusiveItem)
        {
            await _meetingConfigView.Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (configItemKey)
                {
                    case ConfigItemKey.MainCamera:

                        string tempSecondaryCamera = MeetingConfigResult.SecondaryCamera.Name;

                        SecondaryCameras.Clear();

                        CachedCameras.ForEach((camera) =>
                        {
                            if (camera != exclusiveItem)
                            {
                                SecondaryCameras.Add(camera);
                            }
                        });
                        if (!SecondaryCameras.Contains(NonExclusiveItem))
                        {
                            SecondaryCameras.Add(NonExclusiveItem);
                        }
                        MeetingConfigResult.SecondaryCamera.Name = tempSecondaryCamera;

                        break;
                    case ConfigItemKey.SecondaryCamera:

                        string tempMainCamera = MeetingConfigResult.MainCamera.Name;
                        MainCameras.Clear();
                        CachedCameras.ForEach((camera) =>
                        {
                            if (camera != exclusiveItem)
                            {
                                MainCameras.Add(camera);
                            }
                        });

                        if (!MainCameras.Contains(NonExclusiveItem))
                        {
                            MainCameras.Add(NonExclusiveItem);
                        }
                        MeetingConfigResult.MainCamera.Name = tempMainCamera;

                        break;
                    case ConfigItemKey.MainMicrophone:
                        string tempSecondaryMic = MeetingConfigResult.AudioConfig.SecondaryMicrophone;

                        SecondaryMicrophones.Clear();

                        CachedMicrophones.ForEach((mic) =>
                        {
                            if (mic != exclusiveItem)
                            {
                                SecondaryMicrophones.Add(mic);
                            }
                        });

                        if (!SecondaryMicrophones.Contains(NonExclusiveItem))
                        {
                            SecondaryMicrophones.Add(NonExclusiveItem);
                        }

                        MeetingConfigResult.AudioConfig.SecondaryMicrophone = tempSecondaryMic;
                        break;
                    case ConfigItemKey.SecondaryMicrophone:
                        string tempMainMic = MeetingConfigResult.AudioConfig.MainMicrophone;

                        MainMicrophones.Clear();
                        CachedMicrophones.ForEach((mic) =>
                        {
                            if (mic != exclusiveItem)
                            {
                                MainMicrophones.Add(mic);
                            }
                        });

                        if (!MainMicrophones.Contains(NonExclusiveItem))
                        {
                            MainMicrophones.Add(NonExclusiveItem);
                        }
                        MeetingConfigResult.AudioConfig.MainMicrophone = tempMainMic;
                        break;
                    case ConfigItemKey.Unknown:
                    default:
                        break;
                }

            }));
        }

        //only one expander can be expanded at one time
        private void ManageExpanderStatue(bool isExpanded)
        {
            if (isExpanded)
            {
                if (IsMainCameraExpanded | IsSecondaryCameraExpanded | IsAudioExpanded | IsLiveExpanded |
                    IsServerLiveExpanded | IsRecordExpanded)
                {
                    IsMainCameraExpanded = false;
                    IsSecondaryCameraExpanded = false;
                    IsAudioExpanded = false;
                    IsLiveExpanded = false;
                    IsServerLiveExpanded = false;
                    IsRecordExpanded = false;
                }
            }
        }

        private async Task CloneParameters(MeetingSetting newParameters)
        {
            await _meetingConfigView.Dispatcher.BeginInvoke(new Action(() =>
            {
                MeetingConfigParameter.Audio.Clear();
                MeetingConfigParameter.UserCameraSetting.ResolutionList.Clear();
                MeetingConfigParameter.UserCameraSetting.BitRateList.Clear();
                MeetingConfigParameter.DataCameraSetting.ResolutionList.Clear();
                MeetingConfigParameter.DataCameraSetting.BitRateList.Clear();
                MeetingConfigParameter.Live.ResolutionList.Clear();
                MeetingConfigParameter.Live.BitRateList.Clear();

                newParameters.Audio.BitRateList.ToList().ForEach(bitrate =>
                {
                    MeetingConfigParameter.Audio.BitRateList.Add(bitrate);
                });

                newParameters.Audio.SampleRateList.ToList().ForEach(samplerate =>
                {
                    MeetingConfigParameter.Audio.SampleRateList.Add(samplerate);
                });

                newParameters.UserCameraSetting.BitRateList.ToList().ForEach(bitrate =>
                {
                    MeetingConfigParameter.UserCameraSetting.BitRateList.Add(bitrate);
                });

                newParameters.UserCameraSetting.ResolutionList.ToList().ForEach(resolution =>
                {
                    MeetingConfigParameter.UserCameraSetting.ResolutionList.Add(resolution);
                });

                newParameters.DataCameraSetting.BitRateList.ToList().ForEach(bitrate =>
                {
                    MeetingConfigParameter.DataCameraSetting.BitRateList.Add(bitrate);
                });

                newParameters.DataCameraSetting.ResolutionList.ToList().ForEach(resolution =>
                {
                    MeetingConfigParameter.DataCameraSetting.ResolutionList.Add(resolution);
                });

                newParameters.Live.BitRateList.ToList().ForEach(bitrate =>
                {
                    MeetingConfigParameter.Live.BitRateList.Add(bitrate);
                });

                newParameters.Live.ResolutionList.ToList().ForEach(resolution =>
                {
                    MeetingConfigParameter.Live.ResolutionList.Add(resolution);
                });
            }));
        }

        private void ClearDeviceList()
        {
            MainCameras.Clear();
            SecondaryCameras.Clear();
            MainMicrophones.Clear();
            SecondaryMicrophones.Clear();
            Speakers.Clear();
        }
    }
}
