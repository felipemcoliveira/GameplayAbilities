using UnityEditor;
using UnityEngine;

internal static class GameplayTagEditorGUI
{
   internal static Color BorderColor => EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.09f) : new Color(0, 0, 0, 0.16f);

   private static GUIStyle s_ValueStyle;
   private static GUIStyle s_ButtonStyle;
   private static GUIStyle s_ButtonLabelStyle;

   internal static GUIStyle ValueStyle => s_ValueStyle ??= new GUIStyle(EditorStyles.label)
   {
      alignment = TextAnchor.MiddleLeft,
      clipping = TextClipping.Clip,
      padding = new RectOffset(0, 0, 0, 0)
   };

   private static GUIStyle ButtonStyle => s_ButtonStyle ??= new GUIStyle(GUI.skin.button)
   {
      fixedHeight = 0,
      fixedWidth = 0,
      alignment = TextAnchor.MiddleCenter,
      contentOffset = Vector2.zero,
      padding = new RectOffset(0, 0, 0, 0)
   };

   private static GUIStyle ButtonLabelStyle => s_ButtonLabelStyle ??= new GUIStyle(ValueStyle)
   {
      alignment = TextAnchor.MiddleCenter
   };

   internal static void DrawIcon(Rect rect, string name)
   {
      if (Event.current.type != EventType.Repaint || rect.width <= 0 || rect.height <= 0)
         return;

      Texture2D icon = GameplayTagEditorIcons.Get(name);
      Color color = GUI.color;
      bool dark = EditorGUIUtility.isProSkin;
      Color tint = dark ? new Color(0.85f, 0.88f, 0.92f) : new Color(0.23f, 0.26f, 0.30f);
      if (name == "warning")
         tint = dark ? new Color(1f, 0.76f, 0.32f) : new Color(0.64f, 0.37f, 0.04f);
      if (!GUI.enabled)
         tint.a *= 0.55f;

      GUI.color = color * tint;
      GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
      GUI.color = color;
   }

   internal static bool IconButton(Rect rect, string name, string tooltip)
   {
      bool pressed = GUI.Button(rect, new GUIContent(string.Empty, tooltip), ButtonStyle);
      float size = Mathf.Min(16, Mathf.Min(rect.width, rect.height) - 6);
      DrawIcon(new Rect(rect.center.x - size / 2, rect.center.y - size / 2, size, size), name);
      return pressed;
   }

   internal static bool Button(Rect rect, string text, string name, string tooltip)
   {
      const float k_Gap = 6;
      bool pressed = GUI.Button(rect, new GUIContent(string.Empty, tooltip), ButtonStyle);
      float size = Mathf.Min(20, rect.height - 4);
      GUIContent content = new(text, tooltip);
      float textWidth = Mathf.Min(ButtonLabelStyle.CalcSize(content).x, Mathf.Max(0, rect.width - size - k_Gap - 16));
      float contentX = rect.center.x - (size + k_Gap + textWidth) / 2;
      DrawIcon(new Rect(contentX, rect.center.y - size / 2, size, size), name);
      GUI.Label(new Rect(contentX + size + k_Gap, rect.y, textWidth, rect.height), content, ButtonLabelStyle);
      return pressed;
   }

   internal static void Panel(Rect rect)
   {
      GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
   }

   internal static void Separator(Rect rect)
   {
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), BorderColor);
   }
}
