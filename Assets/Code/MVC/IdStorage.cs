using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.MVC {
    public partial class IdStorage : IService {

        // === State
        SaveIdStorage _saveSlotStorage;
        Dictionary<Type, int> _notSavedStorage;

        public void Init() {
            _notSavedStorage = new();
            World.Services.Register(_saveSlotStorage = new SaveIdStorage());
        }
        
        public int NextIdFor(Model model, Type modelType = null, bool forceNotSaved = false) {
            var savedType = forceNotSaved ? (ushort)0 : model.TypeForSerialization;
            if (savedType == 0) {
                modelType ??= model.GetType();
                _notSavedStorage.TryGetValue(modelType, out int id);
                _notSavedStorage[modelType] = id + 1;
                return id;
            } else {
                _saveSlotStorage.storage.TryGetValue(savedType, out int id);
                _saveSlotStorage.storage[savedType] = id + 1;
                return id;
            }
        }
        
        public partial class SaveIdStorage : SerializedService, IDomainBoundService {
            public override ushort TypeForSerialization => SavedServices.SaveIdStorage;
            public Domain Domain => Domain.Gameplay;

            // === State
            [Saved] public Dictionary<ushort, int> storage = new();

            public bool RemoveOnDomainChange() {
                storage.Clear();
                return false;
            }
        }
    }
}