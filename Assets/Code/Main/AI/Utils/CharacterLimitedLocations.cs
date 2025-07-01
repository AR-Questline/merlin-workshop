using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Unity.Collections;

namespace Awaken.TG.Main.AI.Utils {
    public class CharacterLimitedLocations : IDomainBoundService {
        readonly List<CharacterLocations> _registered = new(10);

        public Domain Domain => Domain.Gameplay;

        public static class Events {
            public static readonly Event<ICharacter, int> CharacterLimitedLocationsChanged = new(nameof(CharacterLimitedLocationsChanged));
        }
        
        public bool RemoveOnDomainChange() {
            _registered.Clear();
            return false;
        }

        public void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<ICharacterLimitedLocation>(), this, OnLocationInitialized);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<ICharacterLimitedLocation>(), this, OnLocationDestroyed);
            World.EventSystem.ListenTo(EventSelector.AnySource, Stat.Events.StatChangedBy(HeroStatType.SummonLimit), this, HeroSummonLimitChanged);
            foreach (var location in World.All<ICharacterLimitedLocation>()) {
                Register(location);
            }
        }

        void HeroSummonLimitChanged(Stat.StatChange change) {
            var heroLocations = _registered.FirstOrDefault(x => x.owner is Hero);
            heroLocations?.LimitChanged(change.stat.ModifiedInt);
        }

        void OnLocationDestroyed(Model obj) {
            if (obj is ICharacterLimitedLocation location) {
                Unregister(location);
            }
        }

        void OnLocationInitialized(Model obj) {
            if (obj is ICharacterLimitedLocation location) {
                Register(location);
            }
        }

        void Register(ICharacterLimitedLocation location) {
            if (location.Owner == null) return;
            
            int index = _registered.FindIndex(x => x.Owns(location));
            
            if (index == -1) {
                _registered.Add(new CharacterLocations(location));
                index = _registered.Count - 1;
                location.Owner.ListenTo(Model.Events.BeforeDiscarded, OnCharacterDiscarded, this);
            }
            
            CharacterLocations characterLocations = _registered[index];
            characterLocations.AddLocation(location);
            
            characterLocations.owner.Trigger(Events.CharacterLimitedLocationsChanged, characterLocations.Count);
        }
        
        void Unregister(ICharacterLimitedLocation location) {
            
            int index = _registered.FindIndex(x => x.Owns(location));
            if (index == -1) return;
            
            CharacterLocations characterLocations = _registered[index];
            if (characterLocations.RemovalLock) return;
            
            characterLocations.RemoveLocation(location);
            characterLocations.owner.Trigger(Events.CharacterLimitedLocationsChanged, characterLocations.Count);

            if (characterLocations.IsEmpty) {
                _registered.RemoveAtSwapBack(index);
            }
        }

        void OnCharacterDiscarded(IModel model) {
            ICharacter character = (ICharacter) model;
            for (int i = _registered.Count - 1; i >= 0; i--) {
                if (_registered[i].owner == character) {
                    _registered[i].OwnerDiscarded();
                    _registered.RemoveAtSwapBack(i);
                }
            }
        }

        class CharacterLocations {
            public readonly ICharacter owner;
            readonly CharacterLimitedLocationType _type;
            /// <summary>
            /// Holds the locations that are limited for the character in chronological order
            /// </summary>
            ICharacterLimitedLocation[] _locations;

            int _oldestIndex;
            int _emptyCount;
            

            int OldestIndex {
                get => _oldestIndex;
                set => _oldestIndex = ToIndex(value);
            }
            
            int NewestIndex => ToIndex(OldestIndex + Count - 1);
            
            public int Count => _locations.Length - _emptyCount;
            public bool IsEmpty => _emptyCount == _locations.Length;
            /// <summary>
            /// When we are doing removal operations from within the system we want to prevent the events we send out from triggering removal again
            /// </summary>
            public bool RemovalLock { get; private set; }

            public CharacterLocations(ICharacterLimitedLocation limitedLocation) {
                owner = limitedLocation.Owner;
                _type = limitedLocation.Type;
                
                _emptyCount = limitedLocation.LimitForCharacter(limitedLocation.Owner);
                _locations = new ICharacterLimitedLocation[_emptyCount];
                OldestIndex = 0;
            }
            
            public void LimitChanged(int newLimit) {
                if (newLimit == _locations.Length) return;
                
                RemovalLock = true;
                {
                    ICharacterLimitedLocation[] newLocations = new ICharacterLimitedLocation[newLimit];
                    int amountToMigrate = Math.Min(newLimit, Count);
                    for (int i = 0; i < amountToMigrate; i++) {
                        int index = ToIndex(NewestIndex - i);
                        newLocations[i] = _locations[index];
                        _locations[index] = null;
                    }
                    // destroy any locations that were not migrated
                    for (int i = amountToMigrate; i < Count; i++) {
                        _locations[ToIndex(NewestIndex - i)].Destroy();
                    }

                    _locations = newLocations;
                    _emptyCount = newLimit - amountToMigrate;
                    OldestIndex = 0;
                }
                RemovalLock = false;
            }
            
            public void AddLocation(ICharacterLimitedLocation location) {
                if (_emptyCount == 0) {
                    RemovalLock = true;
                    {
                        _locations[OldestIndex].Destroy();
                        _locations[OldestIndex] = location;
                        ++OldestIndex;
                        Asserts.AreEqual(_emptyCount, 0, "Empty count should be 0 as we just replaced a location");
                        Log.Debug?.Info(owner.ContextID + " +a Result List: empty " + _emptyCount + " oldestIndex " + OldestIndex +
                                        "\n" + string.Join("\n", _locations.Select(l => l?.ID)));
                    }
                    RemovalLock = false;
                } else {
                    int index = ToIndex(OldestIndex + (_locations.Length - _emptyCount--));
                    _locations[index] = location;
                    Log.Debug?.Info(owner.ContextID + " +b Result List: empty " + _emptyCount + " oldestIndex " + OldestIndex + "\n" + string.Join("\n", _locations.Select(l => l?.ID)));
                }
            }
            
            public void RemoveLocation(ICharacterLimitedLocation location) {
                int removalIndex = Array.IndexOf(_locations, location);
                if (removalIndex == -1) {
                    Log.Important?.Error($"Location {location} not found in {owner}'s locations");
                    return;
                }
                
                if (removalIndex == OldestIndex) {
                    _locations[removalIndex] = null;
                    ++OldestIndex;
                    Log.Debug?.Info(owner.ContextID + " -a Result List: empty " + (_emptyCount + 1) + " oldestIndex " + OldestIndex + "\n" + string.Join("\n", _locations.Select(l => l?.ID)));
                } else {
                    // We could do this only upon adding to list when no more "free" spots are left but might not be necessary
                    // Shift the array to the left after the removal point
                    for (int i = 0; i < _locations.Length; i++) {
                        int shiftedIndexOverRemovalPoint = ToIndex(i + removalIndex);
                        int nextToMoveForward = ToIndex(shiftedIndexOverRemovalPoint + 1);
                        
                        if (nextToMoveForward == OldestIndex) break;
                        
                        _locations[shiftedIndexOverRemovalPoint] = _locations[nextToMoveForward];
                        _locations[nextToMoveForward] = null;
                        Log.Debug?.Info(owner.ContextID + " -b Result List: empty " + (_emptyCount + 1) + " oldestIndex " + OldestIndex + "\n" + string.Join("\n", _locations.Select(l => l?.ID)));
                    }
                }
                _emptyCount++;
            }
            
            int ToIndex(int index) => (index + _locations.Length) % _locations.Length;
            
            public void OwnerDiscarded() {
                if (RemovalLock) return;
                if (IsEmpty) return;
                
                RemovalLock = true;
                {
                    for (int i = 0; i < Count; i++) {
                        _locations[ToIndex(OldestIndex + i)].OwnerDiscarded();
                    }
                }
                RemovalLock = false;
            }

            public bool Owns(ICharacterLimitedLocation location) {
                return location.Owner == owner && location.Type == _type;
            }
        }
    }
}