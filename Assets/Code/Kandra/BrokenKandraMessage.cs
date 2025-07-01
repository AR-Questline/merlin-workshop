using System.Collections.Generic;
using System.Text;
using Awaken.CommonInterfaces;
using Awaken.Kandra.Managers;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using UnityEngine;

namespace Awaken.Kandra {
    public static class BrokenKandraMessage {
        static bool s_outOfMemoryDialogShown;
        static HashSet<KandraMesh> s_dataMismatchedShown = new HashSet<KandraMesh>();

        public static void EDITOR_RuntimeReset() {
            s_outOfMemoryDialogShown = false;
        }

        public static string AppendMessageInfo(string message, uint wantedElements, MemoryBookkeeper memory) {
            if (message == null) {
                return null;
            }
            return $"{message}\n[{memory.Name}] Wanted to allocate {wantedElements} elements, but only {memory.MaxFreeElements} are available";
        }
        
        public static void OutOfMemory(string inputMessage, KandraRenderer renderer) {
            StringBuilder sb = new StringBuilder();

            sb.Append("Out of memory for ");
            sb.Append(renderer.name);
            sb.Append("! ");
            sb.Append(inputMessage);
            sb.Append("\nheroPosition: ");
            sb.Append(HeroPosition.Value);
            sb.AppendLine();
            sb.AppendLine("After that point there is no guarantee that the application will work correctly");
            sb.AppendLine("If there is many NPCs in scene limit the number, otherwise contact programmers");

            AppendInfo(sb, RigManager.EditorAccess.Get().BonesMemory);
            AppendInfo(sb, MeshManager.EditorAccess.Get().BindPosesMemory);
            AppendInfo(sb, MeshManager.EditorAccess.Get().VerticesMemory);
            AppendInfo(sb, BonesManager.EditorAccess.Get().SkinBonesMemory);
            AppendInfo(sb, SkinningManager.EditorAccess.Get().SkinVertsMemory);
            AppendInfo(sb, BlendshapesManager.EditorAccess.Get().BlendshapesMemory);

            var message = sb.ToString();
            Log.Critical?.Error(message, renderer, LogOption.NoStacktrace);

#if UNITY_EDITOR
            if (!s_outOfMemoryDialogShown) {
                UnityEditor.EditorUtility.DisplayDialog("Kandra",
                    $"Kandra out of memory at\n{HeroPosition.Value}\nKandra did brrrryyyyyy and will not render correctly for some time\n" +
                    "Please reduce number of NPCs or cut meshes complexity or let know designers the place where it happen\nPosition is in your clipboard",
                    "OK");
                GUIUtility.systemCopyBuffer = HeroPosition.Value.ToString();
            }
            s_outOfMemoryDialogShown = true;
#endif

            static void AppendInfo(StringBuilder sb, in MemoryBookkeeper memory) {
                sb.AppendLine($"{memory.Name}: Peak: {memory.PeakUsage}, MaxFree: {memory.MaxFreeElements}");
            }
        }

        public static void DataMismatch(KandraMesh kandraMesh, uint expectedSize, uint serializedData) {
            var message = $"Kandra mesh {kandraMesh} serialized data size mismatch. Expected: {expectedSize}, got: {serializedData}";
#if UNITY_EDITOR
            message += $"\nAfter that point renderers with {kandraMesh} will be visually broken";
            Log.Critical?.Error(message, kandraMesh);
            if (s_dataMismatchedShown.Add(kandraMesh)) {
                UnityEditor.EditorUtility.DisplayDialog("Kandra", message, "OK");
            }
#else
            Log.Critical?.Error(message, kandraMesh);
#endif
        }
    }
}