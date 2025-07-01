using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Regrowables.Moths {
    public class VfxMothUpdateHandler : UnityUpdateProvider.IWithUpdateGeneric {
        const float UpdateInterval = 0.5f;
        static VfxMothUpdateHandler s_instance;
        
        readonly List<VFXMoths> _vfxMoths = new();
        float _updateTimer;
        
        public static void EDITOR_RuntimeReset() {
            s_instance = null;
        }
        
        public static void RegisterVfxMoth(VFXMoths vfxMoth) {
            Get().Register(vfxMoth);
        }
        
        public static void UnregisterVfxMoth(VFXMoths vfxMoth) {
            Get().Unregister(vfxMoth);
        }

        static VfxMothUpdateHandler Get() {
            return s_instance ??= new VfxMothUpdateHandler();
        }

        VfxMothUpdateHandler() {
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }

        void Register(VFXMoths vfxMoth) {
            _vfxMoths.Add(vfxMoth);
        }

        void Unregister(VFXMoths vfxMoth) {
            _vfxMoths.Remove(vfxMoth);
            if (_vfxMoths.IsEmpty()) {
                World.Services.Get<UnityUpdateProvider>().UnregisterGeneric(this);
                s_instance = null;
            }
        }

        public void UnityUpdate() {
            _updateTimer += Time.deltaTime;
            if (_updateTimer < UpdateInterval) {
                return;
            }
            _updateTimer = 0f;
            var hero = Hero.Current;
            if (hero is not { HasBeenDiscarded: false }) {
                return;
            }
            var heroPos = hero.Coords;
            var isHeroCrouching = hero.IsCrouching;
            foreach (VFXMoths vfxMoth in _vfxMoths.Where(vfxMoth => vfxMoth.CanUpdate)) {
                vfxMoth.UpdateMoths(heroPos, isHeroCrouching);
            }
        }
    }
}
