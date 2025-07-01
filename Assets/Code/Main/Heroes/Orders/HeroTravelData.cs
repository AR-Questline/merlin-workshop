namespace Awaken.TG.Main.Heroes.Orders {
    [UnityEngine.Scripting.Preserve]
    public readonly struct HeroTravelData {
        public float SpentActionPoints { [UnityEngine.Scripting.Preserve] get; }
        public float TraveledDistance { [UnityEngine.Scripting.Preserve] get; }
        public float ElapsedTime { [UnityEngine.Scripting.Preserve] get; }
        
        public HeroTravelData(float spentActionPoints, float traveledDistance, float elapsedTime) {
            this.SpentActionPoints = spentActionPoints;
            this.TraveledDistance = traveledDistance;
            this.ElapsedTime = elapsedTime;
        }
    }
}