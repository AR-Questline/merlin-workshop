namespace Awaken.Utility {
    /// <summary>
    /// Lookup class for render layer numbers. Prevents magic numbers
    /// in code, but has to be kept up to date with Unity settings.
    /// </summary>
    public class RenderLayers {
        
        /// <summary>
        /// Default layer <br/>
        /// [Unmodifiable] <br/>
        /// </summary>
        public const int Default = 0;
        
        /// <summary>
        /// [Unmodifiable] <br/>
        /// </summary>
        public const int Transparent = 1;
        
        /// <summary>
        /// [Unmodifiable] <br/>
        /// </summary>
        public const int IgnoreRaycast = 2;
        
        /// <summary>
        /// Marks Terrain <br/>
        /// Marks Walkable platforms <br/>
        /// </summary>
        public const int Walkable = 3;
        
        /// <summary>
        /// [Unmodifiable] <br/>
        /// </summary>
        public const int Water = 4;
        
        /// <summary>
        /// [Unmodifiable] <br/>
        /// </summary>
        public const int UI = 5;
        
        /// <summary>
        /// Marks Obstacles <br/>
        /// Marks Location <br/>
        /// Colliders that block player interaction. Except the one with  interactable tag <br/>
        /// </summary>
        public const int Objects = 6;
        
        /// <summary>
        /// Marks PostProcessing <br/>
        /// </summary>
        public const int PostProcessing = 7;
        
        public const int VFX = 8;
        
        /// <summary>
        /// Used for collider for <see cref="Heroes.CharacterSheet.Map.MapMarker"/>
        /// </summary>
        public const int MapMarker = 9;
        
        /// <summary>
        /// Colliders of character triggers (eg. traps) <br/>
        /// </summary>
        public const int TriggerVolumes = 10;
        
        /// <summary>
        /// Marks Objects that are ignored while baking NavMesh <br/>
        /// </summary>
        public const int NavigationIgnoreObj = 11;
        
        /// <summary>
        /// Marks invisible floor on which NavMesh is baked when visible floor is too complex <br/>
        /// </summary>
        public const int NavigationOnlyObjects = 12;

        /// <summary>
        /// Marks terrain on witch vegetation is spawned <br/>
        /// </summary>
        public const int Terrain = 13;

        /// <summary>
        /// Marks terrain on witch vegetation is spawned <br/>
        /// </summary>
        public const int Vegetation = 14;

        /// <summary>
        /// Things invisible for camera that blocks rain <br/>
        /// </summary>
        public const int RainObstacle = 15;
        
        /// <summary>
        /// Triggers that marks that Hero is under roof. Triggers special audio for rain. <br/>
        /// </summary>
        public const int RainTriggerVolume = 16;
        
        /// <summary>
        /// Marks impostors
        /// </summary>
        public const int Impostor = 18;

        /// <summary>
        /// Areas where combat is forbidden.
        /// </summary>
        public const int SafeZone = 19;
        
        /// <summary>
        /// Colliders of Wyrdness <br/>
        /// </summary>
        public const int Wyrdness = 20;
        
        /// <summary>
        /// Colliders of TimeVolumes <br/>
        /// </summary>
        public const int Time = 21;
        
        /// <summary>
        /// Marks NPC Armature <br/>
        /// </summary>
        public const int Ragdolls = 22;
        
        /// <summary>
        /// Colliders of weapons dealing damage <br/>
        /// </summary>
        public const int ProjectileAndAttacks = 23;
        
        /// <summary>
        /// Colliders of damage being dealt <br/>
        /// </summary>
        public const int Hitboxes = 24;
        
        /// <summary>
        /// Colliders of NPCs. Prevents from stepping into NPC. Triggers AI triggers (eg. traps) <br/>
        /// </summary>
        public const int AIs = 25;
        
        /// <summary>
        /// Colliders of AI IdleInteractions <br/>
        /// </summary>
        public const int AIInteractions = 26;
        
        /// <summary>
        /// Colliders of player interactions <br/>
        /// Player is moved on this layer when in NoClip <br/>
        /// </summary>
        public const int PlayerInteractions = 29;

        public const int HLOD = 30;
        /// <summary>
        /// Marks Hero <br/>
        /// </summary>
        public const int Player = 31;

        // == Masks

        public static class Mask {
            /// <inheritdoc cref="RenderLayers.Default"/>
            public const int Default = 1 << RenderLayers.Default;
            /// <inheritdoc cref="RenderLayers.Transparent"/>
            public const int Transparent = 1 << RenderLayers.Transparent;
            /// <inheritdoc cref="RenderLayers.IgnoreRaycast"/>
            public const int IgnoreRaycast = 1 << RenderLayers.IgnoreRaycast;
            /// <inheritdoc cref="RenderLayers.Walkable"/>
            public const int Walkable = 1 << RenderLayers.Walkable;
            /// <inheritdoc cref="RenderLayers.Water"/>
            public const int Water = 1 << RenderLayers.Water;
            /// <inheritdoc cref="RenderLayers.UI"/>
            public const int UI = 1 << RenderLayers.UI;
            /// <inheritdoc cref="RenderLayers.Objects"/>
            public const int Objects = 1 << RenderLayers.Objects;
            /// <inheritdoc cref="RenderLayers.PostProcessing"/>
            public const int PostProcessing = 1 << RenderLayers.PostProcessing;
            /// <inheritdoc cref="RenderLayers.VFX"/>
            public const int VFX = 1 << RenderLayers.VFX;
            /// <inheritdoc cref="RenderLayers.MapMarker"/>
            public const int MapMarker = 1 << RenderLayers.MapMarker;
            /// <inheritdoc cref="RenderLayers.TriggerVolumes"/>
            public const int TriggerVolumes = 1 << RenderLayers.TriggerVolumes;
            /// <inheritdoc cref="RenderLayers.NavigationIgnoreObj"/>
            public const int NavigationIgnoreObj = 1 << RenderLayers.NavigationIgnoreObj;
            /// <inheritdoc cref="RenderLayers.NavigationOnlyObjects"/>
            public const int NavigationOnlyObjects = 1 << RenderLayers.NavigationOnlyObjects;
            /// <inheritdoc cref="RenderLayers.Terrain"/>
            public const int Terrain = 1 << RenderLayers.Terrain;
            /// <inheritdoc cref="RenderLayers.Terrain"/>
            public const int Vegetation = 1 << RenderLayers.Vegetation;
            /// <inheritdoc cref="RenderLayers.RainObstacle"/>
            public const int RainObstacle = 1 << RenderLayers.RainObstacle;
            /// <inheritdoc cref="RenderLayers.RainTriggerVolume"/>
            public const int RainTriggerVolume = 1 << RenderLayers.RainTriggerVolume;
            /// <inheritdoc cref="RenderLayers.Impostor"/>
            public const int Impostor = 1 << RenderLayers.Impostor;
            /// <inheritdoc cref="RenderLayers.SafeZone"/>
            public const int SafeZone = 1 << RenderLayers.SafeZone;
            /// <inheritdoc cref="RenderLayers.Wyrdness"/>
            public const int Wyrdness = 1 << RenderLayers.Wyrdness;
            /// <inheritdoc cref="RenderLayers.Time"/>
            public const int Time = 1 << RenderLayers.Time;
            /// <inheritdoc cref="RenderLayers.Ragdolls"/>
            public const int Ragdolls = 1 << RenderLayers.Ragdolls;
            /// <inheritdoc cref="RenderLayers.ProjectileAndAttacks"/>
            public const int ProjectileAndAttacks = 1 << RenderLayers.ProjectileAndAttacks;
            /// <inheritdoc cref="RenderLayers.Hitboxes"/>
            public const int Hitboxes = 1 << RenderLayers.Hitboxes;
            /// <inheritdoc cref="RenderLayers.AIs"/>
            public const int AIs = 1 << RenderLayers.AIs;
            /// <inheritdoc cref="RenderLayers.AIInteractions"/>
            public const int AIInteractions = 1 << RenderLayers.AIInteractions;
            /// <inheritdoc cref="RenderLayers.PlayerInteractions"/>
            public const int PlayerInteractions = 1 << RenderLayers.PlayerInteractions;
            /// <inheritdoc cref="RenderLayers.Player"/>
            public const int Player = 1 << RenderLayers.Player;
            /// <inheritdoc cref="RenderLayers.HLOD"/>
            public const int HLOD = 1 << RenderLayers.HLOD;
            /// <summary>
            /// When we are rendering only the void
            /// </summary>
            public const int Void = UI | Player;
            public const int CharacterGround = Default | Walkable | Objects | NavigationIgnoreObj | Terrain | Vegetation;
            public const int FallDamageNullifier = Transparent;
        }
    }
}
