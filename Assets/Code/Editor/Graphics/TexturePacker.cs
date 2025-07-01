using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

// Texture Packer v1.2

namespace Awaken.TG.Editor.Graphics {
    public class TexturePacker : OdinEditorWindow {
        #region Properties
        [EnumToggleButtons, HideLabel] 
        public ScriptMode scriptMode = ScriptMode.Pack;

        [EnumToggleButtons, HideLabel, ShowIf("scriptMode", ScriptMode.Pack)] 
        public ChannelPreview channelPreview = ChannelPreview.RGB;
        
        [HideLabel]
        public Texture2D texPreview;

        [HideLabel]
        public Preset preset = Preset.HDRP_Lit_BaseMap;
        
        [HorizontalGroup("SH")]
        [BoxGroup("SH/R", showLabel: false), HideLabel, ReadOnly, ShowIf("scriptMode", ScriptMode.Pack)]
        public string chRoleR = "Metallic";
        [BoxGroup("SH/G", showLabel: false), HideLabel, ReadOnly, ShowIf("scriptMode", ScriptMode.Pack)]
        public string chRoleG = "Occlusion";
        [BoxGroup("SH/B", showLabel: false), HideLabel, ReadOnly, ShowIf("scriptMode", ScriptMode.Pack)]
        public string chRoleB = "Detail";
        [BoxGroup("SH/A", showLabel: false), HideLabel, ReadOnly, ShowIf("scriptMode", ScriptMode.Pack)]
        public string chRoleA = "Smoothness";
        
        [BoxGroup("SH/R", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack), ReadOnly]
        public Vector2 texSampleRsize;
        [BoxGroup("SH/G", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack), ReadOnly]
        public Vector2 texSampleGsize;
        [BoxGroup("SH/B", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack), ReadOnly]
        public Vector2 texSampleBsize;
        [BoxGroup("SH/A", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack), ReadOnly]
        public Vector2 texSampleAsize;
        
        [BoxGroup("SH/R", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)] 
        public Texture2D texSampleR;
        [BoxGroup("SH/G", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)] 
        public Texture2D texSampleG;
        [BoxGroup("SH/B", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)] 
        public Texture2D texSampleB;
        [BoxGroup("SH/A", centerLabel: true), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)] 
        public Texture2D texSampleA;
        
