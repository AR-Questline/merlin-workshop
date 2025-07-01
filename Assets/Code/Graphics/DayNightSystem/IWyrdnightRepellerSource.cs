using UnityEngine;

namespace Awaken.TG.Graphics.DayNightSystem {
    public interface IWyrdnightRepellerSource {
        bool IsFast => false;
        bool IsPositionInRepeller(Vector3 position);
    }
}