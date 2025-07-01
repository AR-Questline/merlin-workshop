using System.Collections.Generic;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.NPCs.Providers {
    public sealed partial class NpcIsGroundedHandler : Element<NpcElement> {
        public override bool IsNotSaved => true;

        readonly List<IIsGroundedProvider> _isGroundedProviders = new();

        bool IsGroundedInternal {
            get {
                int count = _isGroundedProviders.Count;
                if (count <= 0) {
                    return true;
                }

                for (int i = 0; i < count; i++) {
                    if (_isGroundedProviders[i].IsGrounded) {
                        return true;
                    }
                }

                return false;
            }
        }

        public static void AddIsGroundedProvider(NpcElement npc, IIsGroundedProvider isGroundedProvider) {
            var handler = npc.IsGroundedHandler ?? npc.AddElement<NpcIsGroundedHandler>();
            handler.AddProvider(isGroundedProvider);
        }

        public static void RemoveIsGroundedProvider(NpcElement npc, IIsGroundedProvider isGroundedProvider) {
            npc.IsGroundedHandler?.RemoveProvider(isGroundedProvider);
        }

        public static bool IsGrounded(NpcElement npc) {
            return npc.IsGroundedHandler?.IsGroundedInternal ?? true;
        }
        
        void AddProvider(IIsGroundedProvider isGroundedProvider) {
            _isGroundedProviders.Add(isGroundedProvider);
        }

        void RemoveProvider(IIsGroundedProvider isGroundedProvider) {
            _isGroundedProviders.Remove(isGroundedProvider);
        }
    }
}