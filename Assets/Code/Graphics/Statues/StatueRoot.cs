using Awaken.CommonInterfaces;
using UnityEngine;

namespace Awaken.TG.Graphics.Statues {
    public class StatueRoot : MonoBehaviour, IDrakeRepresentationOptionsProvider {
        public bool ProvideRepresentationOptions => true;

        public IWithUnityRepresentation.Options GetRepresentationOptions() {
            return new IWithUnityRepresentation.Options() {
                movable = false
            };
        }
    }
}