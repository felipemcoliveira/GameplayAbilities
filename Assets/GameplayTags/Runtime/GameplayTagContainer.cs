using System.Collections.Generic;

/// <summary>Stores unique explicit tags and tracks the ancestors they imply.</summary>
public sealed class GameplayTagContainer
{
   public int Count => m_ExplicitTags.Count;

   private readonly List<GameplayTag> m_ExplicitTags;
   private readonly Dictionary<GameplayTag, int> m_ExplicitIndices;
   private readonly Dictionary<GameplayTag, int> m_InheritedCounts;

   /// <summary>Reserves initial storage for explicit tags and ancestor counts.</summary>
   public GameplayTagContainer(int initialCapacity = 0)
   {
      m_ExplicitTags = new List<GameplayTag>(initialCapacity);
      m_ExplicitIndices = new Dictionary<GameplayTag, int>(initialCapacity);
      m_InheritedCounts = new Dictionary<GameplayTag, int>(initialCapacity);
   }

   /// <summary>Adds a nonempty tag explicitly and registers its ancestors as implied.</summary>
   public bool Add(GameplayTag tag)
   {
      if (tag.IsEmpty || !m_ExplicitIndices.TryAdd(tag, m_ExplicitTags.Count))
         return false;

      m_ExplicitTags.Add(tag);
      for (GameplayTag parent = tag.Parent; !parent.IsEmpty; parent = parent.Parent)
      {
         m_InheritedCounts.TryGetValue(parent, out int count);
         m_InheritedCounts[parent] = count + 1;
      }

      return true;
   }

   public bool Remove(GameplayTag tag)
   {
      if (!m_ExplicitIndices.Remove(tag, out int index))
         return false;

      // Move the last explicit tag into the gap to avoid shifting the list.
      int lastIndex = m_ExplicitTags.Count - 1;
      if (index != lastIndex)
      {
         GameplayTag last = m_ExplicitTags[lastIndex];
         m_ExplicitTags[index] = last;
         m_ExplicitIndices[last] = index;
      }
      m_ExplicitTags.RemoveAt(lastIndex);

      for (GameplayTag parent = tag.Parent; !parent.IsEmpty; parent = parent.Parent)
      {
         int count = m_InheritedCounts[parent];
         if (count == 1)
            m_InheritedCounts.Remove(parent);
         else
            m_InheritedCounts[parent] = count - 1;
      }

      return true;
   }

   public bool Contains(GameplayTag tag)
   {
      return ContainsExact(tag) || m_InheritedCounts.ContainsKey(tag);
   }

   public bool ContainsExact(GameplayTag tag)
   {
      return m_ExplicitIndices.ContainsKey(tag);
   }

   public GameplayTag GetExplicitTag(int index)
   {
      return m_ExplicitTags[index];
   }

   public bool HasAll(GameplayTagContainer required) => Match(required, exact: false, requireAll: true);
   public bool HasAny(GameplayTagContainer required) => Match(required, exact: false, requireAll: false);
   public bool HasAllExact(GameplayTagContainer required) => Match(required, exact: true, requireAll: true);
   public bool HasAnyExact(GameplayTagContainer required) => Match(required, exact: true, requireAll: false);

   private bool Match(GameplayTagContainer required, bool exact, bool requireAll)
   {
      if (required == null)
         throw new System.ArgumentNullException(nameof(required));

      for (int i = 0; i < required.Count; i++)
      {
         GameplayTag tag = required.GetExplicitTag(i);
         bool matches = exact ? ContainsExact(tag) : Contains(tag);
         if (matches != requireAll)
            return !requireAll;
      }

      return requireAll;
   }

   public void Clear()
   {
      m_ExplicitTags.Clear();
      m_ExplicitIndices.Clear();
      m_InheritedCounts.Clear();
   }
}
