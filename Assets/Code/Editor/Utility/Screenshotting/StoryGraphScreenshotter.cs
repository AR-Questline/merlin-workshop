using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Main.Stories;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using XNode;
using XNodeEditor;
using Color = UnityEngine.Color;

namespace Awaken.TG.Editor.Utility.Screenshotting {
    public static class StoryGraphScreenshotter {

        const int BorderOffset = 5;
        const int TopTabOffset = 50;
        const int NodeSafetySpace = 75;

        static bool s_isScreenshotting;

        public static async void TakeScreenshot(StoryGraphEditor nodeEditor) {
            if (s_isScreenshotting) {
                return;
            }

            s_isScreenshotting = true;

            try {
                bool blackboardState = nodeEditor.blackBoardFolded;
                nodeEditor.blackBoardFolded = true;

                float zoom = nodeEditor.window.zoom;
                nodeEditor.window.zoom = 1;

                Vector2 panOffset = nodeEditor.window.panOffset;

                await Task.Yield();
                try {
                    await StartScreenshotting(nodeEditor);
                } catch (Exception e) {
                    Debug.LogException(e);
                }

                nodeEditor.blackBoardFolded = blackboardState;
                nodeEditor.window.zoom = zoom;
                nodeEditor.window.panOffset = panOffset;
            } finally {
                s_isScreenshotting = false;
            }
        }        
        
        static async Task StartScreenshotting(StoryGraphEditor nodeEditor) {
            RectInt graphRect = GetGraphRect(nodeEditor.target, nodeEditor.window);

            Vector2 windowSize = nodeEditor.window.position.size;
            Vector2Int singleScreenshotSize = new ((int) windowSize.x - BorderOffset*2, 
                (int) windowSize.y - BorderOffset - TopTabOffset);
            Vector2 screenshotPosition = nodeEditor.window.position.position + new Vector2(BorderOffset, TopTabOffset);

            int xShots = Mathf.CeilToInt(graphRect.width / (singleScreenshotSize.x*1f));
            int yShots = Mathf.CeilToInt(graphRect.height / (singleScreenshotSize.y*1f));
            
            Bitmap result = GetTexture(xShots * singleScreenshotSize.x, yShots * singleScreenshotSize.y);

            Vector2Int screenshotModulo = GetScreenshotModulo(graphRect, result.Width, result.Height);
            Vector2Int startScreenshotPos = new (graphRect.xMin + singleScreenshotSize.x / 2 - screenshotModulo.x/2,
                graphRect.yMin + singleScreenshotSize.y / 2 - screenshotModulo.y/2);

            for (int x = 0; x<xShots; x++) {
                for (int y = 0; y<yShots; y++) {
                    var graphPosition = startScreenshotPos +
                                        new Vector2Int(x * singleScreenshotSize.x, y * singleScreenshotSize.y);
                    nodeEditor.window.panOffset = new Vector2(-graphPosition.x, graphPosition.y);
                    nodeEditor.wasRepaintedForScreenshot = false;
                    nodeEditor.window.Repaint();
                    while (!nodeEditor.wasRepaintedForScreenshot) {
                        await Task.Yield();
                    }
                    await Task.Yield();

                    AddScreenshotToResult(screenshotPosition, singleScreenshotSize, result, x * singleScreenshotSize.x,
                        y * singleScreenshotSize.y);
                }
            }

            SaveTexture(result, nodeEditor);
        }

        static Bitmap GetTexture(int x, int y) {
            try {
                return new Bitmap(x, y);
            } catch (Exception) {
                return new Bitmap(x, y, PixelFormat.Format4bppIndexed);
            }
        }

        static RectInt GetGraphRect(NodeGraph graph, NodeEditorWindow nodeEditorWindow) {
            int minX = (int)graph.nodes.Select(n => n.position.x)
                .Min() - NodeSafetySpace;
            int maxX = (int)graph.nodes.Select(n => n.position.x + nodeEditorWindow.nodeSizes[n].x)
                .Max() + NodeSafetySpace;
            int minY = -(int)graph.nodes.Select(n => n.position.y + nodeEditorWindow.nodeSizes[n].y)
                .Max() - NodeSafetySpace;
            int maxY = -(int)graph.nodes.Select(n => n.position.y)
                .Min() + NodeSafetySpace;

            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }

        static Vector2Int GetScreenshotModulo(RectInt graphRect, int screenshotWidth, int screenshotHeight) {
            int xModulo = screenshotWidth % graphRect.width;
            int yModulo = screenshotHeight % graphRect.height;
            return new Vector2Int(xModulo, yModulo);
        }

        static Color[] TakeSingleScreenshot(Vector2 position, int sizeX, int sizeY) {
            return InternalEditorUtility.ReadScreenPixel(position, sizeX, sizeY);
        }

        static void AddScreenshotToResult(Vector2 screenshotPosition, Vector2Int singleScreenshotSize,
            Bitmap result, int tx, int ty) {
            
            Color[] screenshot = TakeSingleScreenshot(screenshotPosition, singleScreenshotSize.x,
                singleScreenshotSize.y);

            int colorIndex = 0;
            for (int y = 0; y < singleScreenshotSize.y; y++) {
                for (int x = 0; x < singleScreenshotSize.x; x++) {
                    result.SetPixel(tx+x, ty+y, screenshot[colorIndex].ToSystemColor());
                    colorIndex++;
                }
            }
        }

        static void SaveTexture(Bitmap texture, NodeGraphEditor owner) {
            texture.RotateFlip(RotateFlipType.RotateNoneFlipY);
            var bytes = ImageToByte(texture);

            if (TryGetPath(owner, out string path)) {
                File.WriteAllBytes(path, bytes);
            }
        }
        
        static bool TryGetPath(NodeGraphEditor editor, out string path) {
            var timestamp = DateTime.Now;
            var defaultName =
                $"{editor.target.name}_{timestamp.Year}-{timestamp.Month:00}-{timestamp.Day:00}_{timestamp.Hour:00}-{timestamp.Minute:00}";

            path = EditorUtility.SaveFilePanel("Save screenshot", AssetPaths.GetSelectedPathOrFallback(), defaultName, "png");
            return !string.IsNullOrEmpty(path);
        }
        
        static byte[] ImageToByte(Image img)
        {
            var converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}