using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Stories
{
    /// <summary>
    /// Allows starting a specific story from a chosen point.
    /// </summary>
    [Serializable]
    public partial class StoryBookmark {
        public ushort TypeForSerialization => SavedTypes.StoryBookmark;

        // === References

        [TemplateType(typeof(StoryGraph))]
        [Saved] public TemplateReference story;
        [Saved] public string chapterName;

        // === Properties

        public bool IsValid => story?.IsSet ?? false;
        public string GUID => story?.GUID ?? "(null)";
        public string ChapterName => string.IsNullOrEmpty(chapterName) ? "Start" : chapterName;
        
#if UNITY_EDITOR
        public StoryGraph EDITOR_Graph => story.Get<StoryGraph>();
        public IEditorChapter EDITOR_Chapter => string.IsNullOrEmpty(chapterName) ? EDITOR_Graph?.InitialChapter : EDITOR_Graph?.BookmarkedChapter(chapterName);

#endif

        // === Constructors

        [JsonConstructor, UnityEngine.Scripting.Preserve] public StoryBookmark() {}
        
        StoryBookmark(TemplateReference template, string chapterName) {
            this.story = template;
            this.chapterName = chapterName;
        }

        public static StoryBookmark ToInitialChapter(TemplateReference story) {
            if (story == null || story.IsSet == false) {
                return null;
            }
            return new StoryBookmark(story, null);
        }

        public static StoryBookmark ToSpecificChapter(TemplateReference story, string chapter) {
            if (story == null || story.IsSet == false) {
                return null;
            }
            return new StoryBookmark(story, chapter);
        }
        
        public static bool ToInitialChapter(TemplateReference story, out StoryBookmark bookmark) {
            if (story == null || story.IsSet == false) {
                bookmark = null;
                return false;
            }
            bookmark = new StoryBookmark(story, null);
            return true;
        }
        
        public static bool ToSpecificChapter(TemplateReference story, string chapter, out StoryBookmark bookmark) {
            if (story == null || story.IsSet == false) {
                bookmark = null;
                return false;
            }
            bookmark = new StoryBookmark(story, chapter);
            return true;
        }

#if UNITY_EDITOR
        public static StoryBookmark EDITOR_ToInitialChapter(StoryGraph story) => new(new TemplateReference(story), null);
        public static StoryBookmark EDITOR_ToSpecificChapter(StoryGraph story, string chapter) => new(new TemplateReference(story), chapter);
#endif
        
        // === Operations

        public ARAssetReference GetGraphReference() {
            return new ARAssetReference(story.GUID);
        }
        
        public void JumpToBookmark(Story api) => api.JumpToDifferentGraph(this);

        // === Conversions

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(story), story);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(chapterName), chapterName);
            jsonWriter.WriteEndObject();
        }
        
        // == Equals

        public override int GetHashCode() {
            unchecked {
                return ((story != null ? story.GetHashCode() : 0) * 397) ^ (chapterName != null ? chapterName.GetHashCode() : 0);
            }
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is StoryBookmark bookmark && Equals(bookmark);
        }

        bool Equals(StoryBookmark other) {
            return other.story == story && other.chapterName == chapterName;
        }
        
        public static bool operator ==(StoryBookmark a, StoryBookmark b) {
            return Equals(a, b);
        }
        public static bool operator !=(StoryBookmark a, StoryBookmark b) {
            return !Equals(a, b);
        }
    }
}
