using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Enums;
using FMODUnity;

namespace Awaken.TG.Main.Utility.Animations {
    [RichEnumAlwaysDisplayCategory]
    public class SurfaceType : RichEnum {
        public string FModParameterName { get; }
        int FModParameterValue { get; }
        protected SurfaceType(string enumName, string parameterName, int fModParameterValue, string inspectorCategory) : base(enumName, inspectorCategory) {
            FModParameterValue = fModParameterValue;
            FModParameterName = parameterName;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly SurfaceType
            HitWood = new(nameof(HitWood), "Surface", 3, "HitSurface"),
            HitStone = new(nameof(HitStone), "Surface", 2, "HitSurface"),
            HitMetal = new(nameof(HitMetal), "Surface", 0, "HitSurface"),
            HitFlesh = new(nameof(HitFlesh), "Surface", 1, "HitSurface"),
            HitGround = new(nameof(HitGround), "Surface", 4, "HitSurface"),
            HitMagic = new(nameof(HitMagic), "Surface", 5, "HitSurface"),
            HitFabric = new(nameof(HitFabric), "Surface", 6, "HitSurface"),
            HitLeather = new(nameof(HitLeather), "Surface", 7, "HitSurface"),
            HitBones = new(nameof(HitBones), "Surface", 1, "HitSurface"),

            DamageWood = new(nameof(DamageWood), "Surface", 0, "DamageSurface"),
            DamageMetal = new(nameof(DamageMetal), "Surface", 1, "DamageSurface"),
            DamageArrow = new(nameof(DamageArrow), "Surface", 2, "DamageSurface"),
            DamageMagic = new(nameof(DamageMagic), "Surface", 3, "DamageSurface"),
            DamageOrganic = new(nameof(DamageOrganic), "Surface", 4, "DamageSurface"),

            TerrainGrass = new(nameof(TerrainGrass), "FTS_Grass", 0, "TerrainType"),
            TerrainGravel = new(nameof(TerrainGravel), "FTS_Gravel", 0, "TerrainType"),
            TerrainGround = new(nameof(TerrainGround), "FTS_Ground", 0, "TerrainType"),
            TerrainMud = new(nameof(TerrainMud), "FTS_Mud", 0, "TerrainType"),
            TerrainPuddle = new(nameof(TerrainPuddle), "FTS_Puddle", 0, "TerrainType"),
            TerrainSnow = new(nameof(TerrainSnow), "FTS_Snow", 0, "TerrainType"),
            TerrainStone = new(nameof(TerrainStone), "FTS_Stone", 0, "TerrainType"),
            TerrainSand = new(nameof(TerrainSand), "FTS_Sand", 0, "TerrainType"),
            TerrainWood = new(nameof(TerrainWood), "FTS_Wood", 0, "TerrainType"),
            TerrainCloth = new(nameof(TerrainCloth), "FTS_Cloth", 0, "TerrainType"),
            TerrainMetal = new(nameof(TerrainMetal), "FTS_Metal", 0, "TerrainType"),

            ArmorMetal = new(nameof(ArmorMetal), "Surface", 0, "ArmorType"),
            ArmorFabric = new(nameof(ArmorFabric), "Surface", 1, "ArmorType"),
            ArmorLeather = new(nameof(ArmorLeather), "Surface", 2, "ArmorType"),
            
            FallNone = new(nameof(FallNone), "FallNullifier", 0, "FallNullifier"),
            FallHay = new(nameof(FallHay), "FallNullifier", 1, "FallNullifier"),
            FallBodies = new(nameof(FallBodies), "FallNullifier", 2, "FallNullifier"),
            FallWater = new(nameof(FallWater), "FallNullifier", 3, "FallNullifier");

        public static readonly SurfaceType[] TerrainTypes = {
            TerrainGrass, TerrainGravel, TerrainGround, TerrainMud, TerrainPuddle, TerrainSnow, TerrainStone, TerrainSand, TerrainWood, TerrainCloth, TerrainMetal
        };
        
        

        public static implicit operator FMODParameter(SurfaceType surfaceType) {
            return new FMODParameter(surfaceType.FModParameterName, surfaceType.FModParameterValue);
        }
    }
}