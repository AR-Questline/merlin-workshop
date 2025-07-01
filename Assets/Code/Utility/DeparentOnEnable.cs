using UnityEngine;

namespace Awaken
{
    public class DeparentOnEnable : MonoBehaviour
    {
        void OnEnable()
        {
            transform.SetParent(null);
        }
    }
}
