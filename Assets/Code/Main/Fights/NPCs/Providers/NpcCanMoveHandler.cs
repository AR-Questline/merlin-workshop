using System.Collections.Generic;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.NPCs.Providers {
    public partial class NpcCanMoveHandler : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        readonly List<ICanMoveProvider> _canMoveProviders = new();
        bool CanMoveInternal {
            get {
                int count = _canMoveProviders.Count;
                for (int i = 0; i < count; i++) {
                    if (_canMoveProviders[i].CanMove == false) {
                        return false;
                    }
                }

                return true;
            }
        }

        bool CanOverrideDestinationInternal {
            get {
                int count = _canMoveProviders.Count;
                for (int i = 0; i < count; i++) {
                    if (_canMoveProviders[i].CanOverrideDestination == false) {
                        return false;
                    }
                }

                return true;
            }
        }

        bool ResetMovementSpeedInternal {
            get {
                int count = _canMoveProviders.Count;
                for (int i = 0; i < count; i++) {
                    if (_canMoveProviders[i].ResetMovementSpeed) {
                        return true;
                    }
                }

                return false;
            }
        } 
        
        public static void AddCanMoveProvider(NpcElement npc, ICanMoveProvider canMoveProvider) {
            var handler = npc.CanMoveHandler ?? npc.AddElement<NpcCanMoveHandler>();
            handler.AddProvider(canMoveProvider);
        }

        public static void RemoveCanMoveProvider(NpcElement npc, ICanMoveProvider canMoveProvider) {
            npc.CanMoveHandler?.RemoveProvider(canMoveProvider);
        }

        public static bool CanMove(NpcElement npc) {
            return npc.CanMoveHandler?.CanMoveInternal ?? true;
        }
        
        public static bool CanOverrideDestination(NpcElement npc) {
            return npc.CanMoveHandler?.CanOverrideDestinationInternal ?? true;
        }

        public static bool ShouldResetMovementSpeed(NpcElement npc) {
            return npc.CanMoveHandler?.ResetMovementSpeedInternal ?? false;
        }
        
        void AddProvider(ICanMoveProvider canMoveProvider) {
            _canMoveProviders.Add(canMoveProvider);
        }

        void RemoveProvider(ICanMoveProvider canMoveProvider) {
            _canMoveProviders.Remove(canMoveProvider);
        }
    }
}