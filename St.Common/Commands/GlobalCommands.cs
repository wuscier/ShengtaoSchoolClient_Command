using Caliburn.Micro;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using Squirrel.Json;

namespace St.Common.Commands
{
    public class GlobalCommands
    {

        public static readonly GlobalCommands Instance = new GlobalCommands();

        private GlobalCommands()
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            InitializeCommands();
        }

        private readonly IEventAggregator _eventAggregator;

        private void InitializeCommands()
        {
            GotoInteractiveCommand =
                new SscCommand(
                    () => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(GotoInteractiveCommand); },
                    true,
                    () => GotoInteractiveCommand.Enabled,
                    "远程互动授课",
                    66);

            GotoDiscussionCommand =
                new SscCommand(
                    () => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(GotoDiscussionCommand); },
                    true,
                    () => GotoDiscussionCommand.Enabled,
                    "远程听评课",
                    67);

            GotoInteractiveWithoutLiveCommand =
                new SscCommand(
                    () =>
                    {
                        _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(GotoInteractiveWithoutLiveCommand);
                    },
                    true,
                    () => GotoInteractiveWithoutLiveCommand.Enabled,
                    "远程教研",
                    68);

            GotoClassCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(GotoClassCommand); },
                    false,
                    () => GotoClassCommand.Enabled,
                    "进入课堂",
                    69);

            ExitClassCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(ExitClassCommand); },
                    false,
                    () => ExitClassCommand.Enabled,
                    "退出课堂",
                    70, true);

            SpeakCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(SpeakCommand); },
                    false,
                    () => SpeakCommand.Enabled,
                    "申请/停止发言",
                    71, true);

            DocCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(DocCommand); },
                    false,
                    () => DocCommand.Enabled,
                    "打开/关闭课件",
                    73, true);

            RecordCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(RecordCommand); },
                    false,
                    () => RecordCommand.Enabled,
                    "开始/停止录制",
                    74, true);

            PushLiveCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(PushLiveCommand); },
                    false,
                    () => PushLiveCommand.Enabled,
                    "开始/停止推流",
                    75, true);

            AverageCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(AverageCommand); },
                    false,
                    () => AverageCommand.Enabled,
                    "平均排列",
                    77, true);

            BigSmallsCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(BigSmallsCommand); },
                    false,
                    () => BigSmallsCommand.Enabled,
                    "一大多小",
                    78, true);
            CloseupCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(CloseupCommand); },
                    false,
                    () => CloseupCommand.Enabled,
                    "特写",
                    79, true);

            InteractionCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(InteractionCommand); },
                    false,
                    () => InteractionCommand.Enabled,
                    "互动模式",
                    80, true);

            SpeakerCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(SpeakerCommand); },
                    false,
                    () => SpeakerCommand.Enabled,
                    "主讲模式",
                    81, true);

            ShareCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(ShareCommand); },
                    false,
                    () => ShareCommand.Enabled,
                    "共享模式",
                    82, true);

            UpCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(UpCommand); },
                    true,
                    () => UpCommand.Enabled,
                    "上",
                    38);

            DownCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(DownCommand); },
                    true,
                    () => DownCommand.Enabled,
                    "下",
                    40);

            LeftCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(LeftCommand); },
                    true,
                    () => LeftCommand.Enabled,
                    "左",
                    37);

            RightCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(RightCommand); },
                    true,
                    () => RightCommand.Enabled,
                    "右",
                    39);

            ConfirmCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(ConfirmCommand); },
                    true,
                    () => ConfirmCommand.Enabled,
                    "确认",
                    13);

            CancelCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(CancelCommand); },
                    true,
                    () => CancelCommand.Enabled,
                    "取消",
                    27);
            ForwardCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(ForwardCommand); },
                    true,
                    () => ForwardCommand.Enabled,
                    "向后导航",
                    9);

            BackwardCommand =
                new SscCommand(() => { _eventAggregator.GetEvent<CommandReceivedEvent>().Publish(BackwardCommand); },
                    true,
                    () => BackwardCommand.Enabled,
                    "向前导航",
                    255);

            ActivateCommand = new SscCommand(() => { }, true, () => false, "激活", 90);

        }

        public string GetCurrentCommands()
        {
            Type type = typeof(GlobalCommands);

            PropertyInfo[] propertyInfos = type.GetProperties();

            List<SscCommand> commandList = new List<SscCommand>();

            string commandListStr = string.Empty;
            try
            {
                foreach (var propertyInfo in propertyInfos)
                {
                    SscCommand sscCommand = (SscCommand) propertyInfo.GetValue(Instance);
                    commandList.Add(sscCommand);
                }

                commandListStr = JsonConvert.SerializeObject(commandList);

                if (!_appActivated)
                {
                    commandListStr = commandListStr.Replace("\"Enabled\":true", "\"Enabled\":false");
                    ActivateCommand.Enabled = false;
                    string disabledActiveCommand = JsonConvert.SerializeObject(ActivateCommand);
                    ActivateCommand.Enabled = true;
                    string enabledActivatedCommand = JsonConvert.SerializeObject(ActivateCommand);

                    commandListStr = commandListStr.Replace(disabledActiveCommand, enabledActivatedCommand);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"GetCurrentCommands => {ex}");
            }

            return commandListStr;
        }

        private bool _appActivated;
        public void SetAppActivatedState(bool appActivated)
        {
            _appActivated = appActivated;
        }

        public bool GetAppActivatedState()
        {
            return _appActivated;
        }

        public void SetCommandsStateInNonDiscussionClass()
        {
            GotoInteractiveCommand.Enabled = false;
            GotoDiscussionCommand.Enabled = false;
            GotoInteractiveWithoutLiveCommand.Enabled = false;
            GotoClassCommand.Enabled = false;

            SpeakCommand.Enabled = true;
            RecordCommand.Enabled = true;
            PushLiveCommand.Enabled = true;
            DocCommand.Enabled = true;
            AverageCommand.Enabled = true;
            BigSmallsCommand.Enabled = true;
            CloseupCommand.Enabled = true;
            InteractionCommand.Enabled = true;
            SpeakerCommand.Enabled = true;
            ShareCommand.Enabled = true;
            ExitClassCommand.Enabled = true;

            SendCommandsStateUpdatedNotification();
        }

        public void SetCommandsStateInDiscussionClass()
        {
            GotoInteractiveCommand.Enabled = false;
            GotoDiscussionCommand.Enabled = false;
            GotoInteractiveWithoutLiveCommand.Enabled = false;
            GotoClassCommand.Enabled = false;

            SpeakCommand.Enabled = true;
            RecordCommand.Enabled = true;
            PushLiveCommand.Enabled = false;
            DocCommand.Enabled = true;
            AverageCommand.Enabled = true;
            BigSmallsCommand.Enabled = true;
            CloseupCommand.Enabled = true;
            InteractionCommand.Enabled = false;
            SpeakerCommand.Enabled = false;
            ShareCommand.Enabled = false;
            ExitClassCommand.Enabled = true;

            SendCommandsStateUpdatedNotification();
        }



        public void SetCommandsBackToMainView()
        {
            GotoInteractiveCommand.Enabled = true;
            GotoDiscussionCommand.Enabled = true;
            GotoInteractiveWithoutLiveCommand.Enabled = true;
            GotoClassCommand.Enabled = true;

            SpeakCommand.Enabled = false;
            RecordCommand.Enabled = false;
            PushLiveCommand.Enabled = false;
            DocCommand.Enabled = false;
            AverageCommand.Enabled = false;
            BigSmallsCommand.Enabled = false;
            CloseupCommand.Enabled = false;
            InteractionCommand.Enabled = false;
            SpeakerCommand.Enabled = false;
            ShareCommand.Enabled = false;
            ExitClassCommand.Enabled = false;

            SendCommandsStateUpdatedNotification();
        }

        public void SetGotoClassCommandState(bool enabled)
        {
            GotoClassCommand.Enabled = enabled;

            SendCommandsStateUpdatedNotification();
        }

        private void SendCommandsStateUpdatedNotification()
        {
            _eventAggregator.GetEvent<CommandUpdatedEvent>().Publish();
        }

        public SscCommand GotoInteractiveCommand { get; private set; }
        public SscCommand GotoDiscussionCommand{ get; private set; }
        public SscCommand GotoInteractiveWithoutLiveCommand{ get; private set; }
        public SscCommand GotoClassCommand{ get; private set; }


        public SscCommand UpCommand{ get; private set; }
        public SscCommand DownCommand{ get; private set; }
        public SscCommand LeftCommand{ get; private set; }
        public SscCommand RightCommand{ get; private set; }
        public SscCommand ConfirmCommand{ get; private set; }
        public SscCommand CancelCommand{ get; private set; }
        public SscCommand ForwardCommand { get; private set; }
        public SscCommand BackwardCommand { get; private set; }


        public SscCommand SpeakCommand { get; private set; }
        public SscCommand RecordCommand { get; private set; }
        public SscCommand PushLiveCommand { get; private set; }
        public SscCommand DocCommand { get; private set; }
        public SscCommand AverageCommand { get; private set; }
        public SscCommand BigSmallsCommand { get; private set; }
        public SscCommand CloseupCommand { get; private set; }
        public SscCommand InteractionCommand { get; private set; }
        public SscCommand SpeakerCommand { get; private set; }
        public SscCommand ShareCommand { get; private set; }
        public SscCommand ExitClassCommand { get; private set; }

        public SscCommand ActivateCommand { get; set; }
    }
}