using System.Runtime.CompilerServices;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.NPCs {
    public static class UniqueNpcUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InvalidLocation(TemplateReference template, Object context = null) {
#if DEBUG
            return template is { IsSet: true } && !Check(template.Get<LocationTemplate>(), context);
#else
            return false;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static bool Check(Location location, Object context = null) {
#if DEBUG
            return !location.TryGetElement(out NpcElement npc) || Check(npc, context);
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(NpcElement npc, Object context = null) {
#if DEBUG
            if (!npc.IsUnique) {
                Log.Important?.Error($"Repetitive Npc {npc} must not be used as unique!", context ?? npc.ParentModel.Spec);
                return false;
            }
            return true;
#else
            return true;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(LocationTemplate template, Object context = null) {
#if DEBUG
            return Check(template.GetComponentInChildren<NpcAttachment>(), context);
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static bool Check(LocationSpec spec, Object context = null) {
#if DEBUG
            return Check(spec.GetComponentInChildren<NpcAttachment>(), context);
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(NpcAttachment npcAttachment, Object context = null) {
#if DEBUG
            if (!npcAttachment.IsUnique) {
                Log.Important?.Error("Repetitive Npc must not be used as unique!", context ?? npcAttachment);
                return false;
            }
            return true;
#else
            return true;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnique(TemplateReference template) {
            return template is { IsSet: true } && IsUnique(template.Get<LocationTemplate>());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnique(LocationTemplate template) {
            return template.GetComponentInChildren<NpcAttachment>()?.IsUnique ?? false;
        }
        
        public static NpcElement GetNpcFromLocation(this Location location) {
            return location.TryGetElement<NpcElement>() ?? location.TryGetElement<NpcPresence>()?.AliveNpc;
        }
    }

    public static class RepetitiveNpcUtils {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InvalidLocation(TemplateReference template, Object context = null) {
#if DEBUG
            return template is { IsSet: true } && !Check(template.Get<LocationTemplate>(), context);
#else
            return false;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(Location location, Object context = null) {
#if DEBUG
            return !location.TryGetElement(out NpcElement npc) || Check(npc, context);
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(NpcElement npc, Object context = null) {
#if DEBUG
            if (npc.IsUnique) {
                Log.Important?.Error($"Unique Npc {npc} must not be used as repetitive!", context ?? npc.ParentModel.Spec);
                return false;
            }
            return true;
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(LocationTemplate template, Object context = null) {
#if DEBUG
            return Check(template.GetComponentInChildren<NpcAttachment>(), context);
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static bool Check(LocationSpec spec, Object context = null) {
#if DEBUG
            return Check(spec.GetComponentInChildren<NpcAttachment>(), context);
#else
            return true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Check(NpcAttachment npcAttachment, Object context = null) {
#if DEBUG
            if (npcAttachment.IsUnique) {
                Log.Important?.Error("Unique Npc must not be used as repetitive!", context ?? npcAttachment);
                return false;
            }
            return true;
#else
            return true;
#endif
        }
    }
}