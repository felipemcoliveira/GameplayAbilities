using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal sealed class GameplayTagPicker : PopupWindowContent
{
   internal const string NameProperty = "m_Name";
   internal const string ExplicitTagsProperty = "m_ExplicitTags";

   private const string k_SearchControl = "GameplayTagSearch";
   private const float k_Padding = 10;
   private const float k_RowHeight = 20;

   private readonly UnityEngine.Object m_Target;
   private readonly string m_PropertyPath;
   private readonly bool m_IsContainer;
   private readonly List<GameplayTag> m_Matches = new();
   private readonly HashSet<string> m_SelectedNames = new(StringComparer.Ordinal);
   private SerializedObject m_SerializedObject;
   private GameplayTag[] m_Tags;
   private string m_Search = string.Empty;
   private Vector2 m_Scroll;
   private bool m_FocusSearch = true;

   internal GameplayTagPicker(SerializedProperty property, bool isContainer)
   {
      m_Target = property.serializedObject.targetObject;
      m_PropertyPath = property.propertyPath;
      m_IsContainer = isContainer;
   }

   public override Vector2 GetWindowSize() => new(400, 350);

   public override void OnOpen()
   {
      m_SerializedObject = new SerializedObject(m_Target);
      GameplayTagEditorCatalog.Changed += ReloadTags;
      ReloadTags();
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

      Rect row = new(k_Padding, k_Padding, rect.width - 2 * k_Padding, k_RowHeight);
      GUI.Label(row, m_IsContainer ? "Edit Gameplay Tags" : "Choose a Gameplay Tag", EditorStyles.boldLabel);
      row.y += k_RowHeight + 4;
      GUI.SetNextControlName(k_SearchControl);
      string search = EditorGUI.TextField(row, "Search", m_Search);
      if (m_FocusSearch && Event.current.type == EventType.Repaint)
      {
         EditorGUI.FocusTextInControl(k_SearchControl);
         m_FocusSearch = false;
      }
      if (m_Search != search)
      {
         m_Search = search;
         m_Scroll = Vector2.zero;
         FilterTags();
      }

      row.y += k_RowHeight + 4;
      m_SelectedNames.Clear();
      if (!m_IsContainer)
      {
         using SerializedProperty name = property.FindPropertyRelative(NameProperty);
         using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(name.stringValue)))
         {
            if (GUI.Button(row, "Clear Tag", EditorStyles.miniButton))
            {
               ApplyTag(property, string.Empty);
               return;
            }
         }
         m_SelectedNames.Add(name.stringValue);
      }
      else
      {
         using SerializedProperty tags = property.FindPropertyRelative(ExplicitTagsProperty);
         for (int i = 0; i < tags.arraySize; i++)
            m_SelectedNames.Add(GetExplicitTagName(tags, i));

         GUI.Label(row, $"{m_SelectedNames.Count} selected explicitly", EditorStyles.miniLabel);
      }

      row.y += k_RowHeight + 4;
      if (m_Matches.Count == 0)
      {
         GUI.Label(row, m_Tags.Length == 0 ? "No tags registered. Add a tag to a library first." : "No tags match your search.");
         return;
      }

      Rect viewport = new(row.x, row.y, row.width, rect.height - row.y - k_Padding);
      float listHeight = m_Matches.Count * k_RowHeight;
      float listWidth = viewport.width - (listHeight > viewport.height ? GUI.skin.verticalScrollbar.fixedWidth + 4 : 0);
      m_Scroll = GUI.BeginScrollView(viewport, m_Scroll, new Rect(0, 0, listWidth, listHeight));
      int first = Mathf.Max(0, Mathf.FloorToInt(m_Scroll.y / k_RowHeight));
      int end = Mathf.Min(m_Matches.Count, Mathf.CeilToInt((m_Scroll.y + viewport.height) / k_RowHeight));
      for (int i = first; i < end; i++)
      {
         GameplayTag tag = m_Matches[i];
         bool selected = m_SelectedNames.Contains(tag.Name);
         GUIContent content = DescribeRegisteredTag(tag);
         Rect item = new(0, i * k_RowHeight, listWidth, k_RowHeight);
         if (m_IsContainer)
         {
            if (!selected && IsInherited(tag.Name))
            {
               content.text += " (inherited)";
               content.tooltip += "\nInherited from a selected child. Check it to add it explicitly.";
            }
            bool toggled = EditorGUI.ToggleLeft(item, content, selected);
            if (toggled != selected)
            {
               using SerializedProperty tags = property.FindPropertyRelative(ExplicitTagsProperty);
               if (SetExplicitTag(tags, tag.Name, toggled))
                  m_SerializedObject.ApplyModifiedProperties();
            }
         }
         else if (GUI.Toggle(item, selected, content, EditorStyles.miniButton) != selected)
         {
            GUI.EndScrollView();
            ApplyTag(property, tag.Name);
            return;
         }
      }
      GUI.EndScrollView();
   }

   internal static GUIContent DescribeTag(string name)
   {
      if (string.IsNullOrEmpty(name))
         return new GUIContent("None", "Choose a registered gameplay tag.");

      if (!GameplayTagManager.TryGetTag(name, out GameplayTag tag))
         return new GUIContent($"{name} (unregistered)", $"{name}\nNo current source registers this saved name.");

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

   private void ApplyTag(SerializedProperty property, string name)
   {
      if (string.IsNullOrEmpty(name) || GameplayTagManager.TryGetTag(name, out _))
      {
         using SerializedProperty nameProperty = property.FindPropertyRelative(NameProperty);
         nameProperty.stringValue = name;
         m_SerializedObject.ApplyModifiedProperties();
      }
      editorWindow.Close();
   }

   private void ReloadTags()
   {
      m_Tags = GameplayTagEditorCatalog.GetTags();
      FilterTags();
      editorWindow.Repaint();
   }

   private void FilterTags()
   {
      m_Matches.Clear();
      string search = m_Search.Trim();
      foreach (GameplayTag tag in m_Tags)
      {
         if (tag.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            m_Matches.Add(tag);
      }
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
