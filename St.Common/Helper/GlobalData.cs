using System;
using System.Collections.Generic;
using System.Windows;

namespace St.Common
{
    public sealed class GlobalData
    {
        private GlobalData()
        {
            RunMode = RunMode.Product;
            ActiveModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static GlobalData Instance { get; } = new GlobalData();

        public HashSet<string> ActiveModules { get; set; }
        public string SerialNo { get; set; }
        public Version Version { get; set; }
        public Device Device { get; set; }
        public Window UpdatingDialog { get; set; }
        public ViewArea ViewArea { get; set; }
        public RunMode RunMode { get; set; }
        public IntPtr CurWindowHwnd { get; set; }

        public AggregatedConfig AggregatedConfig { get; set; }
    }
}
