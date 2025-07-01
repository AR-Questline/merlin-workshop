using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Audio;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using ItemData = Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item.ItemData;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public static class StoryUtils {
        
        /// <summary>
        /// Stops story execution until user input is detected.
        /// If there is audio played, it gets stopped on user's input.
        /// </summary>
        public static StepResult WaitForInput(Story api) {
            StepResult result = new();
            WaitForInput(result, api).Forget();
            return result;
        }

        static async UniTaskVoid WaitForInput(StepResult result, Story api) {
            CancellationTokenSource cancellationToken = new();
            bool success = await AsyncUtil.DelayTime(api, 0.25f, ignoreTimeScale: true, source: cancellationToken);
            if (success) {
                await AsyncUtil.WaitForInput(api, cancellationToken);
            }
            result.Complete();
        }

        static CancellationTokenSource GetStoryCancellationSource(Story api) {
            CancellationTokenSource cancellationSource = new();
            var listener = api.ListenTo(Model.Events.BeforeDiscarded, () => cancellationSource.Cancel());
            EnsureCleanupListener(cancellationSource, listener).Forget();
            return cancellationSource;
        }

        static async UniTaskVoid EnsureCleanupListener(CancellationTokenSource token, IEventListener listener) {
            await AsyncUtil.UntilCancelled(token.Token);
            World.EventSystem.RemoveListener(listener);
        }

        public static async UniTaskVoid WaitDialogueLine(StepResult result, Story api, string text, VoiceOversEventEmitter emitter, NonSpatialVoiceOvers nonSpatialVO, StoryAnimationData storyAnimationData, int? cutDuration = null) {
            if (emitter == null && nonSpatialVO == null && !api.HasElement<HeroDialogueInvolvement>()) throw new InvalidOperationException("Not enough data provided for correct functioning of method!");

            CancellationTokenSource cancellationSource = GetStoryCancellationSource(api);
            storyAnimationData?.StartAnimation(cancellationSource.Token).Forget();
            
            bool hasHeroInvolvement = api.HasElement<HeroDialogueInvolvement>();
            bool waitOnlyForInput = hasHeroInvolvement && !World.Only<DialogueAutoAdvance>().Enabled;

            UniTask voTask;

            if (emitter != null && !emitter.EventReference.IsNull) {
                // Play VO
                //int eventDuration;
                // if (RuntimeManager.TryGetEventDescription(emitter.EventReference, out var eventDescription, emitter)) {
                //     eventDescription.getLength(out eventDuration);
                // } else {
                //     eventDuration = 0;
                // }
                // if (eventDuration <= 1050f) {
                //     eventDuration = math.max(eventDuration, GetTextReadTime(text));
                // } else if (cutDuration.HasValue) {
                //     eventDuration = math.clamp(eventDuration - cutDuration.Value, 1, eventDuration);
                // }
                //voTask = WaitForClipEnd(eventDuration, cancellationSource, !waitOnlyForInput);
            } else {
                voTask = nonSpatialVO?.Play(cancellationSource, GetTextReadTime(text), cutDuration, cancelTokenOnEnd: !waitOnlyForInput) ?? UniTask.CompletedTask;
            }

            // If Hero is involved, allow skipping dialogue lines
            if (hasHeroInvolvement) {
                api.AddElement(new HeroInvolvedInputListener(cancellationSource));

                if (waitOnlyForInput) {
                    //ShowSkipDialoguePromptAfterVO(api, voTask).Forget();
                }
            }

            //var emitterActiveEventInstance = emitter?.EventInstance;
            // Wait for end of text or skip
            await AsyncUtil.UntilCancelled(cancellationSource.Token);

            if (waitOnlyForInput && !api.HasBeenDiscarded) {
                var vDialogue = api.View<VDialogue>();
                if (vDialogue) {
                    vDialogue.HideSkipDialoguePrompt();
                }
            }

            // Finalize audio and animation players
            // if (emitter != null && emitter.EventInstance.handle == emitterActiveEventInstance.Value.handle) {
            //     emitter.Stop();
            // }
            // Continue story
            result.Complete();
        }
        
        static async UniTask WaitForClipEnd(int clipLength, CancellationTokenSource token, bool cancelTokenOnEnd) {
            await UniTask.Delay(clipLength);
            if (cancelTokenOnEnd && token.Token.CanBeCanceled) {
                token.Cancel();
            }
        }

        static async UniTaskVoid ShowSkipDialoguePromptAfterVO(Story api, UniTask voTask) {
            await voTask;
            if (api.HasBeenDiscarded || api.HasElement<Choice>()) {
                return;
            }
            
            var vDialogue = api.View<VDialogue>();
            if (vDialogue) {
                vDialogue.ShowSkipDialoguePrompt();
            }
        }

        /// <summary>
        /// Complete StepResult after time needed to read the text.
        /// If player is involved, their input also completes StepResult.
        /// </summary>
        public static async UniTaskVoid CompleteWhenReadOrInput(Story api, StepResult result, string text, StoryAnimationData storyAnimationData = null) {
            CancellationTokenSource cancellationSource = GetStoryCancellationSource(api);

            int animationLength = 0;
            if (storyAnimationData != null && storyAnimationData.animationClip != null) {
                animationLength = (int)(storyAnimationData.animationClip.length * 1000);
                storyAnimationData.StartAnimation(cancellationSource.Token).Forget();
            }
            CancelWhenRead(text, cancellationSource, animationLength).Forget();
            
            if (api.HasElement<HeroDialogueInvolvement>()) {
                api.AddElement(new HeroInvolvedInputListener(cancellationSource));
            }
            await AsyncUtil.UntilCancelled(cancellationSource.Token);
            
            result.Complete();
        }
        
        static async UniTaskVoid CancelWhenRead(string text, CancellationTokenSource token, int animationLength) {
            int readTime = GetTextReadTime(text);
            await UniTask.Delay(Mathf.Max(readTime, animationLength));
            if (token.Token.CanBeCanceled) {
                token.Cancel();
            }
        }
        
        /// <summary>
        /// Get wait time from text length
        /// https://ux.stackexchange.com/a/61976
        /// </summary>
        /// <returns>Reading time in milliseconds</returns>
        static int GetTextReadTime(string text) {
            const int Wpm = 180; // words per minute
            const int WordLength = 5; // standardized number of chars in calculable word
            
            int words = text.Length / WordLength;
            float wordsTime = words / (float)Wpm * 60 * 1000; // total time in milliseconds

            const int Delay = 1500; // milliseconds before user starts reading the notification
            const int Extra = 500; // extra time

            return Delay + Mathf.CeilToInt(wordsTime) + Extra;
        }

        public static async UniTaskVoid CompleteWhenGestureEnds(Story api, StepResult result, StoryAnimationData storyAnimationData = null) {
            CancellationTokenSource cancellationSource = GetStoryCancellationSource(api);
            int animationLength = 0;
            if (storyAnimationData != null && storyAnimationData.animationClip != null) {
                animationLength = (int)(storyAnimationData.animationClip.length * 1000);
                storyAnimationData.StartAnimation(cancellationSource.Token).Forget();
            }
            await UniTask.Delay(animationLength, cancellationToken: cancellationSource.Token);
            result?.Complete();
        }

        /// <summary>
        /// Verifies whether hero in story has enough items.
        /// There are 2 categories of items: identified by ItemTemplate and by tags.
        /// IsNegative determines if we should invert quantity (if we want to change item quantity by -5, we need to check if we have 5, not -5) 
        /// </summary>
        public static bool HasRequiredItems(IEnumerable<ItemSpawningData> pairs, string[] tags, int taggedQuantity, bool isNegative = false, bool onlyStolen = false, bool onlyEquipped = false, string[] forbiddenTags = null) {
            Hero hero = Hero.Current;
            bool taggedItemsDoesNotHaveForbiddenTags;
            
            // check by tags
            taggedQuantity = taggedQuantity * (isNegative ? -1 : 1);
            bool hasTaggedItems;
            if (tags == null || !tags.Any()) {
                hasTaggedItems = true;
                taggedItemsDoesNotHaveForbiddenTags = hero.Inventory.Items.Where(item => !item.HiddenOnUI && TagUtils.DoesNotHaveForbiddenTags(item, forbiddenTags)).Sum(static item => item.Quantity) >= taggedQuantity;
            } else {
                List<Item> qualifiedItems = hero.Inventory.Items.Where(item => !item.HiddenOnUI && (!onlyStolen || item.IsStolen) && (!onlyEquipped || item.IsEquipped) && TagUtils.HasRequiredTags(item, tags)).ToList();
                hasTaggedItems = qualifiedItems.Sum(static item => item.Quantity) >= taggedQuantity;

                if (forbiddenTags == null || !forbiddenTags.Any()) {
                    taggedItemsDoesNotHaveForbiddenTags = true;
                } else {
                    taggedItemsDoesNotHaveForbiddenTags = qualifiedItems.Where(item => TagUtils.DoesNotHaveForbiddenTags(item, forbiddenTags)).Sum(static item => item.Quantity) >= taggedQuantity;
                }
            }
            
            // check by templates
            bool hasSpecifiedItems = pairs == null || pairs.All(pair => {
                ItemTemplate itemTemplate = pair.ItemTemplate(null);
                if (itemTemplate == null) {
                    return true;
                }
                int quantity = pair.quantity * (isNegative ? -1 : 1);
                int heroHasQuantity = hero.Inventory.Items.Where(i => (!onlyStolen || i.IsStolen) && (!onlyEquipped || i.IsEquipped) && i.Template == itemTemplate).Sum(i => i.Quantity);
                return heroHasQuantity >= quantity;
            });
            
            return hasTaggedItems && hasSpecifiedItems && taggedItemsDoesNotHaveForbiddenTags;
        }

        /// <summary>
        /// Construct WorldMemory context using both string ids and Context objects.
        /// </summary>
        public static string[] Context([CanBeNull] Story story, ICollection<string> stringContexts, ICollection<Context> contexts) {
            stringContexts ??= Array.Empty<string>();
            return stringContexts.Concat(contexts.Select(c => c.ToContextID(story))).ToArray();
        }
        
        /// <summary>
        /// ChoiceConfig extension to allow adding real Sprites to choices. 
        /// </summary>
        public static ChoiceConfig WithSpriteIcon(this ChoiceConfig config, ShareableSpriteReference iconReference) {
            config.AssignIconReference(iconReference);
            return config;
        }

        public static ICharacter FindCharacter(Story api, Actor actor, bool allowDefault = true) {
            if (actor == DefinedActor.Hero.Retrieve()) {
                return Hero.Current;
            }
            
            NpcElement npc = null;
            foreach (var locationRef in api.Locations) {
                var npcElement = TryGetNpcElement(api, locationRef.Get());
                if (npcElement != null && npcElement.Actor == actor) {
                    npc = npcElement;
                    break;
                }
            }

            if (npc != null) {
                return npc;
            } else {
                return allowDefault ? TryGetNpcElement(api, api.FocusedLocation) : null;
            }
        }
        
        public static IWithActor FindIWithActor(Story api, Actor actor, bool allowDefault = true) {
            if (actor == DefinedActor.Hero.Retrieve()) {
                return Hero.Current;
            }

            if (actor.IsFake) {
                int i = 0;
                foreach (var location in api.Locations) {
                    var iWithActor = TryGetWithActor(api, location.Get());
                    if (iWithActor != null) {
                        if (i == actor.FakeIndex) {
                            return iWithActor;
                        }

                        i++;
                    }
                }
                return allowDefault ? TryGetWithActor(api, api.FocusedLocation) : null;
            }

            ILocationElementWithActor withActor = null;
            foreach (var locationRef in api.Locations) {
                var iWithActor = TryGetWithActor(api, locationRef.Get());
                if (iWithActor != null && iWithActor.Actor == actor) {
                    withActor = iWithActor;
                    break;
                }
            }

            if (withActor != null) {
                return withActor;
            } else {
                return allowDefault ? TryGetWithActor(api, api.FocusedLocation) : null;
            }
        }
        
        static NpcElement TryGetNpcElement(Story api, Location location) {
            if (location == null) {
                return null;
            }
            if (location.HasBeenDiscarded) {
                Log.Important?.Error($"{LogUtils.GetDebugName(api)} - Trying to get a NpcElement from discarded location: {LogUtils.GetDebugName(location)}");
                return null;
            }
            return location.TryGetElement<NpcElement>();
        }
        
        static ILocationElementWithActor TryGetWithActor(Story api, Location location) {
            if (location == null) {
                return null;
            }
            if (location.HasBeenDiscarded) {
                Log.Important?.Error($"{LogUtils.GetDebugName(api)} - Trying to get an Actor from discarded location: {LogUtils.GetDebugName(location)}");
                return null;
            }
            return location.TryGetElement<ILocationElementWithActor>();
        }

        public static IGrounded FindIGrounded(Story api, Actor actor, bool allowDefault = true) {
            var withActor = FindIWithActor(api, actor, allowDefault);
            return withActor switch {
                NpcElement npc => npc,
                ILocationElementWithActor locationElementWithActor => locationElementWithActor.ParentModel,
                Hero hero => hero,
                _ => null
            };
        }

        public static void AddTextLinkHandler(View view, GameObject text) {
            var handler = text.GetComponent<TextLinkHandler>();
            if (handler == null) {
                handler = text.AddComponent<TextLinkHandler>();
            }
            handler.Attach(World.Services, view.GenericTarget, view);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void SendVgEvent(Story api, VSCustomEvent action, params object[] args) {
            api.FocusedLocation?.TriggerVisualScriptingEvent(action, args);
        } 
        
        public static void EndStory(Story api, bool stopInvolvingHero = true, bool withInterrupt = false) {
            Location locationToSayGoodbye = null;
            var story = api as Story;
            story?.SetIsEnding(true);
            if (withInterrupt) {
                story?.SetInterrupted();
            } else {
                locationToSayGoodbye = api.Locations.Select(weakRef => weakRef.Get()).TryGetOnly(loc => loc != null);
                if (locationToSayGoodbye is { HasBeenDiscarded: true }) {
                    locationToSayGoodbye = null;
                }
            }
            
            // --- Stop involving Hero
            if (stopInvolvingHero) {
                api.RemoveElementsOfType<HeroDialogueInvolvement>();
                var view = api.View<VDialogue>();
                if (view != null) {
                    view.ShowOnlyText();
                }
            }

            // --- Finish Story
            story?.DropChildren();
            api.Discard();
            story?.SetIsEnding(false);
            locationToSayGoodbye?.TryGetElement<NpcElement>()?.TryGetElement<BarkElement>()?.TrySayGoodbyeOnStoryEnd(api);
        }
        
        [UsedImplicitly]
        public static Story TryStartStory(StoryBookmark bookmark) {
            if (bookmark != null && bookmark.IsValid) {
                var config = StoryConfig.Base(bookmark, typeof(VDialogue));
                return Story.StartStory(config);
            } else {
                return null;
            }
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static Story TryStartLocationStory(StoryBookmark bookmark, Location location) {
            if (bookmark != null && bookmark.IsValid && location != null) {
                var config = StoryConfig.Location(location, bookmark, typeof(VDialogue));
                return Story.StartStory(config);
            } else {
                return null;
            }
        }
        
        public static void AnnounceGettingStat(StatType stat, int statChangeValue, IModel relatedModel, bool isHeroTarget = false) {
            if (statChangeValue == 0 || stat == CurrencyStatType.Wealth || !isHeroTarget) {
                return;
            }
                
            if (stat is ProfStatType profStatType) {
                Stat currentStatValue = profStatType.RetrieveFrom(Hero.Current);
                var proficiencyData = new ProficiencyData(profStatType, currentStatValue.ModifiedInt);
                AdvancedNotificationBuffer.Push<ProficiencyNotificationBuffer>(new ProficiencyNotification(proficiencyData));
                
                return; 
            }
            
            char changeSign = statChangeValue > 0 ? '+' : ' ';
            var statData = new ItemData(stat.DisplayName, statChangeValue, ARColor.MainGrey, changeSign);
            AdvancedNotificationBuffer.Push<ItemNotificationBuffer>(new ItemNotification(statData));
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool TryGetFactionTemplate(Story api, LocationReference locationRef, out FactionTemplate factionTemplate) {
            factionTemplate = locationRef.FirstOrDefault(api)?.TryGetElement<IWithFaction>()?.Faction.Template;
            return factionTemplate != null;
        }
        public static bool TryGetCrimeOwnerTemplate(Story api, LocationReference locationRef, out CrimeOwnerTemplate crimeOwner) {
            crimeOwner = locationRef.FirstOrDefault(api)?.TryGetElement<NpcElement>()?.GetCurrentCrimeOwnersFor(CrimeArchetype.None).PrimaryOwner;
            return crimeOwner != null;
        }
        
        public static IEnumerable<Location> MatchActorLocations(Actor actor) {
            foreach (var withActor in World.All<IWithActor>()) {
                if (withActor.Actor == actor && withActor is Element { GenericParentModel: Location loc }) {
                    yield return loc;
                }
            }
        }
    }
}
