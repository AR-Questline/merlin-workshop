using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Character.Features.Config {
    public class NpcRandomizer : ViewComponent<Location> {
        [Space]
        [InfoBox("Randomizer config is being used instead of the old settings", InfoMessageType.Info, "randomizerConfig")]
        [SerializeField, InlineEditor] NPCRandomConfigSO randomizerConfig;
        
        [Space(20)]
        [InfoBox("The following settings are obsolete and will be ignored if Randomizer config is set", InfoMessageType.Warning)]
        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField] BlendShapeConfigSO configSO;

        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField, PrefabAssetReference] List<ARAssetReference> featurePool = new ();
        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField, Range(0,100)] float featurePoolChance = 30;
        
        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField, PrefabAssetReference] List<ARAssetReference> hairPool;
        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField, PrefabAssetReference] List<ARAssetReference> beardPool;

        [Obsolete("Use CharacterDefaultClothes instead"), InfoBox("Underwear is Obsolete. Use CharacterDefaultClothes instead.", InfoMessageType.Warning)]
        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField, PrefabAssetReference] ARAssetReference underwear;

        [DisableIf("@" + nameof(randomizerConfig) + " != null"), SerializeField] List<HairConfig> hairColorPool;

        IWithBodyFeature _withBodyFeature;

        protected override void OnAttach() {
            Init(true).Forget();
        }

        public void ReInitialize() {
            Init(false).Forget();
        }

        async UniTaskVoid Init(bool withFeatures) {
            _withBodyFeature = await Target.WaitForElement<IWithBodyFeature>();
            
            if (_withBodyFeature == null) {
                return;
            }
            
            if (_withBodyFeature is NpcElement element) {
                element.ListenTo(Model.Events.AfterDiscarded, ReInitialize, this);
            }
            
            if (withFeatures) {
                ApplyFeatures(_withBodyFeature.Element<BodyFeatures>());
            }
        }
        
        void ApplyFeatures(BodyFeatures features) {
            if (features.BlockRandomization) return;
            features.BlockRandomization = true;
            
            if (randomizerConfig != null) {
                randomizerConfig.RandomizeFeatures(features);
            } else {
                ApplyOldFeatureSetup(features);
            }

            if (TryGetComponent(out CharacterDefaultClothes clothes)) {
                clothes.AddTo(features).Forget();
            }
        }

        void ApplyOldFeatureSetup(BodyFeatures features) {
            if (configSO != null) {
                features.ShapesFeature = new BlendShapesFeature(BlendShapeUtils.RandomizeWithParams(configSO));
            }

            foreach (ARAssetReference feature in featurePool) {
                if (feature is {IsSet: true} && Random.Range(0, 100) <= featurePoolChance) {
                    features.AddAdditionalFeature(new MeshFeature(feature));
                }
            }

#pragma warning disable CS0618
            if (underwear.IsSet) {
                features.AddAdditionalFeature(new MeshFeature(underwear));
            }
#pragma warning restore CS0618

            
            hairPool.RemoveAll(h => h is not {IsSet: true});
            if (!hairPool.IsNullOrEmpty()) {
                ARAssetReference arAssetReference = hairPool[Random.Range(0, hairPool.Count)];
                if (arAssetReference.IsSet) {
                    features.Hair = new MeshFeature(arAssetReference);
                }
            }
            
            beardPool.RemoveAll(b => b is not {IsSet: true});
            if (!beardPool.IsNullOrEmpty()) {
                ARAssetReference arAssetReference = beardPool[Random.Range(0, beardPool.Count)];
                if (arAssetReference.IsSet) {
                    features.Beard = new MeshFeature(arAssetReference);
                }
            }

            
            hairColorPool.RemoveAll(h => h == null);
            if (!hairColorPool.IsNullOrEmpty()) {
                HairConfig uniformSelectSafe = RandomUtil.UniformSelectSafe(hairColorPool);
                features.ChangeHairColor(uniformSelectSafe);
                features.ChangeBeardColor(uniformSelectSafe);
            }
        }
    }
}