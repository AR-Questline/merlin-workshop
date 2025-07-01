using System.Runtime.CompilerServices;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.LowLevel.Collections;
using Unity.Mathematics;

namespace Awaken.TG.Main.AI.States {
    public class NpcDangerTracker {
        public const float EnviroDangerLifetime = 10f;
        public const float NpcDangerLifetime = 10f;
        public const float EventDangerLifetime = 10f;
        public const float CombatDangerLifetime = 10f;
        public const float HeroDangerLifetime = 10f;
        
        public static int FleeingFromHero { get; private set; }

        public bool InEnviroDanger { get; private set; }
        public bool InDirectDanger { get; private set; }
        public bool FearfulsInDanger { get; private set; }
        public bool InAnyDanger => InEnviroDanger || InDirectDanger;

        NpcElement _owner;
        EnviroDanger _enviroDanger;
        DirectHeroDanger _directHeroDanger;
        UnsafePinnableList<DirectNpcDanger> _directNpcDangers;
        
        IEventListener _characterDangerListener;

        public static class Events {
            public static readonly Event<NpcElement, DirectDangerData> CharacterDangerNearby = new(nameof(CharacterDangerNearby));
        }

        public NpcDangerTracker(NpcElement npc) {
            _owner = npc;
            InEnviroDanger = false;
            InDirectDanger = false;
            _enviroDanger = default;
            _directHeroDanger = default;
            _directNpcDangers = new UnsafePinnableList<DirectNpcDanger>();
            _characterDangerListener = null;
        }

        public void OnStart() {
            _characterDangerListener = _owner.ListenTo(Events.CharacterDangerNearby, OnCharacterDanger);
        }

        public void OnStop() {
            World.EventSystem.DisposeListener(ref _characterDangerListener);

            _enviroDanger = default;
            _directNpcDangers.Clear();
            if (_directHeroDanger.hero != null) {
                RemoveHeroDanger();
            }

            InEnviroDanger = false;
            InDirectDanger = false;
        }
        
        public void Update(bool enviroDanger, bool fearfulsInDanger, float deltaTime, NpcElement npc) {
            UpdateEnviroDanger(enviroDanger, deltaTime);
            UpdateDirectDanger(npc, deltaTime);
            FearfulsInDanger = InAnyDanger && fearfulsInDanger;
        }

        public void OnPeasantNoticedCrime(CrimeArchetype crime) {
            if (_characterDangerListener != null) {
                OnCharacterDanger(new DirectDangerData(_owner, Hero.Current, crime.SimpleCrimeType));
            }
        }

        void UpdateEnviroDanger(bool enviroDanger, float deltaTime) {
            InEnviroDanger = UpdateDanger(ref _enviroDanger.lifetime, enviroDanger, deltaTime, EnviroDangerLifetime);
        }
        
        void UpdateDirectDanger(NpcElement npc, float deltaTime) {
            InDirectDanger = false; 
                
            if (_directHeroDanger.hero is { } hero) {
                bool isDanger = hero.IsAlive && hero.NpcChunk == npc.NpcChunk;
                if (UpdateDanger(ref _directHeroDanger.lifetime, isDanger, deltaTime, NpcDangerLifetime)) {
                    InDirectDanger = true;
                } else {
                    RemoveHeroDanger();
                }
            }

            for (int i = _directNpcDangers.Count - 1; i >= 0; i--) {
                ref var danger = ref _directNpcDangers[i];
                var isDanger = danger.npc.IsAlive && danger.npc.NpcChunk == npc.NpcChunk;
                if (UpdateDanger(ref danger.lifetime, isDanger, deltaTime, NpcDangerLifetime)) {
                    InDirectDanger = true;
                } else {
                    _directNpcDangers.SwapBackRemove(i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UpdateDanger(ref float lifetime, bool inDanger, float deltaTime, float maxLifetime) {
            lifetime = inDanger ? maxLifetime : math.max(0, lifetime - deltaTime);
            return lifetime > 0;
        }

        void OnCharacterDanger(DirectDangerData data) {
            if (data.attacker is NpcElement npc) {
                AddNpcDanger(npc);
            } else if (data.attacker is Hero hero) {
                if (_directHeroDanger.hero == null) {
                    AddHeroDanger(hero, data);
                }
            }
        }

        void AddNpcDanger(NpcElement npc) {
            foreach (ref var danger in _directNpcDangers) {
                if (danger.npc == npc) {
                    danger.lifetime = NpcDangerLifetime;
                    return;
                }
            }
            _directNpcDangers.Add(new DirectNpcDanger(npc));
        }

        void AddHeroDanger(Hero hero, in DirectDangerData data) {
            _directHeroDanger = new DirectHeroDanger(hero, data.crimeType);
            FleeingFromHero++;
        }
        
        void RemoveHeroDanger() {
            _directHeroDanger = default;
            FleeingFromHero--;
        }
        
        struct DirectNpcDanger {
            public NpcElement npc;
            public float lifetime;

            public DirectNpcDanger(NpcElement npc) {
                this.npc = npc;
                lifetime = NpcDangerLifetime;
            }
        }

        struct DirectHeroDanger {
            public Hero hero;
            public float lifetime;
            [UnityEngine.Scripting.Preserve] SimpleCrimeType crimeType;

            public DirectHeroDanger(Hero hero, SimpleCrimeType crimeType = SimpleCrimeType.None) {
                this.hero = hero;
                lifetime = HeroDangerLifetime;
                this.crimeType = crimeType;
            }
        }

        struct EnviroDanger {
            public float lifetime;
        }
        
        public struct DirectDangerData {
            public NpcElement receiver;
            public ICharacter attacker;
            public SimpleCrimeType crimeType;
            
            public DirectDangerData(NpcElement receiver, ICharacter attacker, SimpleCrimeType crimeType = SimpleCrimeType.None) {
                this.receiver = receiver;
                this.attacker = attacker;
                this.crimeType = crimeType;
            }
        }
    }
}