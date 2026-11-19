using System;

/// <summary>A read-only snapshot of a tag's direct children.</summary>
public readonly struct GameplayTagChildren
{
   public int Count => m_Children?.Length ?? 0;

   public GameplayTag this[int index] => new ReadOnlySpan<GameplayTag>(m_Children)[index];

   private readonly GameplayTag[] m_Children;

   internal GameplayTagChildren(GameplayTag[] children)
   {
      m_Children = children;
   }

   public ReadOnlySpan<GameplayTag>.Enumerator GetEnumerator()
   {
      return new ReadOnlySpan<GameplayTag>(m_Children).GetEnumerator();
   }
}
