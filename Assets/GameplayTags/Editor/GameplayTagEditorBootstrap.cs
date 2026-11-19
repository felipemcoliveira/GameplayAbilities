using UnityEditor;

internal sealed class GameplayTagEditorBootstrap : AssetPostprocessor
{
   private static void OnPostprocessAllAssets(
      string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
   {
      // Source initialization reads assets, so wait for the import pass after script reload.
      if (didDomainReload)
         GameplayTagManager.Initialize();
   }
}
