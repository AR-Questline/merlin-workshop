using System;
using Awaken.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Fights.NPCs.Presences {
    public partial class UniqueNpcStash : SerializedService, IDomainBoundService {
        static readonly UniversalProfilerMarker StashMarker = new("UniqueNpcStash.Stash");
        static readonly UniversalProfilerMarker ModelsToSaveMarker = new("UniqueNpcStash.ModelsToSave");

        public override ushort TypeForSerialization => SavedServices.UniqueNpcStash;
        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() => true;
        
        [Saved] Dictionary<LocationTemplate, byte[]> _dataByNpc = new();

        Dictionary<NpcElement, int> _npcRefCount = new();

        public bool TryUnstash(LocationTemplate template, out NpcElement npc) {
            npc = null;
            if (_dataByNpc.TryGetValue(template, out var data)) {
                var stream = new MemoryStream(data);
                LoadSave.Get.LoadSystem.Deserialize(Domain.Gameplay, stream, out var models);
                stream.Dispose();
                npc = TryFindRestoredNpc(models);
                _dataByNpc.Remove(template);
            }
            return npc != null;
        }

        public void Stash(NpcElement npc) {
            StashMarker.Begin();
            PrepareNpcForStash(npc);
            var modelsToStash = ModelsToSave(npc);

            var stream = new NativeMemoryStream(1024, Allocator.Temp);
            LoadSave.Get.SaveSystem.SerializeNpcStash(modelsToStash, stream);

            _dataByNpc[npc.ParentModel.Template] = stream.ToArray();

            stream.Dispose();

            for (int i = modelsToStash.Count - 1; i >= 0; i--) {
                if (modelsToStash[i] is not Element) {
                    try {
                        modelsToStash[i].DiscardFromDomainDrop();
                    } catch (Exception e) {
                        Log.Critical?.Error($"DOMAIN ERROR! Exception below happened while discarding model: {modelsToStash[i].ID}");
                        Debug.LogException(e);
                    
                        string summary = "DOMAIN ERROR! Model discard failed";
                        string description = $"Discard failed for: {modelsToStash[i].ID} while stashing single unique NPC {npc.ID}";
                        AutoBugReporting.SendAutoReport(summary, description);
                        DomainErrorPopup.Display();
                    }
                }
            }

            StatTweak.CleanupObsoleteStatTweaks();
            StashMarker.End();
        }

        void PrepareNpcForStash(NpcElement npc) {
            npc.ParentModel.SetCoordsBeforeSave(NpcPresence.AbyssPosition);
        }

        public void StashAllUnused() {
            foreach (var npc in World.All<NpcElement>().ToArraySlow()) {
                if (npc.IsUnique && _npcRefCount.TryGetValue(npc, out _) == false) {
                    Stash(npc);
                }
            }
        }
        
        public void MarkUsed(NpcElement npc) {
            var count = _npcRefCount.GetValueOrDefault(npc, 0);
            _npcRefCount[npc] = count + 1;
        }

        public int GetUseAmount(NpcElement npc) {
            if (_npcRefCount.TryGetValue(npc, out var useAmount)) {
                return useAmount;
            }
            return 0;
        }

        public void MarkUnused(NpcElement npc) {
            if (!_npcRefCount.TryGetValue(npc, out var count)) {
                Log.Critical?.Error($"Tried to mark unused NPC {npc.ID} that was never used before!");
                return;
            }
            if (count <= 1) {
                _npcRefCount.Remove(npc);
                if (!npc.HasBeenDiscarded) {
                    Stash(npc);
                }
            } else {
                _npcRefCount[npc] = count - 1;
            }
        }

        static StructList<Model> ModelsToSave(NpcElement npc) {
            ModelsToSaveMarker.Begin();
            var modelsToSave = new StructList<Model>(42);
            foreach (var model in World.AllInOrderReadonlyNotValidated()) {
                if (model.HasBeenDiscarded) {
                    continue;
                }
                if (model.CurrentDomain != Domain.Gameplay) {
                    continue;
                }

                IModel outermost = model;
                while (outermost is Element element) {
                    outermost = element.GenericParentModel;
                }
                bool isRelevant = outermost switch {
                    Location location => location == npc.ParentModel,
                    Item item => item.Owner == npc.ParentModel || (item.Owner is Shop shop && shop.ParentModel == npc.ParentModel),
                    _ => false,
                };
                if (isRelevant) {
                    modelsToSave.Add(model);
                    model.PrepareForSaving();
                }
            }
            ModelsToSaveMarker.End();
            return modelsToSave;
        }

        static NpcElement TryFindRestoredNpc(List<Model> models) {
            NpcElement npc = null;
            foreach (var model in models) {
                if (model is NpcElement restoredNpc) {
                    if (npc != null) {
                        Log.Critical?.Error("Multiple NPCs in single unique restore!");
                    } else {
                        npc = restoredNpc;
                    }
                }
            }
            if (npc == null) {
                Log.Critical?.Error("No NPC in single unique restore!");
            }
            return npc;
        }
    }
}