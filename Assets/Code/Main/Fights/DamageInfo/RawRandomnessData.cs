namespace Awaken.TG.Main.Fights.DamageInfo {
    public class RawRandomnessData {
        public float AdditionalCritChance { get; private set; } = 0;
        public float RandomOccurrenceEfficiency { get; private set; } = 1f;
        
        [UnityEngine.Scripting.Preserve]
        public void AddCritChance(float critChance) {
            AdditionalCritChance += critChance;
        }
        
        public void SetRandomOccurrenceEfficiency(float randomOccurrenceEfficiency) {
            RandomOccurrenceEfficiency = randomOccurrenceEfficiency;
        }
    }
}