using System;
using System.Collections.Generic;
using System.Threading;

public sealed class GameplayTagMetadata
{
   public GameplayTag Tag { get; }
   public GameplayTag Parent { get; }
   public GameplayTagChildren Children => new(Volatile.Read(ref m_Children));
   public bool IsExplicit => m_IsExplicit;
   public IReadOnlyList<string> Comments => Volatile.Read(ref m_Comments);

   private GameplayTag[] m_Children = Array.Empty<GameplayTag>();
   private IReadOnlyList<string> m_Comments = Array.Empty<string>();
   private volatile bool m_IsExplicit;

   internal GameplayTagMetadata(GameplayTag tag, GameplayTag parent)
   {
      Tag = tag;
      Parent = parent;
   }

   internal void AddChild(GameplayTag child)
   {
      GameplayTag[] children = new GameplayTag[m_Children.Length + 1];
      int index = 0;
      while (index < m_Children.Length && string.CompareOrdinal(m_Children[index].Name, child.Name) < 0)
         index++;

      Array.Copy(m_Children, 0, children, 0, index);
      children[index] = child;
      Array.Copy(m_Children, index, children, index + 1, m_Children.Length - index);
      Volatile.Write(ref m_Children, children);
   }

   internal void Declare(string comment)
   {
      m_IsExplicit = true;
      if (string.IsNullOrEmpty(comment))
         return;

      for (int i = 0; i < m_Comments.Count; i++)
      {
         if (string.Equals(m_Comments[i], comment, StringComparison.Ordinal))
            return;
      }

      string[] comments = new string[m_Comments.Count + 1];
      for (int i = 0; i < m_Comments.Count; i++)
         comments[i] = m_Comments[i];

      comments[^1] = comment;
      Volatile.Write(ref m_Comments, Array.AsReadOnly(comments));
   }
}
