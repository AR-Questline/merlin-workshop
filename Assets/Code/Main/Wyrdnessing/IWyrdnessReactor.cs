using Awaken.TG.Main.Grounds;

namespace Awaken.TG.Main.Wyrdnessing {
    public interface IWyrdnessReactor : IGrounded {
        public bool IsSafeFromWyrdness { get; set; }
    }
}

