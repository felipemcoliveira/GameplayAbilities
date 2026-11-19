using System.Collections.Generic;

/// <summary>Stores exact tags without adding their ancestors.</summary>
public sealed class GameplayTagContainer
{
   public int Count => m_Tags.Count;

   private readonly HashSet<GameplayTag> m_Tags;

   /// <summary>Reserve capacity up front to avoid growing the set during gameplay.</summary>
   public GameplayTagContainer(int initialCapacity = 0)
   {
      m_Tags = new HashSet<GameplayTag>(initialCapacity);
   }

   /// <summary>Adds a nonempty tag and returns whether it was newly added.</summary>
   public bool Add(GameplayTag tag)
   {
      return !tag.IsEmpty && m_Tags.Add(tag);
   }

   public bool Remove(GameplayTag tag)
   {
      return !tag.IsEmpty && m_Tags.Remove(tag);
   }

   public bool Contains(GameplayTag tag)
   {
      return !tag.IsEmpty && m_Tags.Contains(tag);
   }

   public void Clear()
   {
      m_Tags.Clear();
   }
}
