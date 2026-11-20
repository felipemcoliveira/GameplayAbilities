using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GameplayTagEditorCatalogTests
{
   private string m_Folder;

   [SetUp]
   public void CreateTestFolder()
   {
      string name = "GameplayTagEditorTests_" + Guid.NewGuid().ToString("N");
      AssetDatabase.CreateFolder("Assets", name);
      m_Folder = "Assets/" + name;
      AssetDatabase.CreateFolder(m_Folder, "Resources");
      AssetDatabase.CreateFolder(m_Folder + "/Resources", "GameplayTags");
   }

   [TearDown]
   public void RemoveTestFolder()
   {
      AssetDatabase.DeleteAsset(m_Folder);
      GameplayTagEditorCatalog.Refresh();
   }

   [Test]
   public void MovingLibraryIntoResourcesConfirmsItsNamesInEditMode()
   {
      const string k_Name = "EditorTests.AssetSource.Mark";
      const string k_Comment = "Applied by the fixture.";
      string outside = m_Folder + "/Library.asset";
      string inside = m_Folder + "/Resources/GameplayTags/Library.asset";
      GameplayTagLibrary library = CreateLibrary(outside, k_Name, k_Comment);

      Assert.That(GameplayTagEditorCatalog.GetLibraries(), Does.Contain(library));
      Assert.That(GameplayTagManager.TryGetTag(k_Name, out _), Is.False);

      Assert.That(AssetDatabase.MoveAsset(outside, inside), Is.Empty);
      Assert.That(GameplayTagManager.TryGetTag(k_Name, out GameplayTag mark), Is.True);
      Assert.That(GameplayTagEditorCatalog.GetTags(), Does.Contain(mark.Parent));
      Assert.That(GameplayTagManager.GetMetadata(mark).Comments, Does.Contain(k_Comment));
   }

   [Test]
   public void RefreshPreservesRecoveredNamesWhileLibraryEntriesAreRenamedAndRestored()
   {
      const string k_Saved = "EditorTests.Recovery.OldName";
      const string k_Renamed = "EditorTests.Recovery.NewName";
      string path = m_Folder + "/Resources/GameplayTags/Library.asset";
      GameplayTagLibrary library = CreateLibrary(path, k_Saved, "Original");
      Assert.That(GameplayTagManager.TryRegisterSerializedTag(k_Saved, out GameplayTag saved), Is.True);

      WriteEntry(library, k_Renamed, "Updated");
      GameplayTagEditorCatalog.Refresh();

      Assert.That(GameplayTagManager.TryGetTag(k_Renamed, out GameplayTag renamed), Is.True);
      Assert.That(GameplayTagManager.GetMetadata(renamed).Comments, Does.Contain("Updated"));
      Assert.That(GameplayTagEditorCatalog.GetUnresolvedTags(), Does.Contain(saved));

      WriteEntry(library, k_Saved, "Restored");
      GameplayTagEditorCatalog.Refresh();

      Assert.That(GameplayTagEditorCatalog.GetTags(), Does.Contain(saved));
      Assert.That(GameplayTagEditorCatalog.GetUnresolvedTags(), Has.No.Member(saved));
   }

   [Test]
   public void InvalidLibraryEntryCanBeRepairedInEditMode()
   {
      const string k_Valid = "EditorTests.Repair.Fixed";
      string path = m_Folder + "/Resources/GameplayTags/Library.asset";
      GameplayTagLibrary library = CreateLibrary(path, "EditorTests..Malformed", string.Empty);

      Assert.That(GameplayTagEditorCatalog.GetLibraries(), Does.Contain(library));
      Assert.That(GameplayTagManager.TryGetTag("EditorTests..Malformed", out _), Is.False);
      Assert.That(GameplayTagTestTags.Flux.IsEmpty, Is.False);

      WriteEntry(library, k_Valid, string.Empty);
      GameplayTagEditorCatalog.Refresh();

      Assert.That(GameplayTagManager.TryGetTag(k_Valid, out _), Is.True);
   }

   private static GameplayTagLibrary CreateLibrary(string path, string name, string comment)
   {
      GameplayTagLibrary library = ScriptableObject.CreateInstance<GameplayTagLibrary>();
      WriteEntry(library, name, comment);
      AssetDatabase.CreateAsset(library, path);
      return library;
   }

   private static void WriteEntry(GameplayTagLibrary library, string name, string comment)
   {
      using SerializedObject serialized = new(library);
      SerializedProperty entries = serialized.FindProperty("m_Entries");
      entries.arraySize = 1;
      SerializedProperty entry = entries.GetArrayElementAtIndex(0);
      entry.FindPropertyRelative("m_Name").stringValue = name;
      entry.FindPropertyRelative("m_Comment").stringValue = comment;
      serialized.ApplyModifiedPropertiesWithoutUndo();
   }
}
