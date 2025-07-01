using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Stories.Choices {
    public class ProficiencyHoverInfo : IHoverInfo {
        public string InfoGroupName { get; set; }
        public string InfoName { get; set; }
        public string InfoDescription{ get; set; }
        public ShareableSpriteReference InfoIcon { get; set; }
        
        public ProficiencyHoverInfo(string groupName, ProfStatType profType, StatValue statValue) {
            InfoGroupName = groupName;
            InfoName = $"{profType.DisplayName} +{statValue.value}";
            InfoDescription = profType.Description;
            InfoIcon = profType.GetIcon?.Invoke();
        }
    }
}