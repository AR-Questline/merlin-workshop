// From multiple answers at: https://answers.unity.com/questions/956123/add-and-select-game-view-resolution.html
using System;
using System.Reflection;
using UnityEditor;

namespace Awaken.TG.Code.Editor.Tests.Performance {
    public static class GameViewUtils {
        static object gameViewSizesInstance;
        static MethodInfo getGroup;
 
        static GameViewUtils() {
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProperty = singleType.GetProperty("instance");
            getGroup = sizesType.GetMethod("GetGroup");
            gameViewSizesInstance = instanceProperty.GetValue(null, null);
        }

        public static void SetSize(int width, int height) {
            if (!SizeExists(width, height)) {
                AddCustomSize(width, height, "");
            }

            SetSize(FindSize(width, height));
        }

        static bool SizeExists(int width, int height)
        {
            return FindSize(width, height) != -1;
        }

        static int FindSize(int width, int height)
        {
            var group = GetGroup(GameViewSizeGroupType.Standalone);
            var groupType = group.GetType();
            var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            var getCustomCount = groupType.GetMethod("GetCustomCount");
            int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var gvsType = getGameViewSize.ReturnType;
            var widthProp = gvsType.GetProperty("width");
            var heightProp = gvsType.GetProperty("height");
            var indexValue = new object[1];
            for(int i = 0; i < sizesCount; i++) {
                indexValue[0] = i;
                var size = getGameViewSize.Invoke(group, indexValue);
                int sizeWidth = (int)widthProp.GetValue(size, null);
                int sizeHeight = (int)heightProp.GetValue(size, null);
                if (sizeWidth == width && sizeHeight == height)
                    return i;
            }
            return -1;
        }
 
        static object GetGroup(GameViewSizeGroupType type) => getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });

        static void AddCustomSize(int width, int height, string text) {
            const string assemblyName = "UnityEditor.dll";
            
            var group = GetGroup(GameViewSizeGroupType.Standalone);
            var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
            
            Assembly assembly = Assembly.Load(assemblyName);
            Type gameViewSize = assembly.GetType("UnityEditor.GameViewSize");
            Type gameViewSizeType = assembly.GetType("UnityEditor.GameViewSizeType");
            ConstructorInfo ctor = gameViewSize.GetConstructor(new[]
            {
                gameViewSizeType,
                typeof(int),
                typeof(int),
                typeof(string)
            });
            var newSize = ctor.Invoke(new object[] { 1, width, height, text });
            addCustomSize.Invoke(group, new[] { newSize });
        }

        static void SetSize(int index) {
            var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameWindow = EditorWindow.GetWindow(gameViewType);
            var setSizeMethod = gameViewType.GetMethod("SizeSelectionCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            setSizeMethod.Invoke(gameWindow, new object[] { index, null });
        }
    }
}