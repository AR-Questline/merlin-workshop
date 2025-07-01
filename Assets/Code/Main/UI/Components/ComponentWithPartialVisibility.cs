using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// Base for components with 'layered' visibility. <br/>
    /// It has 3 visibility layers: External, Middle and Internal. <br/>
    /// Only when all three layers are set to visible, component is visible. <br/>
    /// It is refreshed using TData. <br/> <br/>
    /// 
    /// Responsibility of each layer is hold by different entity: <br/> <br/>
    ///
    /// External: <br/>
    /// Should be set from owner of component. <br/>
    /// It is used to set visibility of component based on specific case component is used in. <br/>
    /// <i> eg. EmptySlotIcon in Equipment is visible, but in Bag it is not. </i> <br/> <br/>
    ///
    /// Middle: <br/>
    /// Is set only by this class on TryRefresh and is computed in MiddleVisibilityOf. <br/>
    /// It is used to set visibility of component based on the correctness of the data. <br/>
    /// <i> eg. SlottedItemIcon is hidden when Item is null. </i> <br/> <br/>
    ///
    /// Internal: <br/>
    /// Is set by specific implementation of this class on Refresh. <br/>
    /// Is is used to set visibility of component base on the content of the data. <br/>
    /// <i> eg. ItemQuantity is hidden when Item.Quantity is equal to one. </i> <br/>
    /// </summary>
    /// <typeparam name="TData"> Data it is refreshed with </typeparam>
    public abstract class ComponentWithPartialVisibility<TData> : MonoBehaviour {
        [SerializeField] GameObject panel;

        PartialVisibility _visibility;

        public void TryRefresh(TData data) {
            if (MiddleVisibilityOf(data)) {
                SetMiddleVisibility(true);
                Refresh(data);
            } else {
                SetMiddleVisibility(false);
            }
        }

        protected abstract bool MiddleVisibilityOf(TData data);
        protected abstract void Refresh(TData data);

        /// <summary>
        /// External: <br/>
        /// Should be set from owner of component. <br/>
        /// It is used to set visibility of component based on specific case component is used in. <br/>
        /// <i> eg. EmptySlotIcon in Equipment is visible, but in Bag it is not. </i>
        /// </summary>
        public void SetExternalVisibility(bool visibility) {
            _visibility.SetExternal(visibility);
            panel.SetActive(_visibility);
        }

        /// <summary>
        /// Middle: <br/>
        /// Is set only by this class on TryRefresh and is computed in MiddleVisibilityOf. <br/>
        /// It is used to set visibility of component based on the correctness of the data. <br/>
        /// <i> eg. SlottedItemIcon is hidden when Item is null. </i> <br/>
        /// </summary>
        void SetMiddleVisibility(bool visibility) {
            _visibility.SetMiddle(visibility);
            panel.SetActive(_visibility);
        }

        /// <summary>
        /// Internal: <br/>
        /// Is set by specific implementation of this class on Refresh. <br/>
        /// Is is used to set visibility of component base on the content of the data. <br/>
        /// <i> eg. ItemQuantity is hidden when Item.Quantity is equal to one. </i>
        /// </summary>
        protected void SetInternalVisibility(bool visibility) {
            _visibility.SetInternal(visibility);
            panel.SetActive(_visibility);
        }
    }
}