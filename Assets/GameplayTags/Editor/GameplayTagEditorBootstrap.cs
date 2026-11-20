using UnityEditor;

[InitializeOnLoad]
internal sealed class GameplayTagEditorBootstrap : AssetPostprocessor
{
   static GameplayTagEditorBootstrap()
   {
      Undo.undoRedoPerformed += GameplayTagEditorCatalog.Refresh;
      EditorApplication.playModeStateChanged += OnPlayModeChanged;
   }

   private static void OnPostprocessAllAssets(
      string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
   {
      // Source initialization reads assets, so wait for the import pass after script reload.
      if (didDomainReload ||
          GameplayTagEditorCatalog.ContainsLibrary(importedAssets) ||
          GameplayTagEditorCatalog.ContainsLibrary(movedAssets) ||
          GameplayTagEditorCatalog.HadLibrary(deletedAssets) ||
          GameplayTagEditorCatalog.HadLibrary(movedFromAssetPaths))
         GameplayTagEditorCatalog.Refresh();
   }

   private static void OnPlayModeChanged(PlayModeStateChange state)
   {
      if (state == PlayModeStateChange.EnteredEditMode)
         GameplayTagEditorCatalog.Refresh();
   }
}
