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

        private static Queue<GameObject> items = new Queue<GameObject>();

        private static GameObject canvas;
        public static void Create()
        {
            if (canvas != null) return;
            // Create base canvas
            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            Object.DontDestroyOnLoad(canvas);

            CanvasUtil.CreateTextPanel(canvas, "Recent Items", 24, TextAnchor.MiddleCenter,
                new CanvasUtil.RectData(new Vector2(200, 100), Vector2.zero,
                new Vector2(0.87f, 0.95f), new Vector2(0.87f, 0.95f)));

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

        public static void AddItem(string item, string msg)
        {
            if (canvas == null)
            {
                Create();
            }

            string name = LanguageStringManager.GetLanguageString(LogicManager.GetItemDef(item).nameKey, "UI");
            if (msg != "")
            {
                msg = $"{name}\n{msg}";
            }
            else
            {
                msg = name;
            }

            GameObject basePanel = CanvasUtil.CreateBasePanel(canvas,
                new CanvasUtil.RectData(new Vector2(200, 50), Vector2.zero,
                new Vector2(0.9f, 0.9f), new Vector2(0.9f, 0.9f)));

            string spriteKey = LogicManager.GetItemDef(item).shopSpriteKey;
            CanvasUtil.CreateImagePanel(basePanel, RandomizerMod.GetSprite(spriteKey),
                new CanvasUtil.RectData(new Vector2(50, 50), Vector2.zero, new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f)));
            CanvasUtil.CreateTextPanel(basePanel, msg, 24, TextAnchor.MiddleLeft,
                new CanvasUtil.RectData(new Vector2(400, 100), Vector2.zero,
                new Vector2(1.2f, 0.5f), new Vector2(1.2f, 0.5f)),
                CanvasUtil.GetFont("Perpetua"));

            items.Enqueue(basePanel);
            if (items.Count > MaxItems)
            {
                Object.DestroyImmediate(items.Dequeue());
            }

            UpdatePositions();
        }

        private static void UpdatePositions()
        {
            int i = items.Count - 1;
            foreach (GameObject item in items)
            {
                Vector2 newPos = new Vector2(0.9f, 0.9f - 0.06f * i--);
                item.GetComponent<RectTransform>().anchorMin = newPos;
                item.GetComponent<RectTransform>().anchorMax = newPos;
            }
        }

        public static void Show()
        {
            if (canvas == null) return;
            canvas.SetActive(true);
        }

        public static void Hide()
        {
            if (canvas == null) return;
            canvas.SetActive(false);
        }
    }
}
