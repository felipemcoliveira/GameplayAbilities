using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

internal sealed class GameplayTagTreeView : TreeView
{
   internal int TagCount => m_Tags.Length;

   private readonly Func<string, bool> m_IsExplicit;
   private readonly Func<string, bool> m_IsInherited;
   private readonly Action<GameplayTag, bool> m_ToggleTag;
   private readonly Dictionary<GameplayTag, int> m_ItemIds = new();
   private GameplayTag[] m_Tags = Array.Empty<GameplayTag>();

   internal GameplayTagTreeView(Func<string, bool> isExplicit, Func<string, bool> isInherited,
      Action<GameplayTag, bool> toggleTag) : base(new TreeViewState())
   {
      m_IsExplicit = isExplicit;
      m_IsInherited = isInherited;
      m_ToggleTag = toggleTag;
      rowHeight = 24;
      showAlternatingRowBackgrounds = true;
   }

   internal void SetTags(GameplayTag[] tags)
   {
      m_Tags = tags;
      Reload();
   }

   protected override TreeViewItem BuildRoot()
   {
      TreeViewItem root = new(0, -1, "Root") { children = new List<TreeViewItem>() };
      Dictionary<GameplayTag, TagItem> items = new(m_Tags.Length);
      foreach (GameplayTag tag in m_Tags)
      {
         if (!m_ItemIds.TryGetValue(tag, out int id))
         {
            id = m_ItemIds.Count + 1;
            m_ItemIds.Add(tag, id);
         }
         items.Add(tag, new TagItem(id, tag));
      }

      foreach (GameplayTag tag in m_Tags)
      {
         if (items.TryGetValue(tag.Parent, out TagItem parent))
            parent.AddChild(items[tag]);
         else
            root.AddChild(items[tag]);
      }

      SetupDepthsFromParentsAndChildren(root);
      return root;
   }

   protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
   {
      if (item is not TagItem tagItem)
         return false;
      if (tagItem.Tag.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
         return true;

      foreach (string comment in tagItem.Comments)
      {
         if (comment.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;
      }
      return false;
   }

   protected override void RowGUI(RowGUIArgs args)
   {
      TagItem item = (TagItem)args.item;
      Rect rect = args.rowRect;
      rect.xMin += GetContentIndent(item);
      Rect checkbox = new(rect.x, rect.y + 3, 18, 18);
      bool selected = m_IsExplicit(item.Tag.Name);
      bool inherited = !selected && m_IsInherited(item.Tag.Name);
      bool previousMixed = EditorGUI.showMixedValue;
      EditorGUI.showMixedValue = inherited;
      using EditorGUI.ChangeCheckScope check = new();
      bool toggled = EditorGUI.Toggle(checkbox, selected);
      EditorGUI.showMixedValue = previousMixed;

      rect.xMin += 22;
      GameplayTagEditorGUI.DrawIcon(new Rect(rect.x, rect.center.y - 7, 14, 14), "tag");
      rect.xMin += 21;
      string label = hasSearch ? item.Tag.Name : item.displayName;
      string tooltip = inherited ? item.Tooltip + "\nInherited from a selected child. Click to add it explicitly." : item.Tooltip;
      GUI.Label(rect, new GUIContent(label, tooltip), selected ? EditorStyles.boldLabel : EditorStyles.label);
      if (check.changed)
         m_ToggleTag(item.Tag, inherited || toggled);
   }

   private sealed class TagItem : TreeViewItem
   {
      internal GameplayTag Tag { get; }
      internal IReadOnlyList<string> Comments { get; }
      internal string Tooltip { get; }

      internal TagItem(int id, GameplayTag tag) : base(id, 0, tag.Name[(tag.Name.LastIndexOf('.') + 1)..])
      {
         Tag = tag;
         GameplayTagManager.TryGetMetadata(tag, out GameplayTagMetadata metadata);
         Comments = metadata?.Comments ?? Array.Empty<string>();
         Tooltip = Comments.Count > 0 ? tag.Name + "\n" + string.Join("\n", Comments) : tag.Name;
      }
   }
}
