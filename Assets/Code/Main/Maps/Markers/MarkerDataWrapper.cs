using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Markers {
    public interface IMarkerDataWrapper {
        LocationMarker CreateMarker();
        MarkerData MarkerData => this switch {
            SimpleMarkerDataWrapper simple => simple.Get,
            NpcMarkerDataWrapper npc => npc.Get,
            DiscoveryMarkerDataWrapper discovery => discovery.Get,
            _ => throw new ArgumentException($"[{this}].[{this.GetType()}] is not implemented for MarkerData"),
        };
        IMarkerDataTemplate Template { get; }
    }

    public class SimpleMarkerDataWrapper : MarkerDataWrapper<MarkerData>, IMarkerDataWrapper {
        public LocationMarker CreateMarker() => new();
    }
    public class NpcMarkerDataWrapper : MarkerDataWrapper<NpcMarkerData>, IMarkerDataWrapper {
        public LocationMarker CreateMarker() => new NpcMarker();
    }
    public class DiscoveryMarkerDataWrapper : MarkerDataWrapper<DiscoveryMarkerData>, IMarkerDataWrapper {
        public LocationMarker CreateMarker() => new DiscoveryMarker();
    }

    [Serializable]
    public abstract class MarkerDataWrapper<TData> where TData : MarkerData {
        enum Method {
            Embedded = 0,
            Explicit = 1
        }

        [SerializeField, HideLabel, EnumToggleButtons]
        Method method;

        [ShowIf(nameof(IsExplicit), false), SerializeReference]
        MarkerDataTemplate<TData> explicitData;

        [ShowIf(nameof(IsEmbedded), false), HideLabel, SerializeField]
        TData embeddedData;

        TData ExplicitData => explicitData != null ? explicitData.Get : null;

        bool IsExplicit => method == Method.Explicit;
        bool IsEmbedded => method == Method.Embedded;

        public TData Get => IsExplicit ? ExplicitData : embeddedData;
        public IMarkerDataTemplate Template => explicitData;
    }
}