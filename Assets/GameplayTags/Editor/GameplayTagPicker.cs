using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

internal sealed class GameplayTagPicker : PopupWindowContent
{
   internal const string NameProperty = "m_Name";
   internal const string ExplicitTagsProperty = "m_ExplicitTags";

   private readonly UnityEngine.Object m_Target;
   private readonly string m_PropertyPath;
   private readonly bool m_IsContainer;
   private readonly HashSet<string> m_SelectedNames = new(StringComparer.Ordinal);
   private SerializedObject m_SerializedObject;
   private GameplayTagTreeView m_Tree;
   private SearchField m_SearchField;
   private bool m_FocusSearch = true;

   internal GameplayTagPicker(SerializedProperty property, bool isContainer)
   {
      m_Target = property.serializedObject.targetObject;
      m_PropertyPath = property.propertyPath;
      m_IsContainer = isContainer;
   }

   public override Vector2 GetWindowSize() => new(420, 430);

   public override void OnOpen()
   {
      m_SerializedObject = new SerializedObject(m_Target);
      m_Tree = new GameplayTagTreeView(m_SelectedNames.Contains, IsInherited, ToggleTag);
      m_SearchField = new SearchField();
      m_SearchField.downOrUpArrowKeyPressed += m_Tree.SetFocusAndEnsureSelectedItem;
      GameplayTagEditorCatalog.Changed += ReloadTags;
      ReloadTags();
      m_Tree.ExpandAll();
   }

   public override void OnClose()
   {
      GameplayTagEditorCatalog.Changed -= ReloadTags;
      m_SerializedObject?.Dispose();
   }

   public override void OnGUI(Rect rect)
   {
      if (m_Target == null)
      {
         editorWindow.Close();
         return;
      }

      m_SerializedObject.UpdateIfRequiredOrScript();
      using SerializedProperty property = m_SerializedObject.FindProperty(m_PropertyPath);
      if (property == null)
      {
         editorWindow.Close();
         return;
      }

      ReadSelection(property);
      GameplayTagEditorGUI.DrawIcon(new Rect(12, 12, 18, 18), "tag");
      GUI.Label(new Rect(38, 8, rect.width - 50, 22), m_IsContainer ? "Gameplay Tags" : "Gameplay Tag", EditorStyles.boldLabel);
      GUI.Label(new Rect(12, 34, rect.width - 24, 18),
         m_IsContainer ? $"{m_SelectedNames.Count} selected explicitly" : "Choose one tag", EditorStyles.miniLabel);
      m_Tree.searchString = m_SearchField.OnGUI(new Rect(12, 60, rect.width - 24, 20), m_Tree.searchString);
      if (m_FocusSearch && Event.current.type == EventType.Repaint)
      {
         m_SearchField.SetFocus();
         m_FocusSearch = false;
      }

      GameplayTagEditorGUI.Separator(new Rect(0, 90, rect.width, 1));
      if (m_Tree.TagCount == 0)
      {
         EditorGUI.HelpBox(new Rect(12, 104, rect.width - 24, 60),
            "No tags defined. Open Libraries to create your first tags.", MessageType.Info);
      }
      else
         m_Tree.OnGUI(new Rect(4, 96, rect.width - 8, rect.height - 194));

      DrawFooter(rect, property);
   }

   private void DrawFooter(Rect rect, SerializedProperty property)
   {
      GameplayTagEditorGUI.Separator(new Rect(0, rect.height - 92, rect.width, 1));
      using (new EditorGUI.DisabledScope(true))
      {
         EditorGUI.Toggle(new Rect(12, rect.height - 82, 18, 18), true);
         GUI.Label(new Rect(34, rect.height - 82, 75, 18), "Explicit", EditorStyles.miniLabel);
         if (m_IsContainer)
         {
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = true;
            EditorGUI.Toggle(new Rect(112, rect.height - 82, 18, 18), false);
            EditorGUI.showMixedValue = previousMixed;
            GUI.Label(new Rect(134, rect.height - 82, 200, 18), "Inherited from a child", EditorStyles.miniLabel);
         }
      }

      using (new EditorGUI.DisabledScope(m_SelectedNames.Count == 0))
      {
         if (GameplayTagEditorGUI.Button(new Rect(12, rect.height - 48, 108, 36), "Clear", "clear", "Clear the selection"))
            ClearSelection(property);
      }

      if (GameplayTagEditorGUI.Button(new Rect(rect.width - 180, rect.height - 48, 168, 36),
            "Edit Libraries", "edit", "Edit tag definitions and comments"))
      {
         editorWindow.Close();
         GameplayTagLibrariesWindow.ShowLibraries();
      }
   }

