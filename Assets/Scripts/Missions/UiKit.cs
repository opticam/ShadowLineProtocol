using UnityEngine;

namespace ShadowLineProtocol
{
    // Shared IMGUI styles and helpers for the mission screens. Call Ensure() at the top of OnGUI.
    public static class UiKit
    {
        public static GUIStyle Text, Small, Wrap, Title, BigTitle, Button, ButtonLeft, Label;

        public static void Ensure()
        {
            if (Text != null) return;
            Text = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = false };
            Small = new GUIStyle(Text) { fontSize = 12 };
            Wrap = new GUIStyle(Small) { wordWrap = true };
            Title = new GUIStyle(Text) { fontSize = 16, fontStyle = FontStyle.Bold };
            BigTitle = new GUIStyle(Text) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Button = new GUIStyle(GUI.skin.button) { fontSize = 13, richText = true };
            ButtonLeft = new GUIStyle(Button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 8, 4, 4) };
            Label = new GUIStyle(Small) { alignment = TextAnchor.MiddleCenter };
        }

        // Drawn twice so the default skin's translucent box reads as solid.
        public static void Panel(Rect rect)
        {
            GUI.Box(rect, GUIContent.none);
            GUI.Box(rect, GUIContent.none);
        }

        public static void Dim(float alpha)
        {
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void Bar(Rect rect, float fill01, Color color)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill01), rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void WorldLabel(Camera cam, Vector3 worldPosition, string content, Color color)
        {
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f) return;
            var guiContent = new GUIContent(content);
            Vector2 size = Label.CalcSize(guiContent);
            var rect = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y / 2f, size.x, size.y);
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), guiContent, Label);
            GUI.color = color;
            GUI.Label(rect, guiContent, Label);
            GUI.color = Color.white;
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}
