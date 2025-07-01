using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class UntilDiscarded : NonEditableDuration<IWithDuration> {
        public override ushort TypeForSerialization => SavedModels.UntilDiscarded;

        public override bool Elapsed => false;
        public override string DisplayText => string.Empty;
    }
}