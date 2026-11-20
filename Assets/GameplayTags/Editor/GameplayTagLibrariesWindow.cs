using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class GameplayTagLibrariesWindow : EditorWindow
{
   private const string k_DefaultLibraryFolder = "Assets/GameplayTags/Resources/GameplayTags";
   private static readonly string[] s_Tabs = { "Catalog", "Libraries" };

   [SerializeField]
   private int m_Tab;

   [SerializeField]
   private GameplayTagLibrary m_Library;

   [SerializeField]
   private string m_SelectedTag;

   private TagRow[] m_Confirmed = Array.Empty<TagRow>();
   private TagRow[] m_Temporary = Array.Empty<TagRow>();
   private GameplayTagLibrary[] m_Libraries = Array.Empty<GameplayTagLibrary>();
   private Editor m_LibraryEditor;
   private string m_TagSearch = string.Empty;
   private Vector2 m_TagScroll;
   private Vector2 m_DetailScroll;
   private Vector2 m_LibraryScroll;
   private Vector2 m_LibraryEditorScroll;

   [MenuItem("Window/Gameplay Tags/Catalog")]
   public static void ShowWindow()
   {
      Open(0);
   }

   [MenuItem("Window/Gameplay Tags/Libraries")]
   private static void ShowLibraries()
   {
      Open(1);
   }

   private static void Open(int tab)
   {
      GameplayTagLibrariesWindow window = GetWindow<GameplayTagLibrariesWindow>("Gameplay Tags");
      window.m_Tab = tab;
      window.Show();
   }

   private void OnEnable()
   {
      minSize = new Vector2(620, 360);
      GameplayTagEditorCatalog.Changed += RefreshSnapshots;
      RefreshSnapshots();
   }

   private void OnFocus()
   {
      RefreshSnapshots();
   }

   private void OnDisable()
   {
      GameplayTagEditorCatalog.Changed -= RefreshSnapshots;
      if (m_LibraryEditor != null)
         DestroyImmediate(m_LibraryEditor);
   }

   private void RefreshSnapshots()
   {
      m_Confirmed = CreateRows(GameplayTagEditorCatalog.GetTags(), temporary: false);
      m_Temporary = CreateRows(GameplayTagEditorCatalog.GetUnresolvedTags(), temporary: true);
      m_Libraries = GameplayTagEditorCatalog.GetLibraries();
      if (m_Library == null && m_Libraries.Length > 0)
         m_Library = m_Libraries[0];
      if (FindSelectedTag() == null)
      {
         int index = Array.FindIndex(m_Confirmed, row => row.Comments.Count > 0);
         m_SelectedTag = index >= 0 ? m_Confirmed[index].Name : m_Confirmed.Length > 0 ? m_Confirmed[0].Name : null;
      }

      Repaint();
   }

   private static TagRow[] CreateRows(GameplayTag[] tags, bool temporary)
   {
      TagRow[] rows = new TagRow[tags.Length];
      for (int index = 0; index < tags.Length; index++)
      {
         GameplayTag tag = tags[index];
         GameplayTagManager.TryGetMetadata(tag, out GameplayTagMetadata metadata);
         rows[index] = new TagRow(tag.Name, metadata, temporary);
      }

      return rows;
   }

   private void OnGUI()
   {
      using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
      {
         int tab = GUILayout.Toolbar(m_Tab, s_Tabs, EditorStyles.toolbarButton, GUILayout.Width(176));
         if (tab != m_Tab)
         {
            m_Tab = tab;
            GUIUtility.ExitGUI();
         }

         GUILayout.FlexibleSpace();
         if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(68)))
         {
            GameplayTagEditorCatalog.Refresh();
            RefreshSnapshots();
            GUIUtility.ExitGUI();
         }
      }

      if (m_Tab == 0)
         DrawCatalog();
      else
         DrawLibraries();
   }

   private void DrawCatalog()
   {
      using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
      {
         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
         {
            m_TagSearch = EditorGUILayout.TextField("Search tags", m_TagSearch);
            using EditorGUILayout.ScrollViewScope scroll = new(m_TagScroll);
            m_TagScroll = scroll.scrollPosition;
            DrawTagGroup("Confirmed tags", m_Confirmed, "No registered tags.");
            GUILayout.Space(8);
            DrawTagGroup("Unresolved references", m_Temporary, "No unresolved references.");
         }

         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(240), GUILayout.ExpandHeight(true)))
         {
            using EditorGUILayout.ScrollViewScope scroll = new(m_DetailScroll);
            m_DetailScroll = scroll.scrollPosition;
            DrawTagDetails();
         }
      }
   }

   private void DrawTagGroup(string title, TagRow[] rows, string emptyMessage)
   {
      EditorGUILayout.LabelField($"{title} ({rows.Length})", EditorStyles.boldLabel);
      bool found = false;
      foreach (TagRow row in rows)
      {
         if (!row.Matches(m_TagSearch))
            continue;

         found = true;
         DrawTagRow(row);
      }

      if (!found)
         EditorGUILayout.LabelField(rows.Length == 0 ? emptyMessage : "No matching tags.", EditorStyles.miniLabel);
   }

   private void DrawTagRow(TagRow row)
   {
      Rect rect = EditorGUILayout.GetControlRect(false, 21);
      if (Event.current.type == EventType.Repaint && m_SelectedTag == row.Name)
      {
         Color highlight = EditorGUIUtility.isProSkin ? new Color(0.23f, 0.44f, 0.63f, 0.45f) : new Color(0.28f, 0.55f, 0.84f, 0.28f);
         EditorGUI.DrawRect(rect, highlight);
      }

      if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && m_SelectedTag != row.Name)
      {
         m_SelectedTag = row.Name;
         m_DetailScroll = Vector2.zero;
         GUIUtility.ExitGUI();
      }

      EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
      bool showHierarchy = m_TagSearch.Length == 0 && !row.Temporary;
      float indent = showHierarchy ? Mathf.Min(row.Depth, 8) * 12 : 0;
      Rect nameRect = new(rect.x + 5 + indent, rect.y, Mathf.Max(0, rect.width - 80 - indent), rect.height);
      Rect kindRect = new(rect.xMax - 70, rect.y, 68, rect.height);
      EditorGUI.LabelField(nameRect, new GUIContent(showHierarchy ? row.Segment : row.Name, row.Name));
      EditorGUI.LabelField(kindRect, row.Kind, EditorStyles.miniLabel);
   }

   private TagRow? FindSelectedTag()
   {
      foreach (TagRow row in m_Confirmed)
      {
         if (row.Name == m_SelectedTag)
            return row;
      }

      foreach (TagRow row in m_Temporary)
      {
         if (row.Name == m_SelectedTag)
            return row;
      }

      return null;
   }

   private void DrawTagDetails()
   {
      EditorGUILayout.LabelField("Tag details", EditorStyles.boldLabel);
      TagRow? selected = FindSelectedTag();
      if (selected == null)
      {
         EditorGUILayout.LabelField("Select a tag to see its name and comments.", EditorStyles.wordWrappedLabel);
         return;
      }

      TagRow row = selected.Value;
      EditorGUILayout.SelectableLabel(row.Name, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
      EditorGUILayout.LabelField(row.Description, EditorStyles.wordWrappedLabel);
      if (row.Comments.Count == 0)
         return;

      EditorGUILayout.Space(7);
      EditorGUILayout.LabelField("Developer comments", EditorStyles.miniBoldLabel);
      foreach (string comment in row.Comments)
         EditorGUILayout.LabelField(comment, EditorStyles.wordWrappedLabel);
   }

   private void DrawLibraries()
   {
      using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
      {
         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(205), GUILayout.ExpandHeight(true)))
         {
            EditorGUILayout.LabelField($"Libraries ({m_Libraries.Length})", EditorStyles.boldLabel);
            if (GUILayout.Button("New Library"))
               CreateLibrary();

            using EditorGUILayout.ScrollViewScope scroll = new(m_LibraryScroll);
            m_LibraryScroll = scroll.scrollPosition;
            foreach (GameplayTagLibrary library in m_Libraries)
            {
               if (library != null && GUILayout.Toggle(m_Library == library, library.name, GUI.skin.button) && m_Library != library)
               {
                  m_Library = library;
                  m_LibraryEditorScroll = Vector2.zero;
                  GUIUtility.ExitGUI();
               }
            }
         }

         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
         {
            if (m_Library == null)
            {
               EditorGUILayout.LabelField("Create a library to declare tags and document what they mean.", EditorStyles.wordWrappedLabel);
               return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
               EditorGUILayout.LabelField(m_Library.name, EditorStyles.boldLabel);
               if (GUILayout.Button("Select Asset", GUILayout.Width(95)))
               {
                  Selection.activeObject = m_Library;
                  EditorGUIUtility.PingObject(m_Library);
               }
            }

            string path = AssetDatabase.GetAssetPath(m_Library);
            EditorGUILayout.LabelField(new GUIContent(path, path), EditorStyles.miniLabel);
            using EditorGUILayout.ScrollViewScope scroll = new(m_LibraryEditorScroll);
            m_LibraryEditorScroll = scroll.scrollPosition;
            Editor.CreateCachedEditor(m_Library, typeof(GameplayTagLibraryEditor), ref m_LibraryEditor);
            m_LibraryEditor.OnInspectorGUI();
         }
      }
   }

   private void CreateLibrary()
   {
      string folder = AssetDatabase.IsValidFolder(k_DefaultLibraryFolder) ? k_DefaultLibraryFolder : "Assets";
      string path = EditorUtility.SaveFilePanelInProject("Create Gameplay Tag Library", "GameplayTagLibrary", "asset",
         "Choose a path under Resources/GameplayTags.", folder);
      if (string.IsNullOrEmpty(path))
         return;

      if (!GameplayTagEditorCatalog.IsLibraryPath(path))
      {
         EditorUtility.DisplayDialog("Invalid library path", "Choose a folder under Resources/GameplayTags.", "OK");
         return;
      }

      GameplayTagLibrary library = CreateInstance<GameplayTagLibrary>();
      AssetDatabase.CreateAsset(library, AssetDatabase.GenerateUniqueAssetPath(path));
      m_Library = library;
      Selection.activeObject = library;
      m_LibraryEditorScroll = Vector2.zero;
      GameplayTagEditorCatalog.Refresh();
      GUIUtility.ExitGUI();
   }

   private readonly struct TagRow
   {
      internal readonly string Name;
      internal readonly string Segment;
      internal readonly int Depth;
      internal readonly bool Temporary;
      internal readonly IReadOnlyList<string> Comments;

      private readonly bool m_Declared;

      internal string Kind => Temporary ? "Unresolved" : m_Declared ? "Declared" : "Parent";
      internal string Description => Temporary ? "A serialized value refers to this name, but no current source declares it."
         : m_Declared ? "Declared in a library or in code." : "Registered automatically because a child tag was declared.";

      internal TagRow(string name, GameplayTagMetadata metadata, bool temporary)
      {
         Name = name;
         Segment = name[(name.LastIndexOf('.') + 1)..];
         Depth = 0;
         foreach (char character in name)
         {
            if (character == '.')
               Depth++;
         }

         Temporary = temporary;
         Comments = metadata?.Comments ?? Array.Empty<string>();
         m_Declared = metadata?.IsExplicit ?? false;
      }

      internal bool Matches(string search)
      {
         if (Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

         foreach (string comment in Comments)
         {
            if (comment.Contains(search, StringComparison.OrdinalIgnoreCase))
               return true;
         }

         return false;
      }
   }
}
