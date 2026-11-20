using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public sealed class GameplayTagLibrariesWindow : EditorWindow
{
   private const string k_DefaultLibraryFolder = "Assets/GameplayTags/Resources/GameplayTags";
   private const float k_TabsHeight = 22;
   private const float k_HeaderHeight = 76;
   private const float k_FooterHeight = 27;
   private const float k_SidebarWidth = 208;
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
   private string m_LibrarySearch = string.Empty;
   private Vector2 m_TagScroll;
   private Vector2 m_DetailScroll;
   private Vector2 m_LibraryScroll;
   private SearchField m_LibrarySearchField;
   private GUIStyle m_TitleStyle;

   [MenuItem("Window/Gameplay Tags/Catalog")]
   public static void ShowWindow()
   {
      Open(0);
   }

   [MenuItem("Window/Gameplay Tags/Libraries")]
   internal static void ShowLibraries()
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
      minSize = new Vector2(640, 400);
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
      m_Libraries = Array.FindAll(GameplayTagEditorCatalog.GetLibraries(), library => library != null);
      Array.Sort(m_Libraries, (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.name, right.name));
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
      Rect toolbar = new(0, 0, position.width, k_TabsHeight);
      GUI.Box(toolbar, GUIContent.none, EditorStyles.toolbar);
      int tab = GUI.Toolbar(new Rect(2, 1, 176, k_TabsHeight - 2), m_Tab, s_Tabs, EditorStyles.toolbarButton);
      if (tab != m_Tab)
      {
         m_Tab = tab;
         GUIUtility.ExitGUI();
      }
      if (GUI.Button(new Rect(toolbar.xMax - 70, 1, 68, k_TabsHeight - 2), "Refresh", EditorStyles.toolbarButton))
      {
         GameplayTagEditorCatalog.Refresh();
         GUIUtility.ExitGUI();
      }

      Rect content = new(0, toolbar.yMax, position.width, position.height - toolbar.height);
      if (m_Tab == 0)
      {
         using GUILayout.AreaScope area = new(content);
         DrawCatalog();
      }
      else
      {
         DrawLibraries(content);
      }
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

   private void DrawLibraries(Rect content)
   {
      m_TitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
      {
         fontSize = 18,
         alignment = TextAnchor.MiddleLeft,
         clipping = TextClipping.Clip
      };

      Rect header = new(content.x, content.y, content.width, k_HeaderHeight);
      GameplayTagEditorGUI.DrawIcon(new Rect(header.x + 18, header.y + 23, 24, 24), "library");
      GUI.Label(new Rect(header.x + 54, header.y + 13, 338, 30), "Gameplay Tag Libraries", m_TitleStyle);
      GUI.Label(new Rect(header.x + 54, header.y + 44, 338, 18), "Organize definitions and document their purpose.", EditorStyles.miniLabel);
      if (GameplayTagEditorGUI.Button(new Rect(header.xMax - 160, header.y + 21, 142, 34), "New Library", "add", "Create a new tag library"))
         CreateLibrary();
      GameplayTagEditorGUI.Separator(new Rect(header.x, header.yMax - 1, header.width, 1));

      Rect body = new(content.x, header.yMax, content.width, Mathf.Max(0, content.height - k_HeaderHeight - k_FooterHeight));
      Rect sidebar = new(body.x, body.y, k_SidebarWidth, body.height);
      EditorGUI.DrawRect(new Rect(sidebar.xMax - 1, sidebar.y, 1, sidebar.height), GameplayTagEditorGUI.BorderColor);
      DrawLibrarySidebar(new Rect(sidebar.x + 12, sidebar.y + 14, sidebar.width - 24, Mathf.Max(0, sidebar.height - 26)));
      DrawSelectedLibrary(new Rect(sidebar.xMax + 18, body.y + 16, Mathf.Max(0, body.width - sidebar.width - 36), Mathf.Max(0, body.height - 32)));

      Rect footer = new(content.x, content.yMax - k_FooterHeight, content.width, k_FooterHeight);
      GameplayTagEditorGUI.Separator(footer);
      GUI.Label(new Rect(footer.x + 14, footer.y + 5, footer.width - 28, 18),
         "Parents are created automatically from dot-separated names.  Changes support Undo / Redo.", EditorStyles.miniLabel);
   }

   private void DrawLibrarySidebar(Rect rect)
   {
      using GUILayout.AreaScope area = new(rect);
      EditorGUILayout.LabelField($"LIBRARIES  {m_Libraries.Length}", EditorStyles.miniBoldLabel);
      GUILayout.Space(6);
      m_LibrarySearchField ??= new SearchField();
      string search = m_LibrarySearchField.OnGUI(EditorGUILayout.GetControlRect(), m_LibrarySearch);
      if (search != m_LibrarySearch)
      {
         m_LibrarySearch = search;
         m_LibraryScroll = Vector2.zero;
         GUIUtility.ExitGUI();
      }
      GUILayout.Space(10);

      using EditorGUILayout.ScrollViewScope scroll = new(m_LibraryScroll);
      m_LibraryScroll = scroll.scrollPosition;
      bool visible = false;
      foreach (GameplayTagLibrary library in m_Libraries)
      {
         if (library == null || !library.name.Contains(m_LibrarySearch, StringComparison.OrdinalIgnoreCase))
            continue;

         visible = true;
         Rect row = EditorGUILayout.GetControlRect(false, 48);
         bool selected = m_Library == library;
         if (GUI.Toggle(row, selected, new GUIContent(string.Empty, AssetDatabase.GetAssetPath(library)), GUI.skin.button) && !selected)
         {
            m_Library = library;
            GUI.FocusControl(null);
            GUIUtility.ExitGUI();
         }

         GameplayTagEditorGUI.DrawIcon(new Rect(row.x + 10, row.y + 9, 16, 16), "library");
         GUI.Label(new Rect(row.x + 32, row.y + 5, row.width - 38, 22), library.name, GameplayTagEditorGUI.ValueStyle);
         int count = library.Entries.Count;
         GUI.Label(new Rect(row.x + 32, row.y + 27, row.width - 38, 16),
            count == 1 ? "1 definition" : $"{count} definitions", EditorStyles.miniLabel);
         GUILayout.Space(3);
      }

      if (!visible)
         EditorGUILayout.LabelField("No libraries found", EditorStyles.centeredGreyMiniLabel);
   }

   private void DrawSelectedLibrary(Rect rect)
   {
      using GUILayout.AreaScope area = new(rect);
      if (m_Library == null)
      {
         GUILayout.Space(40);
         EditorGUILayout.LabelField("Create your first library", EditorStyles.boldLabel);
         EditorGUILayout.LabelField("Keep entity, item and ability tags in separate libraries.", EditorStyles.wordWrappedLabel);
         return;
      }

      string path = AssetDatabase.GetAssetPath(m_Library);
      Rect title = EditorGUILayout.GetControlRect(false, 36);
      GUI.Label(new Rect(title.x, title.y, title.width - 192, title.height), m_Library.name, m_TitleStyle);
      if (GameplayTagEditorGUI.Button(new Rect(title.xMax - 184, title.y, 144, 34), "Show in Project", "library", "Find this library in the Project window"))
      {
         Selection.activeObject = m_Library;
         EditorGUIUtility.PingObject(m_Library);
      }
      using (new EditorGUI.DisabledScope(!path.StartsWith("Assets/", StringComparison.Ordinal)))
      {
         if (GameplayTagEditorGUI.IconButton(new Rect(title.xMax - 34, title.y, 34, 34), "remove", "Move library to the trash"))
            DeleteLibrary(path);
      }

      EditorGUILayout.LabelField(new GUIContent(path, path), EditorStyles.miniLabel);
      GUILayout.Space(12);
      Editor.CreateCachedEditor(m_Library, typeof(GameplayTagLibraryEditor), ref m_LibraryEditor);
      ((GameplayTagLibraryEditor)m_LibraryEditor).DrawLibrary(expand: true);
   }

   private void DeleteLibrary(string path)
   {
      string message = $"Move {m_Library.name} to the system trash?\n\n{path}\n\nSaved references to definitions unique to this library may become unresolved. Unity Undo cannot restore the asset.";
      if (!EditorUtility.DisplayDialog("Delete tag library?", message, "Delete Library", "Cancel"))
         return;

      if (!AssetDatabase.MoveAssetToTrash(path))
      {
         EditorUtility.DisplayDialog("Library could not be deleted", $"Unity could not move this asset to the trash:\n\n{path}", "OK");
         return;
      }

      m_Library = null;
      GameplayTagEditorCatalog.Refresh();
      GUI.FocusControl(null);
      GUIUtility.ExitGUI();
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
