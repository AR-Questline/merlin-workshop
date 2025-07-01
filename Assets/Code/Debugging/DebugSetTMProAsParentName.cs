using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Awaken.TG
{
    public class DebugSetTMProAsParentName : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            GetComponent<TextMeshPro>().text = this.transform.parent.name;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
