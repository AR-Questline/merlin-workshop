using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public interface INpcInteractionForwarder : INpcInteractionSearchable {
        private static List<INpcInteraction> s_allInteractionList = new();
        
        INpcInteraction GetInteraction(NpcElement npc);
        void GetAllInteractions(List<INpcInteraction> interactions);
        bool INpcInteractionSearchable.IsValid() {
            if (this is MonoBehaviour mb && mb == null) {
                return false;
            }
            GetAllInteractions(s_allInteractionList);
            bool result = s_allInteractionList.All(i => i.IsValid());
            s_allInteractionList.Clear();
            return result;
        }
    }
}