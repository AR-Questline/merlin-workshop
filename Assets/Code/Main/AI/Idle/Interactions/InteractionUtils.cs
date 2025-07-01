using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public static class InteractionUtils {
        static List<INpcInteraction> s_listA = new();
        static List<INpcInteraction> s_listB = new();
        
        public static InteractionBookingResult BookOneNpc(this INpcInteraction interaction, ref NpcElement interactingNpc, NpcElement bookingNpc) {
            if (interactingNpc != null) {
                if (interactingNpc == bookingNpc) {
                    return InteractionBookingResult.AlreadyBookedBySameNpc;
                }
                Log.Important?.Error($"Trying to book {interaction} for {bookingNpc} while it is already booked for {interactingNpc}");
                return InteractionBookingResult.AlreadyBookedByOtherNpc;
            }
            
            interactingNpc = bookingNpc;
            return InteractionBookingResult.ProperlyBooked;
        }

        public static bool AreSearchablesTheSameInteraction(NpcElement npc, INpcInteractionSearchable a, INpcInteractionSearchable b) {
            //if NPC is null we need to check all available interactions because of Forwarders and Group Interactions
            if (npc == null) {
                if (a is INpcInteractionForwarder forwarderA) {
                    forwarderA.GetAllInteractions(s_listA);
                } else {
                    s_listA.Add(InteractionProvider.GetInteraction(null, a));
                }
                if (b is INpcInteractionForwarder forwarderB) {
                    forwarderB.GetAllInteractions(s_listB);
                } else {
                    s_listB.Add(InteractionProvider.GetInteraction(null, b));
                }
                
                bool result = s_listA.Intersect(s_listB).Any();
                s_listA.Clear();
                s_listB.Clear();
                return result;
            }

            return InteractionProvider.GetInteraction(npc, a) == InteractionProvider.GetInteraction(npc, b);
        }

        public static INpcInteraction GetUnwrappedInteraction(INpcInteraction interaction) {
            while (interaction is INpcInteractionWrapper wrapper) {
                interaction = wrapper.Interaction;
            }
            return interaction;
        }
    }
}