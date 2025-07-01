using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Api {
    /// <summary>
    /// An enum used to refer to various models taking part in a story -
    /// the hero representing the main character, the place we're in, etc.
    /// </summary>
    public class StoryRole : RichEnum {

        // === Roles

        public static readonly StoryRole
            Hero = new(nameof(Hero), s => Heroes.Hero.Current),
            LocationShop = new(nameof(LocationShop), s => s?.FocusedLocation?.TryGetElement<Shop>(),
                (s, locRef) => locRef.MatchingLocations(s).Select(l => l.TryGetElement<Shop>())),
            LocationNpc = new(nameof(LocationNpc), s => s?.FocusedLocation?.TryGetElement<NpcElement>(),
                (s, locRef) => locRef.MatchingLocations(s).Select(l => l.TryGetElement<NpcElement>()));

        // === Role properties

        protected delegate IEnumerable<Model> AdvancedGetterType(Story api, LocationReference locRef);

        readonly Func<Story, Model> _getter;
        readonly AdvancedGetterType _advancedGetter;

        // === Constructor

        protected StoryRole(string id, Func<Story, Model> getter, AdvancedGetterType advancedGetter = null) : base(enumName: id) {
            _getter = getter;
            _advancedGetter = advancedGetter;
        }

        // === Static retrieval

        public static StoryRole DefaultForStat(StatType stat, StoryRoleTarget target = StoryRoleTarget.Hero) {
            return stat switch {
                MerchantStatType => target == StoryRoleTarget.Hero ? Hero : LocationShop,
                CharacterStatType or AliveStatType => target == StoryRoleTarget.Hero ? Hero : LocationNpc,
                NpcStatType => LocationNpc,
                _ => Hero
            };
        }

        // === Usage

        public T RetrieveFrom<T>([CanBeNull] Story story) where T : class, IModel {
            return _getter(story) as T;
        }        
        
        public IEnumerable<T> RetrieveFrom<T>(Story story, LocationReference locationRef) where T : class, IModel {
            if (_advancedGetter != null && locationRef.IsSet) {
                foreach (var model in _advancedGetter(story, locationRef).OfType<T>().WhereNotNull()) {
                    yield return model;
                }
            } else {
                yield return _getter(story) as T;
            }
        }
    }
}
