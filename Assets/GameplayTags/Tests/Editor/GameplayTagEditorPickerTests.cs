using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GameplayTagEditorPickerTests
{
   [Test]
   public void EditingExplicitTagsRebuildsInheritedMembershipAndSupportsUndo()
   {
      TagFields asset = ScriptableObject.CreateInstance<TagFields>();
      try
      {
         GameplayTag flux = GameplayTagTestTags.Flux;
         GameplayTag parent = flux.Parent;
         using SerializedObject serialized = new(asset);
         using SerializedProperty container = serialized.FindProperty("m_Tags");
         using SerializedProperty tags = container.FindPropertyRelative(GameplayTagPicker.ExplicitTagsProperty);

         Assert.That(GameplayTagPicker.SetExplicitTag(tags, flux.Name, selected: true), Is.True);
         Assert.That(GameplayTagPicker.SetExplicitTag(tags, flux.Name, selected: true), Is.False);
         serialized.ApplyModifiedProperties();
         Assert.That(asset.Tags.ContainsExact(flux), Is.True);
         Assert.That(asset.Tags.Contains(parent), Is.True);
         Assert.That(asset.Tags.ContainsExact(parent), Is.False);

         Undo.IncrementCurrentGroup();
         Assert.That(GameplayTagPicker.SetExplicitTag(tags, flux.Name, selected: false), Is.True);
         serialized.ApplyModifiedProperties();
         Assert.That(asset.Tags.Contains(parent), Is.False);

         Undo.FlushUndoRecordObjects();
         Undo.PerformUndo();
         Assert.That(asset.Tags.ContainsExact(flux), Is.True);
         Assert.That(asset.Tags.Contains(parent), Is.True);
      }
      finally
      {
         Undo.ClearUndo(asset);
         Object.DestroyImmediate(asset);
      }
   }

   private sealed class TagFields : ScriptableObject
   {
      public GameplayTagContainer Tags => m_Tags;

      [SerializeField]
      private GameplayTagContainer m_Tags = new();
   }
}
