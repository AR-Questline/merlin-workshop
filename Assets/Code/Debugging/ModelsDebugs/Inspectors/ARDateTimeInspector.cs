using System;
using Awaken.Utility.Times;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class ARDateTimeInspector : MemberListItemInspector<ARDateTime> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var dateValue = CastedValue(value);
            GUILayout.Label(member.Name);
            
            using var checkScope = new TGGUILayout.CheckChangeScope();
            GUILayout.BeginHorizontal();
            int day = TGGUILayout.DelayedIntField("D", dateValue.DayOfTheMonth);
            int hour = TGGUILayout.DelayedIntField("H", dateValue.Hour);
            int minute = TGGUILayout.DelayedIntField("M", dateValue.Minutes);
            if (checkScope) {
                ARDateTime newTime = new DateTime(dateValue.Year, dateValue.Month, day, hour, minute, 0);
                member.SetValue(target, newTime);
            }

            GUILayout.EndHorizontal();
        }
    }
}