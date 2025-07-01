using Awaken.TG.Main.General.Configs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Gems {
    public static class GemUtils {
        public static int AddGemSlotCost => World.Services.Get<GameConstants>().addGemSlotCost;
        public static int AttachGemCost => World.Services.Get<GameConstants>().attachGemCost;
        public static int RetrieveGemSlotCost => World.Services.Get<GameConstants>().retrieveGemSlotCost;
    }
}