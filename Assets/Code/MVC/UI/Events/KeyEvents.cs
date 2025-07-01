using UnityEngine;

namespace Awaken.TG.MVC.UI.Events
{
    /// <summary>
    /// Base class for both key events.
    /// </summary>
    public abstract class UIKeyEvent : UIEvent
    {
        public KeyCode Key { get; set; }
    }

    public class UIEKeyDown : UIKeyEvent { }
}
