using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories;

namespace Awaken.TG.Main.Utility.Patchers.Dlc {
    public class DlcPatcherContentPack : DlcPatcher {
        protected override DlcCategory RequiredDlcCategory => DlcCategory.ContentPack;

        string quest1 = "396f44a11c085e945ae0f96c7503ca99"; // I See A Darkness
        string[] ItemSet1 = { // Basic
            "f33b3da69a501be4ca9fdf5a2816cf8c",
            "8384c3bd505ba4a49955132f457c7038",
            "a5651ac25996dcc499a9d51f3e94039c",
            "f7dde784eaf29e44696a57e5996aebf1",
        };
        string receivedFlag1 = "ContentPack:Received1";
        
        // WyrdStalker Kill
        string[] ItemSet2 = { // Knight
            "6c74a3ebb5522ef4c8c0b8ef9b0e44ae",
            "957c69d9cc0d11841972fec968724eaa",
            "73001daafba14a5449a1405faf146c98",
            "c0f60f596ded2774492a73354d26ad10",
            "eb839508c5dc86142847b08010032665",
        };
        string flagToSet2 = "Dead:WyrdStalker";
        string receivedFlag2 = "ContentPack:Received2";
        
        string flag3 = "Bridei:Bloodtaken"; // Things on a Doorstep
        string[] ItemSet3 = { // Warrior
            "0fefac617d9dd4e4d93df4ea9fadc990",
            "ad45965dc6d2e4b47bbd3838f7f6aa72",
            "5d2072d4c7ac9f642bd244cc22c5bb2e",
            "fcee5949112b4a6488cbe3c1daa70eb3",
            "d7f272d6982770f47a4d8fe5cd3f1bd8",
        };
        string receivedFlag3 = "ContentPack:Received3";
        
        string itemRequiredGUID4 = "01952f09c48b3c34683ff992729ef638"; // Foredweller Life Spindle
        string[] ItemSet4 = { // Weaver
            "91275039ea0b07242983b9405771b297",
            "3a11911af0a992f45932cffb87d67419",
            "2a3d3589576ba3e40b85d4ab012750d4",
            "289dcd73a2345b24cad8931da882534d",
            "ed7b2e43063976541838f9ad4a037d68",
        };
        string receivedFlag4 = "ContentPack:Received4";
        
        string quest5 = "91d2d3e6f341edd4cb917defa32d4444"; // Deiform
        string[] ItemSet5 = { // FalseDeity
            "8bee53cf67c8dfe499c285dac5787a72",
            "6cb77269de5b9b641a87e5822c29764c",
        };
        string receivedFlag5 = "ContentPack:Received5";
        
        protected override void OnDlcActivated(bool forTheFirstTime) {
            if (!FlagCondition(receivedFlag1)) {
                if (QuestCompletedCondition(quest1)) {
                    GrantItemSet(ItemSet1);
                    StoryFlags.Set(receivedFlag1, true);
                }
            }

            if (!FlagCondition(receivedFlag2)) {
                if (WyrdStalkerDeadCondition() && !FlagCondition(flagToSet2)) {
                    GrantItemSet(ItemSet2);
                    StoryFlags.Set(flagToSet2, true);
                    StoryFlags.Set(receivedFlag2, true);
                }
            }

            if (!FlagCondition(receivedFlag3)) {
                if (FlagCondition(flag3)) {
                    GrantItemSet(ItemSet3);
                    StoryFlags.Set(receivedFlag3, true);
                }
            }

            if (!FlagCondition(receivedFlag4)) {
                if (ItemRequiredCondition(itemRequiredGUID4)) {
                    GrantItemSet(ItemSet4);
                    StoryFlags.Set(receivedFlag4, true);
                }
            }

            if (!FlagCondition(receivedFlag5)) {
                if (QuestCompletedCondition(quest5)) {
                    GrantItemSet(ItemSet5);
                    StoryFlags.Set(receivedFlag5, true);
                }
            }
        }

        protected override void OnDlcDeactivated() { }
    }
}