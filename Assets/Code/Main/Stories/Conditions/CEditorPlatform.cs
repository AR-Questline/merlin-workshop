using System;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Technical: Platform check")]
    public class CEditorPlatform : EditorCondition {
        public CPlatform.PlatformType platformType;
        
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CPlatform {
                platformType = this.platformType
            };
        }
    }
    
    public partial class CPlatform : StoryCondition {
        public PlatformType platformType;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return platformType switch {
                PlatformType.PC => PlatformUtils.IsWindows,
                PlatformType.SteamDeck => PlatformUtils.IsSteamDeck,
                PlatformType.Xbox => PlatformUtils.IsXbox,
                PlatformType.Playstation => PlatformUtils.IsPS5,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public enum PlatformType : byte {
            PC = 0,
            SteamDeck = 1,
            Xbox = 2,
            Playstation = 3
        }
    }
}