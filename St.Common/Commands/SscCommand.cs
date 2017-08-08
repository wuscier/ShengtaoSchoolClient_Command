using System;
using Newtonsoft.Json;
using Prism.Commands;
using Action = System.Action;

namespace St.Common.Commands
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SscCommand : DelegateCommand
    {
        public SscCommand(Action executeMethod, bool enabled, Func<bool> canExecuteMethod, string name, byte directive,
            bool isIntoClassCommand = false)
            : base(executeMethod, canExecuteMethod)
        {
            Name = name;
            Directive = directive;
            Enabled = enabled;
            IsIntoClassCommand = isIntoClassCommand;
        }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public byte Directive { get; set; }

        [JsonProperty]
        public bool Enabled { get; set; }

        public bool IsIntoClassCommand { get; set; }
    }
}