using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Tutorials {
    public static class TutorialKeys {
        public const string Forced = "FORCE";
        public const string Break = "GLOBALUNIQUE_Break";

        public static void Consume(string key) => RetrieveFacts?.Set(key, true);
        [UnityEngine.Scripting.Preserve] public static void Consume(TutKeys key) => Consume(FullKey(key));
        public static void Consume(SequenceKey key) => Consume(FullKey(key));
        
        public static void Remove(string key) => RetrieveFacts?.Set(key, false);
        public static void Remove(TutKeys key) => Remove(FullKey(key));
        [UnityEngine.Scripting.Preserve] public static void Remove(SequenceKey key) => Remove(FullKey(key));
        
        public static bool IsConsumed(string key) => RetrieveFacts?.Get(key, false) ?? false;
        public static bool IsConsumed(TutKeys key) => IsConsumed(FullKey(key));
        public static bool IsConsumed(SequenceKey key) => IsConsumed(FullKey(key));
        
        public static void Clear() => RetrieveFacts.Clear();
        
        public static string FullKey(TutKeys key) => $"TUTORIAL_{key}";
        public static string FullKey(SequenceKey key) => $"SEQUENCE_{key}";
        
        static ContextualFacts RetrieveFacts => World.Services?.Get<GameplayMemory>().Context("tutorial_context");
        
        public static readonly SequenceKey[] AllSequenceKeys = (SequenceKey[]) System.Enum.GetValues(typeof(SequenceKey));
    }
}