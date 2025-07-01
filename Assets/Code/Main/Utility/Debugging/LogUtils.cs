using System;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.Debugging {
    public static class LogUtils {
        public static void LogEventException(Exception e, IEvent evt, IEventSource source, IEventListener listener) {
            string sourceName = "";
            string ownerName = "";

            // don't want to crash while logging other error!
            try {
                sourceName = source is INamed sourceNamed ? sourceNamed.DisplayName + ": " + sourceNamed.DebugName : "";
                ownerName = listener.Owner is INamed ownerNamed ? ownerNamed.DisplayName + ": " + ownerNamed.DebugName : "";
            } catch (Exception e2) {
                Log.Important?.Error("Exception while logging event exception!\n" + e2);
            }
            sourceName += $"({source.ID})";
            if (listener.Owner != null) {
                ownerName += $"({listener.Owner})";
            }

            // If exception is View related capture that view and pass as log target
            View target = null;
            {if (listener.Owner is View view) {
                target = view;
            }}
            {if (source is View view) {
                target = view;
            }}
            

            Log.Critical
                   ?.Error($"Exception below happened on event '{evt.Name}' from '{sourceName}'. Invoked on listener '{ownerName}'. Selector '{listener.Selector}'", target);
            Debug.LogException(e, target);
        }
        
        /// <summary>
        /// Searches for INamed in target and returns its DisplayName and DebugName, otherwise returns target.ToString()
        /// </summary>
        public static string GetDebugName(object target) {
            string sourceName = "";

            // don't want to crash while logging other error!
            try {
                if (target is INamed sourceNamed) {
                    sourceName = sourceNamed.DisplayName + ": " + sourceNamed.DebugName;
                } else if (target is IElement element) {
                    var named = element.GetModelInParent<INamed>();
                    if (named != null) {
                        sourceName = named.DisplayName + ": " + named.DebugName;
                    }
                }
            } catch {
                // ignored
            }
            sourceName += $"({target})";
            
            return sourceName;
        }

        public static string GetDebugName(Story story) {
            return $"Story [{story?.Guid ?? "Null"}]";
        }
    }
}