   private void ReadSelection(SerializedProperty property)
   {
      m_SelectedNames.Clear();
      if (!m_IsContainer)
      {
         using SerializedProperty name = property.FindPropertyRelative(NameProperty);
         if (!string.IsNullOrEmpty(name.stringValue))
            m_SelectedNames.Add(name.stringValue);
         return;
      }

      using SerializedProperty tags = property.FindPropertyRelative(ExplicitTagsProperty);
      for (int i = 0; i < tags.arraySize; i++)
         m_SelectedNames.Add(GetExplicitTagName(tags, i));
   }

   internal static GUIContent DescribeTag(string name)
   {
      if (string.IsNullOrEmpty(name))
         return new GUIContent("None", "Choose a registered gameplay tag.");

      if (!GameplayTagManager.TryGetTag(name, out GameplayTag tag))
         return new GUIContent($"{name} (missing)", $"{name}\nNo current source registers this saved name.");

      return DescribeRegisteredTag(tag);
   }

   internal static bool SetExplicitTag(SerializedProperty tags, string name, bool selected)
   {
      for (int i = 0; i < tags.arraySize; i++)
      {
         string current = GetExplicitTagName(tags, i);
         if (!string.Equals(current, name, StringComparison.Ordinal))
            continue;
         if (selected)
            return false;

         tags.DeleteArrayElementAtIndex(i);
         return true;
      }

      if (!selected || !GameplayTagManager.TryGetTag(name, out GameplayTag tag))
         return false;

      int index = tags.arraySize;
      tags.arraySize = index + 1;
      using SerializedProperty item = tags.GetArrayElementAtIndex(index);
      using SerializedProperty nameProperty = item.FindPropertyRelative(NameProperty);
      nameProperty.stringValue = tag.Name;
      return true;
   }

   internal static string GetExplicitTagName(SerializedProperty tags, int index)
   {
      using SerializedProperty item = tags.GetArrayElementAtIndex(index);
      using SerializedProperty name = item.FindPropertyRelative(NameProperty);
      return name.stringValue;
   }

   private static GUIContent DescribeRegisteredTag(GameplayTag tag)
   {
      string name = tag.Name;
      if (GameplayTagManager.TryGetMetadata(tag, out GameplayTagMetadata metadata) && metadata.Comments.Count > 0)
         return new GUIContent(name, $"{name}\n{string.Join("\n", metadata.Comments)}");

      return new GUIContent(name, name);
   }

   private void ToggleTag(GameplayTag tag, bool selected)
   {
      using SerializedProperty property = m_SerializedObject.FindProperty(m_PropertyPath);
      if (property == null)
         return;

      if (!m_IsContainer)
      {
         if (selected && !GameplayTagManager.TryGetTag(tag.Name, out _))
            return;

         using SerializedProperty nameProperty = property.FindPropertyRelative(NameProperty);
         nameProperty.stringValue = selected ? tag.Name : string.Empty;
         m_SerializedObject.ApplyModifiedProperties();
         editorWindow.Close();
         return;
      }

      using SerializedProperty tags = property.FindPropertyRelative(ExplicitTagsProperty);
      if (SetExplicitTag(tags, tag.Name, selected))
      {
         m_SerializedObject.ApplyModifiedProperties();
         editorWindow.Repaint();
      }
   }

   private void ClearSelection(SerializedProperty property)
   {
      if (m_IsContainer)
      {
         using SerializedProperty tags = property.FindPropertyRelative(ExplicitTagsProperty);
         tags.ClearArray();
      }
      else
      {
         using SerializedProperty name = property.FindPropertyRelative(NameProperty);
         name.stringValue = string.Empty;
      }
      m_SerializedObject.ApplyModifiedProperties();
      editorWindow.Repaint();
   }

   private void ReloadTags()
   {
      m_Tree.SetTags(GameplayTagEditorCatalog.GetTags());
      editorWindow.Repaint();
   }

   private bool IsInherited(string name)
   {
      foreach (string selected in m_SelectedNames)
      {
         if (selected.Length > name.Length && selected[name.Length] == '.' && selected.StartsWith(name, StringComparison.Ordinal))
            return true;
      }
      return false;
   }
}
