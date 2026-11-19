using System;
using System.Collections.Generic;

/// <summary>A read-only view that supports allocation-free indexing and direct foreach.</summary>
public readonly struct GameplayTagChildren
{
   public int Count => m_Children?.Count ?? 0;

   public GameplayTag this[int index]
   {
      get
      {
         if (m_Children == null)
            throw new ArgumentOutOfRangeException(nameof(index));

         return m_Children[index];
      }
   }

   private readonly List<GameplayTag> m_Children;

   internal GameplayTagChildren(List<GameplayTag> children)
   {
      m_Children = children;
   }

   public Enumerator GetEnumerator()
   {
      return new Enumerator(m_Children);
   }

   public struct Enumerator
   {
      public readonly GameplayTag Current => m_Enumerator.Current;

      private List<GameplayTag>.Enumerator m_Enumerator;
      private readonly bool m_HasList;

      internal Enumerator(List<GameplayTag> children)
      {
         m_Enumerator = children?.GetEnumerator() ?? default;
         m_HasList = children != null;
      }

      public bool MoveNext()
      {
         return m_HasList && m_Enumerator.MoveNext();
      }
   }
}
