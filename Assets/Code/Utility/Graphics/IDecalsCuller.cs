using System.Collections.Generic;

namespace Awaken.Utility.Graphics {
    public interface IDecalsCuller {
        public static readonly HashSet<IDecalsCuller> DecalsCullers = new HashSet<IDecalsCuller>(8);

        public bool enabled { get; set; }
        public string DescriptiveName { get; }
    }
}