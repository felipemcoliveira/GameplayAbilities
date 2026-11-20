using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Small monochrome shapes for the editor controls, independent of Unity's internal icon names.
internal static class GameplayTagEditorIcons
{
   private const int k_Size = 32;
   private static readonly Dictionary<string, Texture2D> s_Textures = new();

   static GameplayTagEditorIcons()
   {
      AssemblyReloadEvents.beforeAssemblyReload += DisposeTextures;
   }

   internal static Texture2D Get(string name)
   {
      if (s_Textures.TryGetValue(name, out Texture2D cached))
         return cached;

      Color32[] pixels = new Color32[k_Size * k_Size];
      switch (name)
      {
         case "add":
            Stroke(pixels, new(6, 12), new(18, 12));
            Stroke(pixels, new(12, 6), new(12, 18));
            break;
         case "remove":
            Stroke(pixels, new(6, 12), new(18, 12));
            break;
         case "clear":
            Stroke(pixels, new(7, 7), new(17, 17));
            Stroke(pixels, new(17, 7), new(7, 17));
            break;
         case "edit":
            Stroke(pixels, new(5, 19), new(6, 14), new(16, 4), new(20, 8), new(10, 18), new(5, 19));
            Stroke(pixels, new(13, 7), new(17, 11));
            break;
         case "library":
            Stroke(pixels, new(3, 18), new(3, 6), new(10, 6), new(12, 8), new(21, 8), new(21, 18), new(3, 18));
            Stroke(pixels, new(3, 10), new(21, 10));
            break;
         case "warning":
            Stroke(pixels, new(12, 4), new(21, 19), new(3, 19), new(12, 4));
            Stroke(pixels, new(12, 9), new(12, 13));
            Stroke(pixels, new(12, 16), new(12, 16));
            break;
         default:
            Stroke(pixels, new(4, 10), new(4, 4), new(10, 4), new(21, 15), new(15, 21), new(4, 10));
            Stroke(pixels, new(8, 8), new(8, 8));
            break;
      }

      Texture2D texture = new(k_Size, k_Size, TextureFormat.RGBA32, mipChain: false)
      {
         name = "Gameplay Tags " + name,
         hideFlags = HideFlags.HideAndDontSave,
         wrapMode = TextureWrapMode.Clamp
      };
      texture.SetPixels32(pixels);
      texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
      s_Textures.Add(name, texture);
      return texture;
   }

   private static void DisposeTextures()
   {
      foreach (Texture2D texture in s_Textures.Values)
         Object.DestroyImmediate(texture);

      s_Textures.Clear();
   }

   private static void Stroke(Color32[] pixels, params Vector2[] points)
   {
      const float k_Scale = k_Size / 24f;
      const float k_HalfWidth = 0.95f * k_Scale;
      for (int segment = 1; segment < points.Length; segment++)
      {
         Vector2 start = points[segment - 1] * k_Scale;
         Vector2 end = points[segment] * k_Scale;
         Vector2 direction = end - start;
         float lengthSquared = direction.sqrMagnitude;
         for (int y = 0; y < k_Size; y++)
         {
            for (int x = 0; x < k_Size; x++)
            {
               Vector2 offset = new Vector2(x + 0.5f, y + 0.5f) - start;
               float fraction = lengthSquared > 0 ? Mathf.Clamp01(Vector2.Dot(offset, direction) / lengthSquared) : 0;
               float distance = (offset - direction * fraction).magnitude;
               byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(k_HalfWidth + 0.5f - distance) * 255);
               int index = (k_Size - 1 - y) * k_Size + x;
               if (alpha > pixels[index].a)
                  pixels[index] = new Color32(255, 255, 255, alpha);
            }
         }
      }
   }
}
