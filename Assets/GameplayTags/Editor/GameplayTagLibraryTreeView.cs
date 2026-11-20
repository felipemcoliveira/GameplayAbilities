using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

internal sealed class GameplayTagLibraryTreeView : TreeView
{
   private const float k_RowHeight = 30;
   private const float k_InitialNameWidth = 250;
   private const int k_NewTagId = int.MinValue;
   private const string k_InvalidName = "Enter nonempty dot-separated segments with no whitespace.";

   private readonly SerializedObject m_Library;
   private readonly Dictionary<string, int> m_ParentIds = new(StringComparer.Ordinal);
   private readonly string m_ControlPrefix;
   private int m_NextParentId = -1;
   private int m_ContentHash;
   private int m_HeightHash;
   private int m_RemoveIndex = -1;
   private int m_RevealId;
   private int m_FocusId;
   private int m_InvalidId;
   private bool m_Adding;
   private string m_NewName = string.Empty;
   private float m_PreferredNameWidth = k_InitialNameWidth;
   private float m_NameWidth = k_InitialNameWidth;
   private GUIStyle m_CommentStyle;

   private GUIStyle CommentStyle => m_CommentStyle ??= new GUIStyle(EditorStyles.textArea)
   {
      wordWrap = true,
      fixedHeight = 0,
      alignment = TextAnchor.UpperLeft
   };

   internal GameplayTagLibraryTreeView(SerializedObject library) : base(new TreeViewState(), CreateHeader())
   {
      m_Library = library;
      m_ControlPrefix = $"GameplayTagLibrary.Name.{library.targetObject.GetEntityId()}.";
      rowHeight = k_RowHeight;
      showBorder = true;
      columnIndexForTreeFoldouts = 0;
      Reload();
      ExpandAll();
   }

   internal void BeginAdd()
   {
      m_Adding = true;
      searchString = string.Empty;
      Reload();
      RevealRow(k_NewTagId);
      m_FocusId = k_NewTagId;
   }

   internal void Draw(Rect rect)
   {
      if (Event.current.type == EventType.Layout)
         RefreshRows();

      // Allow the name column to be resized and use the remaining viewport for comments.
      ResizeCommentColumn(rect.width - 2);
      if (MeasureRows() > rect.height)
      {
         ResizeCommentColumn(rect.width - GUI.skin.verticalScrollbar.fixedWidth - 2);
         MeasureRows();
      }

      OnGUI(rect);
      if (m_RemoveIndex >= 0)
      {
         using SerializedProperty entries = m_Library.FindProperty("m_Entries");
         entries.DeleteArrayElementAtIndex(m_RemoveIndex);
         m_RemoveIndex = -1;
         m_InvalidId = 0;
         GUI.FocusControl(null);
         SetSelection(Array.Empty<int>());
         Reload();
      }
   }

   protected override TreeViewItem BuildRoot()
   {
      TreeViewItem root = new(0, -1, "Root") { children = new List<TreeViewItem>() };
      if (m_Adding)
         root.AddChild(new TagRow(k_NewTagId, string.Empty));

      Dictionary<string, TagRow> items = new(StringComparer.Ordinal);
      using SerializedProperty entries = m_Library.FindProperty("m_Entries");
      for (int i = 0; i < entries.arraySize; i++)
      {
         using SerializedProperty entry = entries.GetArrayElementAtIndex(i);
         using SerializedProperty nameProperty = entry.FindPropertyRelative("m_Name");
         string name = nameProperty.stringValue;
         if (!GameplayTagRegistry.IsValidName(name))
         {
            root.AddChild(new TagRow(i + 1, name) { EntryIndex = i, Warning = k_InvalidName });
            continue;
         }

         TagRow item = EnsureRow(name, root, items);
         if (item.EntryIndex >= 0)
         {
            item.Warning = "Another definition in this library uses the same name.";
            root.AddChild(new TagRow(i + 1, name) { EntryIndex = i, Warning = item.Warning });
            continue;
         }

         item.id = i + 1;
         item.EntryIndex = i;
      }
      SetupDepthsFromParentsAndChildren(root);
      return root;
   }

   protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
   {
      TagRow row = (TagRow)item;
      if (row.id == k_NewTagId || row.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
         return true;

      return row.EntryIndex >= 0 && ReadComment(row.EntryIndex).Contains(search, StringComparison.OrdinalIgnoreCase);
   }

   protected override float GetCustomRowHeight(int row, TreeViewItem item)
   {
      TagRow tag = (TagRow)item;
      return tag.EntryIndex < 0 ? k_RowHeight : Mathf.Max(k_RowHeight, CommentHeight(ReadComment(tag.EntryIndex)) + 8);
   }

   protected override void BeforeRowsGUI()
   {
      if (Event.current.type != EventType.Repaint)
         return;

      GetFirstAndLastVisibleRows(out int first, out int last);
      int count = GetRows().Count;
      for (int row = Mathf.Max(0, first); row <= last && row < count; row++)
      {
         Rect rect = GetRowRect(row);
         rect.width = Mathf.Max(rect.width, multiColumnHeader.state.widthOfAllVisibleColumns);
         GUIStyle background = row % 2 == 0 ? DefaultStyles.backgroundEven : DefaultStyles.backgroundOdd;
         background.Draw(rect, false, false, false, false);
      }
   }

   protected override void RowGUI(RowGUIArgs args)
   {
      TagRow row = (TagRow)args.item;
      using SerializedProperty entries = m_Library.FindProperty("m_Entries");
      using SerializedProperty entry = row.EntryIndex < 0 ? null : entries.GetArrayElementAtIndex(row.EntryIndex);
      for (int i = 0; i < args.GetNumVisibleColumns(); i++)
      {
         Rect cell = args.GetCellRect(i);
         cell.xMin += 5;
         cell.xMax -= 5;
         switch (args.GetColumn(i))
         {
            case 0:
               DrawName(cell, row, entry);
               break;
            case 1:
               DrawComment(cell, row, entry);
               break;
            case 2:
               DrawAction(cell, row);
               break;
         }
      }
   }

   private void DrawName(Rect cell, TagRow row, SerializedProperty entry)
   {
      cell.xMin += hasSearch ? 0 : GetContentIndent(row);
      GameplayTagEditorGUI.DrawIcon(new Rect(cell.x, cell.center.y - 7, 14, 14), "tag");
      cell.xMin += 20;
      if (cell.width < 14)
         return;

      if (entry == null && row.id != k_NewTagId)
      {
         GUI.Label(cell, new GUIContent(hasSearch ? row.FullName : row.displayName, row.FullName));
         return;
      }

      string warning = m_InvalidId == row.id ? k_InvalidName : row.Warning;
      if (warning != null && cell.width >= 36)
      {
         Rect icon = new(cell.xMax - 18, cell.center.y - 8, 16, 16);
         GameplayTagEditorGUI.DrawIcon(icon, "warning");
         GUI.Label(icon, new GUIContent(string.Empty, warning), GUIStyle.none);
         cell.xMax -= 22;
      }

      cell.y = cell.center.y - EditorGUIUtility.singleLineHeight * 0.5f;
      cell.height = EditorGUIUtility.singleLineHeight;
      string control = m_ControlPrefix + row.id;
      using SerializedProperty property = entry?.FindPropertyRelative("m_Name");
      if (property != null)
         EditorGUI.BeginProperty(cell, GUIContent.none, property);
      GUI.SetNextControlName(control);
      string current = property?.stringValue ?? m_NewName;
      string name = EditorGUI.DelayedTextField(cell, current);
      if (property != null)
         EditorGUI.EndProperty();
      if (name != current)
         SaveName(row.id, property, name);

      if (m_FocusId == row.id && Event.current.type == EventType.Repaint)
      {
         EditorGUI.FocusTextInControl(control);
         m_FocusId = 0;
      }
   }

   private void SaveName(int id, SerializedProperty property, string name)
   {
      if (id == k_NewTagId)
         m_NewName = name;
      if (!GameplayTagRegistry.IsValidName(name))
      {
         m_InvalidId = id;
         return;
      }

      m_InvalidId = 0;
      if (property != null)
      {
         property.stringValue = name;
         m_RevealId = id;
         return;
      }

      using SerializedProperty entries = m_Library.FindProperty("m_Entries");
      int index = entries.arraySize++;
      using SerializedProperty entry = entries.GetArrayElementAtIndex(index);
      using SerializedProperty newName = entry.FindPropertyRelative("m_Name");
      using SerializedProperty comment = entry.FindPropertyRelative("m_Comment");
      newName.stringValue = name;
      comment.stringValue = string.Empty;
      ResetNewRow();
      m_RevealId = index + 1;
   }

   private void DrawComment(Rect cell, TagRow row, SerializedProperty entry)
   {
      if (entry == null)
      {
         string label = row.id == k_NewTagId ? "Enter a name to add the tag" : "Generated parent";
         GUI.Label(cell, label, EditorStyles.centeredGreyMiniLabel);
         return;
      }

      using SerializedProperty comment = entry.FindPropertyRelative("m_Comment");
      float height = CommentHeight(comment.stringValue);
      cell.y = cell.center.y - height * 0.5f;
      cell.height = height;
      using EditorGUI.PropertyScope scope = new(cell, GUIContent.none, comment);
      string text = EditorGUI.TextArea(cell, comment.stringValue, CommentStyle);
      if (text != comment.stringValue)
         comment.stringValue = text;
   }

   private void DrawAction(Rect cell, TagRow row)
   {
      Rect button = new(cell.center.x - 11, cell.center.y - 11, 22, 22);
      if (row.id == k_NewTagId)
      {
         if (GameplayTagEditorGUI.IconButton(button, "clear", "Cancel new definition"))
            ResetNewRow();
      }
      else if (row.EntryIndex >= 0 && GameplayTagEditorGUI.IconButton(button, "remove", "Remove definition (Undo supported)"))
      {
         m_RemoveIndex = row.EntryIndex;
      }
   }

   private void ResetNewRow()
   {
      m_Adding = false;
      m_NewName = string.Empty;
      m_FocusId = 0;
      m_InvalidId = 0;
   }

   private void RefreshRows()
   {
      using SerializedProperty entries = m_Library.FindProperty("m_Entries");
      int hash = HashCode.Combine(entries.arraySize, m_Adding);
      for (int i = 0; i < entries.arraySize; i++)
      {
         using SerializedProperty entry = entries.GetArrayElementAtIndex(i);
         using SerializedProperty name = entry.FindPropertyRelative("m_Name");
         hash = HashCode.Combine(hash, name.stringValue);
         if (hasSearch)
         {
            using SerializedProperty comment = entry.FindPropertyRelative("m_Comment");
            hash = HashCode.Combine(hash, comment.stringValue);
         }
      }
      if (hash == m_ContentHash && m_RevealId == 0)
         return;

      m_ContentHash = hash;
      Reload();
      if (m_RevealId != 0)
      {
         RevealRow(m_RevealId);
         m_RevealId = 0;
      }
   }

   private void RevealRow(int id)
   {
      TreeViewItem item = FindItem(id, rootItem);
      if (item == null)
         return;

      if (hasSearch && !DoesItemMatchSearch(item, searchString))
         searchString = string.Empty;
      for (TreeViewItem parent = item.parent; parent != null && parent.id != 0; parent = parent.parent)
         SetExpanded(parent.id, true);
      SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
   }

   private TagRow EnsureRow(string name, TreeViewItem root, Dictionary<string, TagRow> rows)
   {
      if (rows.TryGetValue(name, out TagRow row))
         return row;

      int separator = name.LastIndexOf('.');
      TreeViewItem parent = separator < 0 ? root : EnsureRow(name[..separator], root, rows);
      if (!m_ParentIds.TryGetValue(name, out int id))
      {
         id = m_NextParentId--;
         m_ParentIds.Add(name, id);
      }
      row = new TagRow(id, name);
      rows.Add(name, row);
      parent.AddChild(row);
      return row;
   }

   private string ReadComment(int index)
   {
      using SerializedProperty entries = m_Library.FindProperty("m_Entries");
      if (index >= entries.arraySize)
         return string.Empty;

      using SerializedProperty entry = entries.GetArrayElementAtIndex(index);
      using SerializedProperty comment = entry.FindPropertyRelative("m_Comment");
      return comment.stringValue;
   }

   private void ResizeCommentColumn(float width)
   {
      // GUILayout returns a dummy rect before it has positioned the table.
      if (width <= 2)
         return;

      MultiColumnHeaderState.Column[] columns = multiColumnHeader.state.columns;
      if (!Mathf.Approximately(columns[0].width, m_NameWidth))
         m_PreferredNameWidth = columns[0].width;

      float nameLimit = Mathf.Max(columns[0].minWidth, width - columns[1].minWidth - columns[2].width);
      m_NameWidth = Mathf.Clamp(m_PreferredNameWidth, columns[0].minWidth, nameLimit);
      columns[0].width = m_NameWidth;
      columns[1].width = Mathf.Max(columns[1].minWidth, width - m_NameWidth - columns[2].width);
   }

   private float CommentHeight(string comment)
   {
      float width = Mathf.Max(1, multiColumnHeader.state.columns[1].width - 10 - cellMargin * 2);
      // Measure the last line even when the comment ends in a newline.
      return Mathf.Ceil(CommentStyle.CalcHeight(new GUIContent(comment + "\u200B"), width));
   }

   private float MeasureRows()
   {
      IList<TreeViewItem> rows = GetRows();
      int hash = rows.Count;
      float height = multiColumnHeader.height;
      for (int i = 0; i < rows.Count; i++)
      {
         float row = GetCustomRowHeight(i, rows[i]);
         height += row;
         hash = HashCode.Combine(hash, rows[i].id, row);
      }
      if (hash != m_HeightHash)
      {
         m_HeightHash = hash;
         RefreshCustomRowHeights();
      }
      return height;
   }

   private static MultiColumnHeader CreateHeader()
   {
      MultiColumnHeaderState.Column[] columns =
      {
         new()
         {
            headerContent = new GUIContent("Tag", "Full name for explicit definitions. Generated parents can be expanded."),
            width = k_InitialNameWidth, minWidth = 132, autoResize = false, canSort = false, allowToggleVisibility = false
         },
         new()
         {
            headerContent = new GUIContent("Developer Comment", "Developer context for this tag."),
            width = 290, minWidth = 130, autoResize = true, canSort = false, allowToggleVisibility = false
         },
         new()
         {
            headerContent = new GUIContent("", "Remove a definition"),
            width = 40, minWidth = 40, maxWidth = 40, autoResize = false, canSort = false, allowToggleVisibility = false
         }
      };
      return new MultiColumnHeader(new MultiColumnHeaderState(columns)) { height = 24 };
   }

   private sealed class TagRow : TreeViewItem
   {
      internal readonly string FullName;
      internal int EntryIndex = -1;
      internal string Warning;

      internal TagRow(int id, string name) : base(id, 0, name[(name.LastIndexOf('.') + 1)..])
      {
         FullName = name;
      }
   }
}
