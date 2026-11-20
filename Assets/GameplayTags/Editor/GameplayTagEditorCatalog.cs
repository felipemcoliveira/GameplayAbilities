using System;
using System.Collections.Generic;
using UnityEditor;

internal static class GameplayTagEditorCatalog
{
   internal static event Action Changed;

   private static GameplayTagLibrary[] s_Libraries;
   private static string[] s_LibraryPaths = Array.Empty<string>();

   internal static GameplayTag[] GetTags() => GameplayTagManager.GetRegisteredTags();
   internal static GameplayTag[] GetUnresolvedTags() => GameplayTagManager.GetTemporaryTags();

   internal static GameplayTagLibrary[] GetLibraries()
   {
      if (s_Libraries == null)
         FindLibraries();

      return s_Libraries;
   }

   internal static bool IsLibraryPath(string path)
   {
      return path.Contains("/Resources/GameplayTags/", StringComparison.Ordinal);
   }

   internal static void Refresh()
   {
      FindLibraries();
      if (!EditorApplication.isPlayingOrWillChangePlaymode)
      {
         List<GameplayTagLibrary> sources = new();
         for (int i = 0; i < s_Libraries.Length; i++)
         {
            if (IsLibraryPath(s_LibraryPaths[i]))
               sources.Add(s_Libraries[i]);
         }

         GameplayTagManager.ReloadEditorSources(sources.ToArray());
      }

      Changed?.Invoke();
   }

   internal static bool ContainsLibrary(string[] paths)
   {
      foreach (string path in paths)
      {
         if (IsLibraryPath(path) || AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(GameplayTagLibrary))
            return true;
      }

      return false;
   }

   internal static bool HadLibrary(string[] paths)
   {
      foreach (string path in paths)
      {
         foreach (string libraryPath in s_LibraryPaths)
         {
            if (libraryPath == path || libraryPath.StartsWith(path + "/", StringComparison.Ordinal))
               return true;
         }
      }

      return false;
   }

   private static void FindLibraries()
   {
      string[] guids = AssetDatabase.FindAssets("t:GameplayTagLibrary");
      string[] paths = Array.ConvertAll(guids, AssetDatabase.GUIDToAssetPath);
      Array.Sort(paths, StringComparer.Ordinal);

      List<GameplayTagLibrary> libraries = new(paths.Length);
      List<string> libraryPaths = new(paths.Length);
      foreach (string path in paths)
      {
         GameplayTagLibrary library = AssetDatabase.LoadAssetAtPath<GameplayTagLibrary>(path);
         if (library != null)
         {
            libraries.Add(library);
            libraryPaths.Add(path);
         }
      }

      s_Libraries = libraries.ToArray();
      s_LibraryPaths = libraryPaths.ToArray();
   }
}
