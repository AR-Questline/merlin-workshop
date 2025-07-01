using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    /// <summary>
    /// Marker interface for all finishers so that spawning new finisher can easily discard finishers already added to hero.
    /// </summary>
    public interface IFinisher : IElement<Hero> {
        void Release(Vector3 position);
    }
}