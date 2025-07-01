using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories {
    public partial class NpcInvolvementOwner : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        NpcInvolvement Involvement { get; }
        
        public NpcInvolvementOwner(NpcInvolvement involvement) {
            Involvement = involvement;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            Involvement.ListenTo(Events.AfterDiscarded, _ => Discard(), this);
        }

        async UniTask StopInvolvement() {
            await Involvement.EndTalk();
            Involvement.Discard();
        }

        bool IsInStory(Story api) {
            return Involvement.ParentModel == api;
        }
        
        public static async UniTask EnsureInvolvementOwned(NpcInvolvement involvement) {
            var npc = involvement.Owner;
            
            if (npc.TryGetElement(out NpcInvolvementOwner involvementOwner)) {
                if (involvementOwner.Involvement == involvement) {
                    return;
                }
                if (!involvementOwner.IsInStory(involvement.ParentModel)) {
                    await involvementOwner.StopInvolvement();
                } else {
                    involvementOwner.Discard();
                }
            }
            
            npc.AddElement(new NpcInvolvementOwner(involvement));
        }
    }
}