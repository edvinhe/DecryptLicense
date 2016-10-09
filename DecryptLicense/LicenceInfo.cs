using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;

namespace DecryptLicense
{
    class LicenceInfo
    {
        public int DebugMode = -1;
        public static string DefVersion = "1.0.0.2";
        public Dictionary<string, List<Equipment>> DeviceList = new Dictionary<string, List<Equipment>>();
        public Station LocationStation;
        public string Version = "";
        public DateTime EndTime
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.EndTimeValue))
                {
                    return DateTime.MinValue;
                }
                return DateTime.Parse(this.EndTimeValue);
            }
            set
            {
                this.EndTimeValue = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public string EndTimeValue { get; set; }
        public DateTime StartTime
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.StartTimeValue))
                {
                    return DateTime.MinValue;
                }
                return DateTime.Parse(this.StartTimeValue);
            }
            set
            {
                this.StartTimeValue = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public string StartTimeValue { get; set; }
    }
}
