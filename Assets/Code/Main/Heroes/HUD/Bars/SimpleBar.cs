using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public abstract class SimpleBar : Bar {
        [SerializeField] protected Image bar;
        
        public override Color Color {
            get => bar.color;
            set => bar.color = value;
        }
    }
}