        [BoxGroup("SH/R", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        public TextureChannel chR = TextureChannel.R;
        [BoxGroup("SH/G", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        public TextureChannel chG = TextureChannel.R;
        [BoxGroup("SH/B", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        public TextureChannel chB = TextureChannel.R;
        [BoxGroup("SH/A", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        public TextureChannel chA = TextureChannel.R;

        [Button(ButtInvName, ButtonStyle.FoldoutButton),BoxGroup("SH/R", showLabel: false), HideLabel, GUIColor("GetColorR"), ShowIf("scriptMode", ScriptMode.Pack)]
        bool InvR() {
            this._invR = !this._invR;
            return _invR;
        }
        [Button(ButtInvName, ButtonStyle.FoldoutButton),BoxGroup("SH/G", showLabel: false), HideLabel, GUIColor("GetColorG"), ShowIf("scriptMode", ScriptMode.Pack)]
        bool InvG() {
            this._invG = !this._invG;
            return _invG;
        }
        [Button(ButtInvName, ButtonStyle.FoldoutButton),BoxGroup("SH/B", showLabel: false), HideLabel, GUIColor("GetColorB"), ShowIf("scriptMode", ScriptMode.Pack)]
        bool InvB() {
            this._invB = !this._invB;
            return _invB;
        }
        [Button(ButtInvName, ButtonStyle.FoldoutButton),BoxGroup("SH/A", showLabel: false), HideLabel, GUIColor("GetColorA"), ShowIf("scriptMode", ScriptMode.Pack)]
        bool InvA() {
            this._invA = !this._invA;
            return _invA;
        }

        [Button(ButtDirName), BoxGroup("SH/R", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        void DirR() {
            exportPath = SamplePath(texSampleR);
        }
        [Button(ButtDirName), BoxGroup("SH/G", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        void DirG() {
            exportPath = SamplePath(texSampleG);
        }
        [Button(ButtDirName), BoxGroup("SH/B", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        void DirB() {
            exportPath = SamplePath(texSampleB);
        }
        [Button(ButtDirName), BoxGroup("SH/A", showLabel: false), HideLabel, ShowIf("scriptMode", ScriptMode.Pack)]
        void DirA() {
            exportPath = SamplePath(texSampleA);
        }
        [Button(ButtDirName), HideLabel, ShowIf("scriptMode", ScriptMode.Unpack)]
        void Dir() {
            exportPath = SamplePath(texPreview);
        }
        
        [BoxGroup("Export Path"), FolderPath(ParentFolder = ""), HideLabel]
        public string exportPath = "Assets";

        [BoxGroup("Texture Name"), HideLabel]
        public string texName = "Tex_";

        [Button(ButtonSizes.Large), ButtonGroup("Refresh"), ShowIf("scriptMode", ScriptMode.Pack)]
        void Refresh() {
            RefreshAll();
        }
        
        [Button(ButtonSizes.Large), ButtonGroup("Refresh"), ShowIf("scriptMode", ScriptMode.Pack), GUIColor("GetColorRefresh")]
        void AutoRefresh() {
            _refresh = !_refresh;
            if (_refresh) {
                RefreshAll();
            }
        }
        
        [Button(ButtonSizes.Gigantic), ShowIf("scriptMode", ScriptMode.Pack)]
        void Pack() {
            PackTextures();
        }
        
        [Button(ButtonSizes.Gigantic), ShowIf("scriptMode", ScriptMode.Unpack)]
        void Unpack() {
            UnpackTextures();
        }
        
        [Button(ButtonSizes.Medium)]
        void Clear() {
            texPreview = null;
            chRoleR = "R";
            chRoleG = "G";
            chRoleB = "B";
            chRoleA = "A";
            texSampleR = null;
            texSampleG = null;
            texSampleB = null;
            texSampleA = null;
            texSampleRsize = new Vector2(0, 0);
            texSampleGsize = new Vector2(0, 0);
            texSampleBsize = new Vector2(0, 0);
            texSampleAsize = new Vector2(0, 0);
            chR = TextureChannel.R;
            chG = TextureChannel.R;
            chB = TextureChannel.R;
            chA = TextureChannel.R;
            texName = "Tex_";
            _invR = false;
            _invG = false;
            _invB = false;
            _invA = false;
            exportPath = "Assets";
            channelPreview = ChannelPreview.RGB;
        }

        public enum ScriptMode {
            Pack,
            Unpack
        }
        public enum Preset {
            None,
            HDRP_Lit_BaseMap,
            HDRP_Lit_MaskMap,
            HDRP_Decal_BaseMap,
            HDRP_Decal_MaskMap,
            Architecture_Base_MaskMap,
            Architecture_Overlay_MaskMap,
            Morph_MaskMapA,
            Morph_MaskMapB,
            KF_Ghost_MaskMap,
            KF_Ghost_DistortionMap,
            KF_Ghost_EmissionMap,
            LayeredTile_BaseColorSmoothness,
            LayeredTile_NormalOcclusion,
            LayeredTile_Masks,
            Lit_FlowMap_MaskMap,
            Lit_FlowMap_FlowMap,
            Lit_FlowMap_Emission
        }
        public enum TextureChannel {
            R,G,B,A
        }

        public enum ChannelPreview {
            RGB,R,G,B,A
        }
        
        bool _invR, _invG, _invB, _invA, _refresh;
        string _channelName;
        string _texName;
        const string ButtInvName = "INVERT";
        const string ButtDirName = "DIR";

        Color GetColorR() { return this._invR ? new Color(0.75f,0.5f,1.0f) : new Color(1.0f,1.0f,1.0f); }
        Color GetColorG() { return this._invG ? new Color(0.75f,0.5f,1.0f) : new Color(1.0f,1.0f,1.0f); }
        Color GetColorB() { return this._invB ? new Color(0.75f,0.5f,1.0f) : new Color(1.0f,1.0f,1.0f); }
        Color GetColorA() { return this._invA ? new Color(0.75f,0.5f,1.0f) : new Color(1.0f,1.0f,1.0f); }
        Color GetColorRefresh() { return this._refresh ? new Color(0.75f,0.5f,1.0f) : new Color(1.0f,1.0f,1.0f); }
        #endregion
        
        #region Window
        [MenuItem("ArtTools/Texture Packer")]
        public static void ShowWindow() {
            GetWindow<TexturePacker>().minSize = new Vector2(360, 822);
            GetWindow<TexturePacker>().maxSize = new Vector2(360, 822);
            GetWindow<TexturePacker>().Show();
        }
        #endregion
        
        #region Execute
        void OnValidate() {
            GetPresetChannelNames();
            SetTexSamplesResolution();
            
            if (_refresh)
                RefreshAll();
        }

        void RefreshAll() {
            if (scriptMode == ScriptMode.Pack) {
                if (texSampleR == null && texSampleG == null && texSampleB == null && texSampleA == null)
                    texPreview = null;
                else {
                    int res = GetSampleMaxResolution();
                    texPreview = new Texture2D(res, res);
                    texPreview.SetPixels(SampleTextures(res));
                    texPreview.Apply();
                }
            }

            if (scriptMode == ScriptMode.Unpack) {
                
            }
        }
        #endregion
        
        #region Methods
        void GetPresetChannelNames() {
            switch (preset) {
                case Preset.None:
                    chRoleR = "R";
                    chRoleG = "G";
                    chRoleB = "B";
                    chRoleA = "A";
                    break;
                case Preset.HDRP_Lit_BaseMap:
                    chRoleR = "Color R";
                    chRoleG = "Color G";
                    chRoleB = "Color B";
                    chRoleA = "Opacity";
                    break;
                case Preset.HDRP_Lit_MaskMap:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "Detail";
                    chRoleA = "Smoothness";
                    break;
                case Preset.HDRP_Decal_BaseMap:
                    chRoleR = "Color R";
                    chRoleG = "Color G";
                    chRoleB = "Color B";
                    chRoleA = "Opacity";
                    break;
                case Preset.HDRP_Decal_MaskMap:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "Opacity";
                    chRoleA = "Smoothness";
                    break;
                case Preset.Architecture_Base_MaskMap:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "Height";
                    chRoleA = "Smoothness";
                    break;
                case Preset.Architecture_Overlay_MaskMap:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "Overlay Mask";
                    chRoleA = "Smoothness";
                    break;
                case Preset.KF_Ghost_MaskMap:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "-";
                    chRoleA = "Smoothness";
                    break;
                case Preset.KF_Ghost_DistortionMap:
                    chRoleR = "Normal R";
                    chRoleG = "Normal G";
                    chRoleB = "Normal B";
                    chRoleA = "-";
                    break;
                case Preset.KF_Ghost_EmissionMap:
                    chRoleR = "Emission R";
                    chRoleG = "Emission G";
                    chRoleB = "Emission B";
                    chRoleA = "-";
                    break;
                case Preset.Morph_MaskMapA:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "Morph Mask";
                    chRoleA = "Smoothness";
                    break;
                case Preset.Morph_MaskMapB:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "Emission Mask";
                    chRoleA = "Smoothness";
                    break;
                case Preset.LayeredTile_BaseColorSmoothness:
                    chRoleR = "Color R";
                    chRoleG = "Color G";
                    chRoleB = "Color B";
                    chRoleA = "Smoothness";
                    break;
                case Preset.LayeredTile_NormalOcclusion:
                    chRoleR = "Normal R";
                    chRoleG = "Normal G";
                    chRoleB = "Occlusion";
                    chRoleA = "-";
                    break;
                case Preset.LayeredTile_Masks:
                    chRoleR = "Emissive Mask";
                    chRoleG = "Layer 2 Mask";
                    chRoleB = "Wetness Mask";
                    chRoleA = "Iridescence Mask";
                    break;
                case Preset.Lit_FlowMap_MaskMap:
                    chRoleR = "Metallic";
                    chRoleG = "Occlusion";
                    chRoleB = "-";
                    chRoleA = "Smoothness";
                    break;
                case Preset.Lit_FlowMap_FlowMap:
                    chRoleR = "Flow V";
                    chRoleG = "Flow H";
                    chRoleB = "Flow Mask";
                    chRoleA = "-";
                    break;
                case Preset.Lit_FlowMap_Emission:
                    chRoleR = "Emission R";
                    chRoleG = "Emission G";
                    chRoleB = "Emission B";
                    chRoleA = "-";
                    break;
            }
        }

        int GetSampleMaxResolution() {
            var res = 1024;
            List<int> resTable = new (){0};
            if (texSampleR != null)
                resTable.Add(texSampleR.width);
            if (texSampleG != null)
                resTable.Add(texSampleG.width);
            if (texSampleB != null)
                resTable.Add(texSampleB.width);
            if (texSampleA != null)
                resTable.Add(texSampleA.width);
            res = resTable.Max();
            return res;
        }

        void SetTexSamplesResolution() {
            if (texSampleR != null)
                texSampleRsize = new Vector2(texSampleR.width, texSampleR.height);
            else
                texSampleRsize = new Vector2(0, 0);
            if (texSampleG != null) 
                texSampleGsize = new Vector2(texSampleG.width, texSampleG.height);
            else
                texSampleGsize = new Vector2(0, 0);
            if (texSampleB != null)
                texSampleBsize = new Vector2(texSampleB.width, texSampleB.height);
            else
                texSampleBsize = new Vector2(0, 0);
            if (texSampleA != null)
                texSampleAsize = new Vector2(texSampleA.width, texSampleA.height);
            else
                texSampleAsize = new Vector2(0, 0);
        }

        Color[] SampleTextures(int resolution) {
            Color[] col = new Color[resolution * resolution];
            Texture2D texR = null;
            Texture2D texG = null;
            Texture2D texB = null;
            Texture2D texA = null;
            
            if(texSampleR != null)
                texR = CopyTexture(texSampleR);
            if(texSampleG != null)
                texG = CopyTexture(texSampleG);
            if(texSampleB != null)
                texB = CopyTexture(texSampleB);
            if(texSampleA != null) 
                texA = CopyTexture(texSampleA);
            
            if (scriptMode == ScriptMode.Pack || channelPreview == ChannelPreview.RGB) {
                for (int i = 0; i < col.Length; i++) {
                    col[i] = new Color();
                    Vector2 res = new Vector2(i % resolution, i / resolution);
                    if (texR != null) {
                        switch (chR) {
                            case TextureChannel.A:
                                col[i].r = texR.GetPixel((int)res.x,(int)res.y).a;
                                break;
                            case TextureChannel.B:
                                col[i].r = texR.GetPixel((int)res.x,(int)res.y).b;
                                break;
                            case TextureChannel.G:
                                col[i].r = texR.GetPixel((int)res.x,(int)res.y).g;
                                break;
                            default:
                                col[i].r = texR.GetPixel((int)res.x,(int)res.y).r;
                                break;
                        }
                        if (_invR)
                            col[i].r = 1 - col[i].r;
                    } else {
                        col[i].r = !_invR ? 0 : 1;
                    }
                    
                    if (texG != null) {
                        switch (chG) {
                            case TextureChannel.A:
                                col[i].g = texG.GetPixel((int)res.x,(int)res.y).a;
                                break;
                            case TextureChannel.B:
                                col[i].g = texG.GetPixel((int)res.x,(int)res.y).b;
                                break;
                            case TextureChannel.G:
                                col[i].g = texG.GetPixel((int)res.x,(int)res.y).g;
                                break;
                            default:
                                col[i].g = texG.GetPixel((int)res.x,(int)res.y).r;
                                break;
                        }
                        if (_invG)
                            col[i].g = 1 - col[i].g;
                    } else {
                        col[i].g = !_invG ? 0 : 1;
                    }
                    
                    if (texB != null) {
                        switch (chB) {
                            case TextureChannel.A:
                                col[i].b = texB.GetPixel((int)res.x,(int)res.y).a;
                                break;
                            case TextureChannel.B:
                                col[i].b = texB.GetPixel((int)res.x,(int)res.y).b;
                                break;
                            case TextureChannel.G:
                                col[i].b = texB.GetPixel((int)res.x,(int)res.y).g;
                                break;
                            default:
                                col[i].b = texB.GetPixel((int)res.x,(int)res.y).r;
                                break;
                        }
                        if (_invB)
                            col[i].b = 1 - col[i].b;
                    } else {
                        col[i].b = !_invB ? 0 : 1;
                    }
                    
                    if (texA != null) {
                        switch (chA) {
                            case TextureChannel.A:
                                col[i].a = texA.GetPixel((int)res.x,(int)res.y).a;
                                break;
                            case TextureChannel.B:
                                col[i].a = texA.GetPixel((int)res.x,(int)res.y).b;
                                break;
                            case TextureChannel.G:
                                col[i].a = texA.GetPixel((int)res.x,(int)res.y).g;
                                break;
                            default:
                                col[i].a = texA.GetPixel((int)res.x,(int)res.y).r;
                                break;
                        }
                        if (_invA)
                            col[i].a = 1 - col[i].a;
                    } else {
                        col[i].a = !_invA ? 0 : 1;
                    }
                }

                if (channelPreview != ChannelPreview.RGB) {
                    Color[] colPrev = new Color[resolution * resolution];
                    for (int i = 0; i < colPrev.Length; i++) {
                        Vector2 res = new Vector2(i % resolution, i / resolution);
                        colPrev[i] = new Color();
                        switch (channelPreview) {
                            case ChannelPreview.A:
                                if (texA != null) {
                                    switch (chA) {
                                        case TextureChannel.A:
                                            var tA = texA.GetPixel((int)res.x, (int)res.y).a;
                                            if (!_invA) {
                                                colPrev[i].r = tA;
                                                colPrev[i].g = tA;
                                                colPrev[i].b = tA;
                                            } else {
                                                colPrev[i].r = 1 - tA;
                                                colPrev[i].g = 1 - tA;
                                                colPrev[i].b = 1 - tA;
                                            }
                                            break;
                                        case TextureChannel.B:
                                            var tB = texA.GetPixel((int)res.x, (int)res.y).b;
                                            if (!_invA) {
                                                colPrev[i].r = tB;
                                                colPrev[i].g = tB;
                                                colPrev[i].b = tB;
                                            } else {
                                                colPrev[i].r = 1 - tB;
                                                colPrev[i].g = 1 - tB;
                                                colPrev[i].b = 1 - tB;
                                            }
                                            break;
                                        case TextureChannel.G:
                                            var tG = texA.GetPixel((int)res.x, (int)res.y).g;
                                            if (!_invA) {
                                                colPrev[i].r = tG;
                                                colPrev[i].g = tG;
                                                colPrev[i].b = tG;
                                            } else {
                                                colPrev[i].r = 1 - tG;
                                                colPrev[i].g = 1 - tG;
                                                colPrev[i].b = 1 - tG;
                                            }
                                            break;
                                        case TextureChannel.R:
                                            var tR = texA.GetPixel((int)res.x, (int)res.y).r;
                                            if (!_invA) {
                                                colPrev[i].r = tR;
                                                colPrev[i].g = tR;
                                                colPrev[i].b = tR;
                                            } else {
                                                colPrev[i].r = 1 - tR;
                                                colPrev[i].g = 1 - tR;
                                                colPrev[i].b = 1 - tR;
                                            }
                                            break;
                                    }
                                } else {
                                    colPrev[i] = !_invA ? Color.black : Color.white;
                                }
                                break;
                            case ChannelPreview.B:
                                if (texB != null) {
                                    switch (chB) {
                                        case TextureChannel.A:
                                            var tA = texB.GetPixel((int)res.x, (int)res.y).a;
                                            if (!_invB) {
                                                colPrev[i].r = tA;
                                                colPrev[i].g = tA;
                                                colPrev[i].b = tA;
                                            } else {
                                                colPrev[i].r = 1 - tA;
                                                colPrev[i].g = 1 - tA;
                                                colPrev[i].b = 1 - tA;
                                            }
                                            break;
                                        case TextureChannel.B:
                                            var tB = texB.GetPixel((int)res.x, (int)res.y).b;
                                            if (!_invB) {
                                                colPrev[i].r = tB; 
                                                colPrev[i].g = tB;
                                                colPrev[i].b = tB;
                                            } else {
                                                colPrev[i].r = 1 - tB;
                                                colPrev[i].g = 1 - tB;
                                                colPrev[i].b = 1 - tB;
                                            }
                                            break;
                                        case TextureChannel.G:
                                            var tG = texB.GetPixel((int)res.x, (int)res.y).g;
                                            if (!_invB) {
                                                colPrev[i].r = tG;
                                                colPrev[i].g = tG;
                                                colPrev[i].b = tG;
                                            } else {
                                                colPrev[i].r = 1 - tG;
                                                colPrev[i].g = 1 - tG;
                                                colPrev[i].b = 1 - tG;
                                            }
                                            break;
                                        case TextureChannel.R:
                                            var tR = texB.GetPixel((int)res.x, (int)res.y).r;
                                            if (!_invB) {
                                                colPrev[i].r = tR;
                                                colPrev[i].g = tR;
                                                colPrev[i].b = tR;
                                            } else {
                                                colPrev[i].r = 1 - tR;
                                                colPrev[i].g = 1 - tR;
                                                colPrev[i].b = 1 - tR;
                                            }
                                            break;
                                    }
                                }else {
                                    colPrev[i] = !_invB ? Color.black : Color.white;
                                }
                                break;
                            case ChannelPreview.G:
                                if (texG != null) {
                                    switch (chG) {
                                        case TextureChannel.A:
                                            var tA = texG.GetPixel((int)res.x, (int)res.y).a;
                                            if (!_invG) {
                                                colPrev[i].r = tA;
                                                colPrev[i].g = tA;
                                                colPrev[i].b = tA;
                                            } else {
                                                colPrev[i].r = 1 - tA;
                                                colPrev[i].g = 1 - tA;
                                                colPrev[i].b = 1 - tA;
                                            }
                                            break;
                                        case TextureChannel.B:
                                            var tB = texG.GetPixel((int)res.x, (int)res.y).b;
                                            if (!_invG) {
                                                colPrev[i].r = tB;
                                                colPrev[i].g = tB;
                                                colPrev[i].b = tB;
                                            } else {
                                                colPrev[i].r = 1 - tB;
                                                colPrev[i].g = 1 - tB;
                                                colPrev[i].b = 1 - tB;
                                            }
                                            break;
                                        case TextureChannel.G:
                                            var tG = texG.GetPixel((int)res.x, (int)res.y).g;
                                            if (!_invG) {
                                                colPrev[i].r = tG;
                                                colPrev[i].g = tG;
                                                colPrev[i].b = tG;
                                            } else {
                                                colPrev[i].r = 1 - tG;
                                                colPrev[i].g = 1 - tG;
                                                colPrev[i].b = 1 - tG;
                                            }
                                            break;
                                        case TextureChannel.R:
                                            var tR = texG.GetPixel((int)res.x, (int)res.y).r;
                                            if (!_invG) {
                                                colPrev[i].r = tR;
                                                colPrev[i].g = tR;
                                                colPrev[i].b = tR;
                                            } else {
                                                colPrev[i].r = 1 - tR;
                                                colPrev[i].g = 1 - tR;
                                                colPrev[i].b = 1 - tR;
                                            }
                                            break;
                                    }
                                }else {
                                    colPrev[i] = !_invG ? Color.black : Color.white;
                                }
                                break;
                            case ChannelPreview.R:
                                if (texR != null) {
                                    switch (chR) {
                                        case TextureChannel.A:
                                            var tA = texR.GetPixel((int)res.x, (int)res.y).a;
                                            if (!_invR) {
                                                colPrev[i].r = tA;
                                                colPrev[i].g = tA;
                                                colPrev[i].b = tA;
                                            } else {
                                                colPrev[i].r = 1 - tA;
                                                colPrev[i].g = 1 - tA;
                                                colPrev[i].b = 1 - tA;
                                            }
                                            break;
                                        case TextureChannel.B:
                                            var tB = texR.GetPixel((int)res.x, (int)res.y).b;
                                            if (!_invR) {
                                                colPrev[i].r = tB;
                                                colPrev[i].g = tB;
                                                colPrev[i].b = tB;
                                            } else {
                                                colPrev[i].r = 1 - tB;
                                                colPrev[i].g = 1 - tB;
                                                colPrev[i].b = 1 - tB;
                                            }
                                            break;
                                        case TextureChannel.G:
                                            var tG = texR.GetPixel((int)res.x, (int)res.y).g;
                                            if (!_invR) {
                                                colPrev[i].r = tG;
                                                colPrev[i].g = tG;
                                                colPrev[i].b = tG;
                                            } else {
                                                colPrev[i].r = 1 - tG;
                                                colPrev[i].g = 1 - tG;
                                                colPrev[i].b = 1 - tG;
                                            }
                                            break;
                                        case TextureChannel.R:
                                            var tR = texR.GetPixel((int)res.x, (int)res.y).r;
                                            if (!_invR) {
                                                colPrev[i].r = tR;
                                                colPrev[i].g = tR;
                                                colPrev[i].b = tR;
                                            } else {
                                                colPrev[i].r = 1 - tR;
                                                colPrev[i].g = 1 - tR;
                                                colPrev[i].b = 1 - tR;
                                            }
                                            break;
                                    }
                                }
                                else {
                                    colPrev[i] = !_invR ? Color.black : Color.white;
                                }
                                break;
                        }
                    }
                    col = colPrev;
                } 
            }
            return col;
        }

        Color[] SamplePreview(int ch) {
            Color[] col = new Color[texPreview.width * texPreview.height];
            Texture2D tex = CopyTexture(texPreview);
            for (int i = 0; i < col.Length; i++) {
                col[i] = new Color();
                Vector2 res = new Vector2(i % texPreview.width, i / texPreview.height);
                if (tex != null){
                    switch (ch) {
                        case 3:
                            col[i].r = tex.GetPixel((int)res.x,(int)res.y).a;
                            col[i].g = tex.GetPixel((int)res.x,(int)res.y).a;
                            col[i].b = tex.GetPixel((int)res.x,(int)res.y).a;
                            break;
                        case 2:
                            col[i].r = tex.GetPixel((int)res.x,(int)res.y).b;
                            col[i].g = tex.GetPixel((int)res.x,(int)res.y).b;
                            col[i].b = tex.GetPixel((int)res.x,(int)res.y).b;
                            break;
                        case 1:
                            col[i].r = tex.GetPixel((int)res.x,(int)res.y).g;
                            col[i].g = tex.GetPixel((int)res.x,(int)res.y).g;
                            col[i].b = tex.GetPixel((int)res.x,(int)res.y).g;
                            break;
                        default:
                            col[i].r = tex.GetPixel((int)res.x,(int)res.y).r;
                            col[i].g = tex.GetPixel((int)res.x,(int)res.y).r;
                            col[i].b = tex.GetPixel((int)res.x,(int)res.y).r;
                            break;
                    }
                }
            }
            return col;
        }
        
        Texture2D CopyTexture(Texture2D texture) {
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0
            );
            UnityEngine.Graphics.Blit(texture,tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
            return myTexture2D;
        }

        string SelectChannel(int index) {
            switch (index) {
                case 3:
                    _channelName = "A";
                    break;
                case 2:
                    _channelName = "B";
                    break;
                case 1:
                    _channelName = "G";
                    break;
                default:
                    _channelName = "R";
                    break;
            }
            return _channelName;
        }
        
        string Path() {
            string path;
            if (exportPath != "") {
                path = exportPath;
            } else {
                path = "Assets/";
            }
            return path;
        }
        
        string SamplePath(Texture2D tex) {
            var path = AssetDatabase.GetAssetPath(tex);
            if (path == "") {
                path = "Assets";
            }else if (System.IO.Path.GetExtension(path) != "") {
                path = path.Replace(System.IO.Path.GetFileName(path), "");
            }
            AssetDatabase.Refresh();
            return path;
        }

        void PackTextures() {
            int res = GetSampleMaxResolution();
            var packedTexture = new Texture2D(res,res);
            packedTexture.SetPixels(SampleTextures(res));

            byte[] tex = packedTexture.EncodeToPNG();
            var path = Path() + texName + ".png";
            
            FileStream stream = new (path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryWriter writer = new (stream);
            for (int i = 0; i < tex.Length; i++)
                writer.Write(tex[i]);
            
            stream.Close();
            writer.Close();
            
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
        }
        
        void UnpackTextures() {
            var unpackedTexture = new Texture2D(texPreview.width, texPreview.height);

            for (int i = 0; i < 4; i++) {
                unpackedTexture.SetPixels(SamplePreview(i));
            
                byte[] tex = unpackedTexture.EncodeToPNG();
                var path = Path() + texName + "_Unpacked_" + SelectChannel(i) + ".png";

                FileStream stream = new (path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                BinaryWriter writer = new (stream);
                for (int j = 0; j < tex.Length; j++)
                    writer.Write(tex[j]);
            
                stream.Close();
                writer.Close();
                
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
            }
        }
        
        void Clean() {
            if (texSampleR == null) {
                texSampleRsize = new Vector2(0, 0);
                chR = TextureChannel.R;
            }
            if (texSampleG == null){
                texSampleGsize = new Vector2(0, 0);
                chG = TextureChannel.R;
            }
            if (texSampleB == null) {
                texSampleBsize = new Vector2(0, 0);
                chB = TextureChannel.R;
            }
            if (texSampleA == null){
                texSampleAsize = new Vector2(0, 0);
                chA = TextureChannel.R;
            }
        }
        #endregion
    }
}
