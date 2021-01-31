using System.Collections;
using System.Collections.Generic;
using Modding;
using RandomizerLib;
using UnityEngine;
using static RandomizerMod.LogHelper;

namespace RandomizerMod.Components
{
    static internal class RecentItems
    {
        public static int MaxItems = 5;

        private static List<GameObject> items = new List<GameObject>();

        private static GameObject canvas;
        public static void Create()
        {
            if (canvas != null) return;
            // Create base canvas
            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            Object.DontDestroyOnLoad(canvas);

            CanvasUtil.CreateTextPanel(canvas, "Recent Items", 24, TextAnchor.MiddleCenter,
                new CanvasUtil.RectData(new Vector2(200, 100), Vector2.zero,
                new Vector2(0.9f, 0.95f), new Vector2(0.9f, 0.95f)));

            canvas.SetActive(true);
        }

        public static void Destroy()
        {
            if (canvas != null) Object.DestroyImmediate(canvas);
            canvas = null;

            foreach (GameObject item in items)
            {
                Object.DestroyImmediate(item);
            }

            items.Clear();
        }

        public static void AddItem(string item, string from)
        {
            if (canvas == null)
            {
                Create();
            }

            GameObject basePanel = CanvasUtil.CreateBasePanel(canvas,
                new CanvasUtil.RectData(new Vector2(200, 50), Vector2.zero,
                new Vector2(0.9f, 0.9f), new Vector2(0.9f, 0.9f)));

            string spriteKey = LogicManager.GetItemDef(item).shopSpriteKey;
            CanvasUtil.CreateImagePanel(basePanel, RandomizerMod.GetSprite(spriteKey),
                new CanvasUtil.RectData(new Vector2(50, 50), Vector2.zero, new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f)));
            CanvasUtil.CreateTextPanel(basePanel, $"from {from}", 24, TextAnchor.MiddleLeft,
                new CanvasUtil.RectData(new Vector2(400, 100), Vector2.zero,
                new Vector2(1.2f, 0.5f), new Vector2(1.2f, 0.5f)),
                CanvasUtil.GetFont("Perpetua"));

            items.Insert(0, basePanel);
            if (items.Count > MaxItems)
            {
                Object.DestroyImmediate(items[items.Count - 1]);
                items.RemoveAt(items.Count - 1);
            }

            UpdatePositions();
        }

        private static void UpdatePositions()
        {
            for (int i = 0; i < items.Count; i++)
            {
                Vector2 newPos = new Vector2(0.9f, 0.9f - 0.05f * i);
                items[i].GetComponent<RectTransform>().anchorMin = newPos;
                items[i].GetComponent<RectTransform>().anchorMax = newPos;
            }
        }

        public static void Show()
        {
            if (canvas == null)
            {
                Create();
            }
            canvas.SetActive(true);
        }

        public static void Hide()
        {
            if (canvas == null)
            {
                Create();
            }
            canvas.SetActive(false);
        }
    }
}
