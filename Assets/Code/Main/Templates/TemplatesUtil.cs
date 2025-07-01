using System;
using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Templates {
    public static class TemplatesUtil {
        public const string AddressableLabel = "template";
        public const string AddressableLabelSO = "templateSO";
        public const string GUIDSeparator = "--";

        public static Object TemplateToObject(ITemplate template) {
            if (template is MonoBehaviour mb) {
                return mb.gameObject;
            }
            if (template is ScriptableObject so) {
                return so;
            }
            return null;
        }

        public static ITemplate ObjectToTemplateUnsafe(Object obj) {
            if (obj is ITemplate template) {
                return template;
            }
            if (obj is GameObject go) {
                ITemplate component = go.GetComponent<ITemplate>();
                if (component != null) {
                    return component;
                }
            }
            return null;
        }
        
        public static ITemplate ObjectToTemplate(Object obj) {
            ITemplate result = ObjectToTemplateUnsafe(obj);
            if (result != null) {
                return result;
            }
            Exception exception = new ArgumentException($"Object is not template: {obj?.name} ({obj?.GetType()})");;
            Debug.LogException(exception, obj);
            return null;
        }
        
        public static T Load<T>(string guid) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return EDITOR_LoadByAssetDatabase<T>(guid);
            } else
#endif
            {
                return World.Services.Get<TemplatesProvider>().Get<T>(guid);
            }
        }

        public static bool TryGetGUIDFromAddress(string address, out string guid) {
            int separatorIndex = address.LastIndexOf(GUIDSeparator, StringComparison.InvariantCulture);
            guid = string.Empty;
            if (separatorIndex < 0) {
                Log.Important?.Error($"Template address {address} doesn't contain separator");
                return false;
            }

            guid = address.Substring(separatorIndex + GUIDSeparator.Length);
            return true;
        }

#if UNITY_EDITOR
        public static void EDITOR_AssignGuid(ITemplate template, Object obj) {
            if (string.IsNullOrWhiteSpace(template.GUID)) {
                string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
                template.GUID = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            }
        }

        public static T EDITOR_LoadByAssetDatabase<T>(string guid) {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null) {
                Log.Important?.Error($"Failed to load object with guid: {guid}. Path: {path}");
                return default;
            }
            ITemplate template = ObjectToTemplate(obj);

            if (template is T result) {
                template.GUID = guid;
                return result;
            }
            Exception exception = new ArgumentException($"Invalid guid: {guid}");
            Debug.LogException(exception, obj);
            return default;
        }
#endif
    }
}