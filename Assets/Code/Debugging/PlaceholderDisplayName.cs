using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG
{
    public class PlaceholderDisplayName : MonoBehaviour
    {
        [SerializeField] Text displayText;
        [SerializeField] string additionalInfo;
        string separator = "\n------\n";
        void Start()
        {
            displayText.text = this.transform.parent.name + separator + additionalInfo;
        }
    }
}
