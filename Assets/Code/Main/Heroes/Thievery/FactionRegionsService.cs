using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Heroes.Thievery {
    public partial class FactionRegionsService : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.FactionRegionsService;
        public Domain Domain => Domain.CurrentMainSceneOrPreviousMainSceneWhileDropping();
        
        [Saved] Dictionary<SpecId, CrimeRegion.Data> _modifiedRegions = new();
        
        public bool RemoveOnDomainChange() {
            _modifiedRegions.Clear();
            return false;
        }
        
        // === Static API
        
        static readonly List<CrimeRegion> ToInitialize = new();

        public static void Initialize(CrimeRegion region) {
            if (World.Services.TryGet(out FactionRegionsService service)) {
                service.TryApply(region);
            } else {
                ToInitialize.Add(region);
            }
        }

        public static void Uninitialize(CrimeRegion region) {
            ToInitialize.Remove(region);
        }

        public static void InitializeWaiting(FactionRegionsService service) {
            foreach (var region in ToInitialize) {
                service.TryApply(region);
            }
            ToInitialize.Clear();
        }
        
        // === Modification management

        public void Modify(CrimeRegion region, CrimeRegion.Data newProperties) {
            _modifiedRegions[region.SceneId] = newProperties;
            region.ApplyModification(newProperties);
        }
        
        [UnityEngine.Scripting.Preserve]
        public CrimeRegion.Data GetModifications(CrimeRegion region) {
            if (!WasModified(region))
                return region.GetData();
            return _modifiedRegions[region.SceneId];
        }
        
        bool WasModified(CrimeRegion region) {
            return _modifiedRegions.ContainsKey(region.SceneId);
        }
        
        void TryApply(CrimeRegion region) {
            if (!WasModified(region)) {
                region.Refresh();
                return;
            }
            region.ApplyModification(_modifiedRegions[region.SceneId]);
        }
    }
}