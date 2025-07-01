using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials {
    public class TutorialConfig : ScriptableObject, ILocalizedSO {
        [FoldoutGroup("Combat")]
        [FoldoutGroup("Combat/Parry")] [UnityEngine.Scripting.Preserve, Tags(TagsCategory.EnemyType)] public string parryableEnemyTag;
        [FoldoutGroup("Combat/Parry")] [UnityEngine.Scripting.Preserve] public VideoTutorial parry;
        [FoldoutGroup("Combat/EnemyStamina")] [UnityEngine.Scripting.Preserve] public VideoTutorial enemyStamina;
        
        [FoldoutGroup("Development")]
        [FoldoutGroup("Development/BonfireSetUp")] [UnityEngine.Scripting.Preserve] public VideoTutorial bonfire;
        [FoldoutGroup("Development/BonfireBoost")] [UnityEngine.Scripting.Preserve] public VideoTutorial bonfireBoost;
        
        [FoldoutGroup("Development/SoulFragment1")] [UnityEngine.Scripting.Preserve] public VideoTutorial soulFragment1;
        [FoldoutGroup("Development/WyrdPower")] [UnityEngine.Scripting.Preserve] public VideoTutorial wyrdPower;
        
        [FoldoutGroup("Development/RedDeathSkillTree"), SerializeField] public GraphicTutorial redDeathSkillTree;
        [FoldoutGroup("Development/FirstMemoryShard"), SerializeField] public GraphicTutorial firstMemoryShardAcquire;
        [FoldoutGroup("Development/FirstWyrdWhisper"), SerializeField] public GraphicTutorial firstWyrdWhisperAcquire;
        
        [FoldoutGroup("Tools")]
        [FoldoutGroup("Tools/Spyglass")] [UnityEngine.Scripting.Preserve] public VideoTutorial spyglass;
        [FoldoutGroup("Tools/FishingRod")] [UnityEngine.Scripting.Preserve] public VideoTutorial fishingRod;
        [FoldoutGroup("Tools/FishingMiniGame")] [UnityEngine.Scripting.Preserve] public VideoTutorial fishing;
        [FoldoutGroup("Tools/Sketchbook")] [UnityEngine.Scripting.Preserve] public VideoTutorial sketchbook;
        
        [FoldoutGroup("Horse")]
        [FoldoutGroup("Horse/PC"), SerializeField] GraphicTutorial horsePC;
        [FoldoutGroup("Horse/Console"), SerializeField] GraphicTutorial horseConsole;
        [FoldoutGroup("Horse/DLC"), SerializeField] public GraphicTutorial horseArmorDlc;

        public GraphicTutorial Horse => RewiredHelper.IsGamepad ? horseConsole : horsePC;

        public IEnumerable<VideoTutorial> AllVideoTutorials() {
            yield return parry;
            yield return enemyStamina;
            yield return bonfire;
            yield return bonfireBoost;
            yield return soulFragment1;
            yield return wyrdPower;
            yield return spyglass;
            yield return fishingRod;
            yield return fishing;
            yield return sketchbook;
        }
        
        public IEnumerable<GraphicTutorial> AllGraphicTutorials() {
            yield return redDeathSkillTree;
            yield return firstMemoryShardAcquire;
            yield return firstWyrdWhisperAcquire;
            yield return Horse;
            yield return horseArmorDlc;
        }

        [Serializable, HideLabel]
        public struct TextTutorial : ITutorialDataOwner, IEquatable<TextTutorial> {
            public SequenceKey sequenceKey;
            [LocStringCategory(Category.Tutorial)]
            public LocString title;
            [LocStringCategory(Category.Tutorial)]
            public LocString text;

            public bool Equals(TextTutorial other) => Equals(title, other.title) && Equals(text, other.text);
            public override bool Equals(object obj) => obj is TextTutorial other && Equals(other);

            public override int GetHashCode() {
                unchecked {
                    return ((title != null ? title.GetHashCode() : 0) * 397) ^ (text != null ? text.GetHashCode() : 0);
                }
            }

            public static bool operator ==(TextTutorial left, TextTutorial right) => left.Equals(right);
            public static bool operator !=(TextTutorial left, TextTutorial right) => !left.Equals(right);
        }
        
        [Serializable, HideLabel]
        public struct GraphicTutorial  : ITutorialDataOwner, IEquatable<GraphicTutorial> {
            public SequenceKey sequenceKey;
            [LocStringCategory(Category.Tutorial)]
            public LocString title;
            [LocStringCategory(Category.Tutorial)]
            public LocString text;
            [UIAssetReference] 
            public ShareableSpriteReference icon;
            [UIAssetReference] 
            public ShareableSpriteReference graphic;
            [RichEnumExtends(typeof(KeyBindings))]
            public List<RichEnumReference> keyBinding;

            public string GetTranslatedText() {
                if (keyBinding == null || keyBinding.Count == 0) {
                    return text.Translate();
                } else {
                    var keys = new object[keyBinding.Count];
                    for (int i = 0; i < keyBinding.Count; i++) {
                        keys[i] = UIUtils.Key(keyBinding[i].EnumAs<KeyBindings>());
                    }
                    return text.Translate(keys);
                }
            }
            
            public bool Equals(GraphicTutorial other) {
                return Equals(title, other.title) && Equals(text, other.text) && Equals(graphic, other.graphic) && Equals(keyBinding, other.keyBinding);
            }

            public override bool Equals(object obj) {
                return obj is GraphicTutorial other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = (title != null ? title.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (text != null ? text.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (graphic != null ? graphic.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (keyBinding != null ? keyBinding.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(GraphicTutorial left, GraphicTutorial right) => left.Equals(right);
            public static bool operator !=(GraphicTutorial left, GraphicTutorial right) => !left.Equals(right);
        }
        
        [Serializable, HideLabel]
        public struct VideoTutorial : ITutorialDataOwner, IEquatable<VideoTutorial> {
            public SequenceKey sequenceKey;
            [LocStringCategory(Category.Tutorial)]
            public LocString title;
            [LocStringCategory(Category.Tutorial)]
            public LocString text;
            [UIAssetReference] 
            public ShareableSpriteReference icon;
            public LoadingHandle video;
            [RichEnumExtends(typeof(KeyBindings))]
            public List<RichEnumReference> keyBinding;
            
            public string GetTranslatedText() {
                if (keyBinding == null || keyBinding.Count == 0) {
                    return text.Translate();
                } else {
                    var keys = new object[keyBinding.Count];
                    for (int i = 0; i < keyBinding.Count; i++) {
                        keys[i] = UIUtils.Key(keyBinding[i].EnumAs<KeyBindings>());
                    }
                    return text.Translate(keys);
                }
            }

            public bool Equals(VideoTutorial other) {
                return Equals(title, other.title) && Equals(text, other.text) && Equals(video, other.video) && Equals(keyBinding, other.keyBinding);
            }

            public override bool Equals(object obj) {
                return obj is VideoTutorial other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = (title != null ? title.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (text != null ? text.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (video != null ? video.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (keyBinding != null ? keyBinding.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(VideoTutorial left, VideoTutorial right) => left.Equals(right);
            public static bool operator !=(VideoTutorial left, VideoTutorial right) => !left.Equals(right);
        }
    }

    public interface ITutorialDataOwner { }
}