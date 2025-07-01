using System;
using Sirenix.OdinInspector;

namespace Awaken.Utility.Times {
    [Serializable]
    public struct SerializableDate {
        [MinValue(2000), HorizontalGroup(Gap = 8), LabelWidth(40)]
        public ushort year;
        [MinValue(1), MaxValue(12), HorizontalGroup, LabelWidth(40)]
        public byte month;
        [MinValue(1), MaxValue(31), HorizontalGroup, LabelWidth(40), InlineButton(nameof(SetDateAsToday), "Today")]
        public byte day;
            
        public readonly DateTime AsDateTime() => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        
        void SetDateAsToday() {
            DateTime dateTime = DateTime.UtcNow;
            year = (ushort)dateTime.Year;
            month = (byte)dateTime.Month;
            day = (byte)dateTime.Day;
        }
    }
}
