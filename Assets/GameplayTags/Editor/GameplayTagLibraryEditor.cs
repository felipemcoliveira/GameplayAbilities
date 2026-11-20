using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(GameplayTagLibrary))]
internal sealed class GameplayTagLibraryEditor : Editor
{
   private SearchField m_SearchField;
   private GameplayTagLibraryTreeView m_Tree;

   public override void OnInspectorGUI() => DrawLibrary(expand: false);

   internal void DrawLibrary(bool expand)
   {
      if (!expand)
      {
         Rect title = EditorGUILayout.GetControlRect(false, 26);
         GameplayTagEditorGUI.DrawIcon(new Rect(title.x, title.center.y - 8, 16, 16), "library");
         GUI.Label(new Rect(title.x + 22, title.y, title.width - 22, title.height),
            new GUIContent("Gameplay Tag Library", target.name), EditorStyles.boldLabel);
         EditorGUILayout.HelpBox("Define tags with dot-separated names. Parent tags are created automatically.", MessageType.Info);
      }

      string path = AssetDatabase.GetAssetPath(target);
      if (!string.IsNullOrEmpty(path) && !GameplayTagEditorCatalog.IsLibraryPath(path))
         EditorGUILayout.HelpBox("Move this library under Resources/GameplayTags to register its definitions.", MessageType.Warning);

      serializedObject.UpdateIfRequiredOrScript();
      m_SearchField ??= new SearchField();
      m_Tree ??= new GameplayTagLibraryTreeView(serializedObject);
      using SerializedProperty entries = serializedObject.FindProperty("m_Entries");

      Rect toolbar = EditorGUILayout.GetControlRect(false, 32);
      Rect search = new(toolbar.x, toolbar.center.y - 10, Mathf.Max(40, toolbar.width - 116), 20);
      m_Tree.searchString = m_SearchField.OnGUI(search, m_Tree.searchString);
      if (GameplayTagEditorGUI.Button(new Rect(toolbar.xMax - 104, toolbar.y, 104, 32), "Add Tag", "add", "Add a new tag definition"))
         m_Tree.BeginAdd();

      EditorGUILayout.Space(4);
      int count = entries.arraySize;
      string countLabel = count == 1 ? "1 definition" : $"{count} definitions";
      EditorGUILayout.LabelField($"{countLabel}  ·  Edit names and comments in the columns", EditorStyles.miniLabel);
      EditorGUILayout.Space(4);
      Rect table = expand
         ? GUILayoutUtility.GetRect(0, 100000, 120, 100000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))
         : GUILayoutUtility.GetRect(0, Mathf.Clamp(70 + count * 36, 180, 480), GUILayout.ExpandWidth(true));
      m_Tree.Draw(table);
      if (serializedObject.ApplyModifiedProperties())
         GameplayTagEditorCatalog.Refresh();
   }
}
