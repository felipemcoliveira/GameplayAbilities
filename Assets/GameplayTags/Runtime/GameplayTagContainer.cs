using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Stores unique explicit tags and tracks the ancestors they imply.</summary>
[Serializable]
public sealed class GameplayTagContainer : IGameplayTagContainer, ISerializationCallbackReceiver
{
   public int Count => m_ExplicitTags.Count;

   [SerializeField]
   private List<GameplayTag> m_ExplicitTags;

   [NonSerialized]
   private Dictionary<GameplayTag, int> m_ExplicitIndices;

   [NonSerialized]
   private Dictionary<GameplayTag, int> m_InheritedCounts;

   public GameplayTagContainer() : this(0)
   {
   }

   /// <summary>Reserves initial storage for explicit tags and ancestor counts.</summary>
   public GameplayTagContainer(int initialCapacity)
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
      AddInheritedTags(tag);
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

   public bool HasAll(IReadOnlyGameplayTagContainer required) => GameplayTagContainerQueries.HasAll(this, required, exact: false);
   public bool HasAny(IReadOnlyGameplayTagContainer required) => GameplayTagContainerQueries.HasAny(this, required, exact: false);
   public bool HasAllExact(IReadOnlyGameplayTagContainer required) => GameplayTagContainerQueries.HasAll(this, required, exact: true);
   public bool HasAnyExact(IReadOnlyGameplayTagContainer required) => GameplayTagContainerQueries.HasAny(this, required, exact: true);

   public void Clear()
   {
      m_ExplicitTags.Clear();
      m_ExplicitIndices.Clear();
      m_InheritedCounts.Clear();
   }

   void ISerializationCallbackReceiver.OnBeforeSerialize()
   {
   }

   void ISerializationCallbackReceiver.OnAfterDeserialize()
   {
      m_ExplicitTags ??= new();
      m_ExplicitIndices ??= new(m_ExplicitTags.Count);
      m_InheritedCounts ??= new(m_ExplicitTags.Count);
      m_ExplicitIndices.Clear();
      m_InheritedCounts.Clear();

      int writeIndex = 0;
      for (int readIndex = 0; readIndex < m_ExplicitTags.Count; readIndex++)
      {
         if (!GameplayTagManager.TryRegisterSerializedTag(m_ExplicitTags[readIndex].Name, out GameplayTag tag))
            continue;

         if (!m_ExplicitIndices.TryAdd(tag, writeIndex))
            continue;

         // Keep the rebuilt hash even if Unity has not called the nested tag's callback yet.
         m_ExplicitTags[writeIndex++] = tag;
         AddInheritedTags(tag);
      }

      m_ExplicitTags.RemoveRange(writeIndex, m_ExplicitTags.Count - writeIndex);
   }

   private void AddInheritedTags(GameplayTag tag)
   {
      for (GameplayTag parent = tag.Parent; !parent.IsEmpty; parent = parent.Parent)
      {
         m_InheritedCounts.TryGetValue(parent, out int count);
         m_InheritedCounts[parent] = count + 1;
      }
   }
}
