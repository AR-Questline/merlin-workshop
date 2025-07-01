using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    public class VFXNamedTarget : MonoBehaviour, ITagged {
        [Tags(TagsCategory.VFXTarget)]
        public string[] tags = Array.Empty<string>();

        public ICollection<string> Tags => tags;
    }
}