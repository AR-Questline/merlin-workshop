using System;
using UnityEngine;

namespace Awaken.TG.Utility.Attributes {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ARTimeSpanAttribute : PropertyAttribute {
        public bool showDays;
        public bool showHours;
        public bool showMinutes;
        
        public ARTimeSpanAttribute(bool showDays = true, bool showHours = true, bool showMinutes = true) {
            this.showDays = showDays;
            this.showHours = showHours;
            this.showMinutes = showMinutes;
        }
    }
}
