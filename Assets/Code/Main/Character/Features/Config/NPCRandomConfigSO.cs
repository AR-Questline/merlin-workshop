using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Character.Features.Config {
    [CreateAssetMenu(menuName = "TG/Character/NPC Randomizer Config")]
    public class NPCRandomConfigSO : ScriptableObject {
        [SerializeField] BlendShapeGroupSO blendShapeConfig;
        [SerializeField] public Color[] skinTints = Array.Empty<Color>();
        [SerializeField] List<Color> eyeTints = new();
        [Space]
        [SerializeField, PrefabAssetReference] List<ShareableARAssetReference> featurePool = new();
        [SerializeField] bool getExactlyFeatureCapAmount;
        [SerializeField, MinMaxSlider(0, 10, true)] Vector2Int featureCap = new(1, 3);
        [SerializeField, Range(0, 100), HideIf(nameof(getExactlyFeatureCapAmount))] float featurePoolChance = 30;
        [Space]
        [SerializeField] List<HairConfig> hairColorPool = new();
        [SerializeField, PrefabAssetReference] List<ShareableARAssetReference> hairPool = new();
        [SerializeField, PrefabAssetReference] List<ShareableARAssetReference> beardPool = new();
        [Title("Textures")]
        [SerializeField] List<BodyConfig> bodyTexturesConfigPool = new();
        [SerializeField, TextureAssetReference] List<ShareableARAssetReference> eyebrowTexturePool = new();
        [SerializeField, TextureAssetReference] List<ShareableARAssetReference> teethTexturePool = new();
        [SerializeField] TattooType tattooType = TattooType.None;
        
        public void RandomizeFeatures(BodyFeatures features) {
            // Remove nulls and not set shareable asset references from all collections
            featurePool.RemoveAll(f => f is not {IsSet: true});
            hairPool.RemoveAll(h => h is not {IsSet: true});
            beardPool.RemoveAll(b => b is not {IsSet: true});
            bodyTexturesConfigPool.RemoveAll(b => b.Invalid);
            eyebrowTexturePool.RemoveAll(e => e is not {IsSet: true});
            teethTexturePool.RemoveAll(t => t is not {IsSet: true});
            
            
            if (blendShapeConfig != null) {
                features.ShapesFeature = new BlendShapesFeature(blendShapeConfig.CollectBlendshapes());
            }
            
            int featuresCount = Random.Range(featureCap.x, featureCap.y + 1);
            if (getExactlyFeatureCapAmount) {
                foreach (ShareableARAssetReference shareableARAssetReference in RandomUtil.UniformSelectMultiple(featurePool, featuresCount)) {
                    features.AddAdditionalFeature(new MeshFeature(shareableARAssetReference.Get()));
                }
            } else {
                foreach (ShareableARAssetReference feature in featurePool) {
                    if (Random.Range(0, 100) <= featurePoolChance) {
                        features.AddAdditionalFeature(new MeshFeature(feature.Get()));
                        if (--featuresCount == 0) {
                            break;
                        }
                    }
                }
            }
            
            if (hairPool.Count > 0) {
                features.Hair = new MeshFeature(RandomUtil.UniformSelect(hairPool).Get());
            }
            
            if (beardPool.Count > 0) {
                features.Beard = new MeshFeature(RandomUtil.UniformSelect(beardPool).Get());
            }
            
            if (hairColorPool.Count > 0) {
                var characterHairColor = RandomUtil.UniformSelect(hairColorPool);
                features.ChangeHairColor(characterHairColor);
                features.ChangeBeardColor(characterHairColor);
            }
            
            if (skinTints.Length > 0) {
                features.SkinColor = new(RandomUtil.UniformSelect(skinTints));
            }
            
            if (bodyTexturesConfigPool.Count > 0) {
                BodyConfig bodyConfig = RandomUtil.UniformSelect(bodyTexturesConfigPool);
                features.BodySkin = new(bodyConfig);
                features.FaceSkin = new(bodyConfig);
            }
            
            if (eyebrowTexturePool.Count > 0) {
                features.Eyebrows = new(RandomUtil.UniformSelect(eyebrowTexturePool).Get());
            }
            
            if (eyeTints.Count > 0) {
                features.Eyes = new(RandomUtil.UniformSelect(eyeTints));
            }
            
            if (teethTexturePool.Count > 0) {
                features.Teeth = new(RandomUtil.UniformSelect(teethTexturePool).Get());
            }
            
            if (tattooType != TattooType.None) {
                var tattoos = CommonReferences.Get.TattooConfigs;
                foreach (var tattooConfig in tattoos) {
                    if (tattooConfig.type == tattooType) {
                        features.BodyTattoo = new BodyTattooFeature(tattooConfig.Copy());
                        break;
                    }
                }
            }
        }
    }
}