using UnityEngine.Localization.Events;

namespace UnityEngine.Localization.Components
{
    /// <summary>
    /// Component that can be used to Localize a [Prefab](https://docs.unity3d.com/Manual/Prefabs.html).
    /// When the Locale is changed the prefab will be instantiated as a child of the gameobject this component is attached to, the instance will then be sent through <see cref="LocalizedAssetEvent{TObject, TReference, TEvent}.OnUpdateAsset"/>.
    /// </summary>
    [AddComponentMenu("Localization/Asset/Localize Prefab Event")]
    public class LocalizedGameObjectEvent : LocalizedAssetEvent<GameObject, LocalizedGameObject, UnityEventGameObject>
    {
        GameObject m_Current;

        /// <inheritdoc/>
        protected override void UpdateAsset(GameObject localizedAsset)
        {
            if (m_Current != null)
            {
                Destroy(m_Current);
                m_Current = null;
            }

            if (localizedAsset != null)
            {
                m_Current = Instantiate(localizedAsset, transform);
                m_Current.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            }

            OnUpdateAsset.Invoke(m_Current);
        }
    }
}
