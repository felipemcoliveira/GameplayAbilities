using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameplayTagLibrary))]
internal sealed class GameplayTagLibraryEditor : Editor
{
   private string m_Search = string.Empty;
   private string m_NewName = string.Empty;
   private string m_Error;

   public override void OnInspectorGUI()
   {
      string assetPath = AssetDatabase.GetAssetPath(target);
      if (!string.IsNullOrEmpty(assetPath) && !GameplayTagEditorCatalog.IsLibraryPath(assetPath))
      {
         EditorGUILayout.HelpBox("Move this asset under a Resources/GameplayTags folder to include its tags in the catalog.", MessageType.Warning);
      }

      serializedObject.UpdateIfRequiredOrScript();
      SerializedProperty entries = serializedObject.FindProperty("m_Entries");

      string previousError = m_Error;
      bool submittedNewEntry = DrawNewEntry(entries);
      if (!submittedNewEntry)
      {
         if (!string.IsNullOrEmpty(m_Error))
            EditorGUILayout.HelpBox(m_Error, MessageType.Warning);

         EditorGUILayout.Space(6);
         m_Search = EditorGUILayout.TextField("Filter entries", m_Search);
         DrawEntries(entries);
      }

      bool changed = serializedObject.ApplyModifiedProperties();
      if (changed)
         GameplayTagEditorCatalog.Refresh();
      if (submittedNewEntry || changed || previousError != m_Error)
         GUIUtility.ExitGUI();
   }

   private bool DrawNewEntry(SerializedProperty entries)
   {
      using (new EditorGUILayout.HorizontalScope())
      {
         m_NewName = EditorGUILayout.TextField("New tag name", m_NewName);
         using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_NewName)))
         {
            if (!GUILayout.Button("Add Tag", GUILayout.Width(70)))
               return false;
         }
      }

      if (!ValidateName(m_NewName))
         return true;

      int index = entries.arraySize++;
      SerializedProperty entry = entries.GetArrayElementAtIndex(index);
      entry.FindPropertyRelative("m_Name").stringValue = m_NewName;
      entry.FindPropertyRelative("m_Comment").stringValue = string.Empty;
      m_NewName = string.Empty;
      m_Search = string.Empty;
      GUI.FocusControl(null);
      return true;
   }

   private void DrawEntries(SerializedProperty entries)
   {
      int removeIndex = -1;
      bool found = false;
      for (int index = 0; index < entries.arraySize; index++)
      {
         SerializedProperty entry = entries.GetArrayElementAtIndex(index);
         SerializedProperty name = entry.FindPropertyRelative("m_Name");
         SerializedProperty comment = entry.FindPropertyRelative("m_Comment");
         if (m_Search.Length > 0 && !name.stringValue.Contains(m_Search, StringComparison.OrdinalIgnoreCase) &&
            !comment.stringValue.Contains(m_Search, StringComparison.OrdinalIgnoreCase))
         {
            continue;
         }

         found = true;
         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
         {
            using (new EditorGUILayout.HorizontalScope())
            {
               string newName = EditorGUILayout.DelayedTextField("Tag name", name.stringValue);
               if (newName != name.stringValue && ValidateName(newName))
                  name.stringValue = newName;

               if (GUILayout.Button("Remove", GUILayout.Width(70)))
                  removeIndex = index;
            }

            if (!GameplayTagRegistry.IsValidName(name.stringValue))
               EditorGUILayout.HelpBox("Enter a dot-separated name with no empty segments or whitespace.", MessageType.Warning);

            comment.stringValue = EditorGUILayout.DelayedTextField("Developer comment", comment.stringValue);
         }
      }

      if (removeIndex >= 0)
      {
         entries.DeleteArrayElementAtIndex(removeIndex);
         m_Error = null;
         GUI.FocusControl(null);
      }

      if (!found)
      {
         string message = entries.arraySize == 0 ? "No tags in this library." : "No entries match the filter.";
         EditorGUILayout.LabelField(message, EditorStyles.centeredGreyMiniLabel);
      }
   }

   private bool ValidateName(string name)
   {
      m_Error = GameplayTagRegistry.IsValidName(name)
         ? null
         : $"'{name}' must have nonempty segments separated by dots, without whitespace.";
      return m_Error == null;
   }
}
