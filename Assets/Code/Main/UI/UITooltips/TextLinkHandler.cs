using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.UI.UITooltips {
    public class TextLinkHandler : ViewComponent<Model>, IWithTechnicalTooltip {
        // === Editable properties

        public Color highlightedLinkColor = Color.white;

        // === Types

        public class Link {
            public int index;
            public string data;
            public TMP_LinkInfo tmpInfo;
            public Color originalColor;
        }

        // === State

        TextMeshProUGUI _text;
        int _hoverFrame;
        Link _currentLink;

        bool Hovered => Time.frameCount < _hoverFrame + 2;

        // === Events

        public static class Events {
            public static readonly Event<IModel, Link> LinkClicked = new(nameof(LinkClicked));
        }

        // === Initialization

        protected override void OnAttach() {
            _text = GetComponent<TextMeshProUGUI>();
            if (_text == null) {
                Destroy(this);
            }
        }

        void OnEnable() {
            Services.Get<UnityUpdateProvider>().RegisterTextLinkHandler(this);
        }

        void OnDisable() {
            Services.Get<UnityUpdateProvider>().UnregisterTextLinkHandler(this);
        }

        public void UnityUpdate() {
            if (_text == null) {
                OnAttach();
                return;
            }

            if (!Hovered && _currentLink == null) return;
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_text, Input.mousePosition, null);
            if (linkIndex == -1) {
                if (_currentLink != null) {
                    SetLinkHighlight(_currentLink, false);
                }

                _currentLink = null;
            } else {
                if (_currentLink?.index == linkIndex) return;
                TMP_LinkInfo linkInfo = _text.textInfo.linkInfo[linkIndex];
                _currentLink = new Link {
                    index = linkIndex,
                    data = linkInfo.GetLinkID(),
                    tmpInfo = linkInfo,
                };
                SetLinkHighlight(_currentLink, true);
            }
        }

        void SetLinkHighlight(Link link, bool highlighted) {
            // hacky, but there are no unhacky solutions
            if (highlighted) {
                link.originalColor = ChangeColor(link, highlightedLinkColor);
            } else {
                ChangeColor(link, link.originalColor);
            }
        }

        Color ChangeColor(Link link, Color newColor) {
            int startIndex = link.tmpInfo.linkTextfirstCharacterIndex;
            int endIndex = startIndex + link.tmpInfo.linkTextLength;
            Color oldColor = Color.red;
            for (int ci = startIndex; ci < endIndex; ci++) {
                TMP_CharacterInfo cInfo = _text.textInfo.characterInfo[ci];
                if (char.IsWhiteSpace(cInfo.character)) continue;
                int materialIndex = cInfo.materialReferenceIndex;
                int vertexIndex = cInfo.vertexIndex;
                Color32[] vertexColors = _text.textInfo.meshInfo[materialIndex].colors32;
                oldColor = vertexColors[vertexIndex];
                vertexColors[vertexIndex + 0] = newColor;
                vertexColors[vertexIndex + 1] = newColor;
                vertexColors[vertexIndex + 2] = newColor;
                vertexColors[vertexIndex + 3] = newColor;
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            return oldColor;
        }

        // === IUIAware

        public UIResult Handle(UIEvent evt) {
            if (evt is UIEMouseDown { IsLeft: true } && _currentLink != null) {
                Target.Trigger(Events.LinkClicked, _currentLink);
            }

            if (evt is UIEPointTo) {
                _hoverFrame = Time.frameCount;
            }

            return UIResult.Ignore;
        }


        string TooltipData {
            get {
                string data = _currentLink?.data;
                if (data != null && data.Contains("tooltip")) {
                    string tooltip = data.Split(';')[0].Replace("tooltip:", "");
                    return Services.Get<UITooltipStorage>().Get(tooltip, tooltip.Translate());
                }

                return null;
            }
        }

        string TechnicalTooltipData {
            get {
                string data = _currentLink?.data;
                if (data != null && data.Contains("technical")) {
                    string tooltip = data.Split(';')[1].Replace("technical:", "");
                    return Services.Get<UITooltipStorage>().Get(tooltip, tooltip.Translate());
                }

                return null;
            }
        }

        public TooltipConstructor TooltipConstructor => TooltipData;
        public TooltipConstructor TechnicalTooltipConstructor => TechnicalTooltipData;

        public static void OpenLinksOf(IModel model) {
            model.ListenTo(Events.LinkClicked, link => {
                var url = link.data;
                if (string.IsNullOrEmpty(url)) {
                    url = link.tmpInfo.GetLinkText();
                }
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto)) {
                    Application.OpenURL(url);
                } else {
                   Log.Important?.Error($"Invalid URL: {url}");
                }
            });
        }
    }
}