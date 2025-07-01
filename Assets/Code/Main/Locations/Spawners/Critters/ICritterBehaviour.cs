using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    public interface ICritterBehaviour {
        [UnityEngine.Scripting.Preserve] void OnInitialize(CritterSpawner spawner, Critter critter);
        [UnityEngine.Scripting.Preserve] void OnUpdate(float deltaTime);
        [UnityEngine.Scripting.Preserve] void OnDeath(GameObject dropGo);
        [UnityEngine.Scripting.Preserve] void OnDisable();
    }
}