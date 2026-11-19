using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

public sealed class GameplayTagCountContainer : IGameplayTagCountContainer
{
   public int Count => m_ExplicitTags.Count;

   private readonly List<GameplayTag> m_ExplicitTags;
   private readonly Dictionary<GameplayTag, (int Index, int Count)> m_ExplicitCounts;
   private readonly Dictionary<GameplayTag, int> m_EffectiveCounts;
   private readonly Dictionary<GameplayTag, List<Subscription>> m_Subscriptions = new();
   private readonly List<Notification> m_PendingNotifications = new();
   private bool m_IsDispatching;

   public GameplayTagCountContainer(int initialCapacity = 0)
   {
      m_ExplicitTags = new(initialCapacity);
      m_ExplicitCounts = new(initialCapacity);
      m_EffectiveCounts = new(initialCapacity);
   }

   public GameplayTag GetExplicitTag(int index) => m_ExplicitTags[index];

   public int GetCount(GameplayTag tag)
   {
      return m_EffectiveCounts.TryGetValue(tag, out int count) ? count : 0;
   }

   public int GetCountExact(GameplayTag tag)
   {
      return m_ExplicitCounts.TryGetValue(tag, out (int Index, int Count) entry) ? entry.Count : 0;
   }

   public bool Contains(GameplayTag tag) => GetCount(tag) > 0;
   public bool ContainsExact(GameplayTag tag) => GetCountExact(tag) > 0;

   public bool HasAll(IReadOnlyGameplayTagContainer required) =>
      GameplayTagContainerQueries.HasAll(this, required, exact: false);

   public bool HasAny(IReadOnlyGameplayTagContainer required) =>
      GameplayTagContainerQueries.HasAny(this, required, exact: false);

   public bool HasAllExact(IReadOnlyGameplayTagContainer required) =>
      GameplayTagContainerQueries.HasAll(this, required, exact: true);

   public bool HasAnyExact(IReadOnlyGameplayTagContainer required) =>
      GameplayTagContainerQueries.HasAny(this, required, exact: true);

   public bool Add(GameplayTag tag)
   {
      if (tag.IsEmpty)
         return false;

      if (m_ExplicitCounts.TryGetValue(tag, out (int Index, int Count) entry))
      {
         m_ExplicitCounts[tag] = (entry.Index, entry.Count + 1);
      }
      else
      {
         m_ExplicitCounts.Add(tag, (m_ExplicitTags.Count, 1));
         m_ExplicitTags.Add(tag);
      }

      ChangeHierarchyCounts(tag, 1);
      DispatchPendingNotifications();
      return true;
   }

   public bool Remove(GameplayTag tag)
   {
      if (!m_ExplicitCounts.TryGetValue(tag, out (int Index, int Count) entry))
         return false;

      if (entry.Count == 1)
         RemoveExplicitTag(tag, entry.Index);
      else
         m_ExplicitCounts[tag] = (entry.Index, entry.Count - 1);

      ChangeHierarchyCounts(tag, -1);
      DispatchPendingNotifications();
      return true;
   }

   public void Clear()
   {
      foreach (KeyValuePair<GameplayTag, int> entry in m_EffectiveCounts)
         EnqueueNotifications(entry.Key, entry.Value, 0);

      m_ExplicitTags.Clear();
      m_ExplicitCounts.Clear();
      m_EffectiveCounts.Clear();
      DispatchPendingNotifications();
   }

   public IDisposable Subscribe(GameplayTag tag, GameplayTagEventType eventType, Action<GameplayTag, int> listener)
   {
      if (tag.IsEmpty)
         throw new ArgumentException("An event subscription requires a tag.", nameof(tag));
      if (eventType != GameplayTagEventType.NewOrRemoved && eventType != GameplayTagEventType.AnyCountChange)
         throw new ArgumentOutOfRangeException(nameof(eventType));
      if (listener == null)
         throw new ArgumentNullException(nameof(listener));

      if (!m_Subscriptions.TryGetValue(tag, out List<Subscription> subscriptions))
      {
         subscriptions = new();
         m_Subscriptions.Add(tag, subscriptions);
      }

      Subscription subscription = new(this, tag, eventType, listener);
      subscriptions.Add(subscription);
      return subscription;
   }

   private void RemoveExplicitTag(GameplayTag tag, int index)
   {
      // Keep indexed access without shifting every remaining entry.
      int lastIndex = m_ExplicitTags.Count - 1;
      if (index != lastIndex)
      {
         GameplayTag lastTag = m_ExplicitTags[lastIndex];
         m_ExplicitTags[index] = lastTag;
         m_ExplicitCounts[lastTag] = (index, m_ExplicitCounts[lastTag].Count);
      }

      m_ExplicitTags.RemoveAt(lastIndex);
      m_ExplicitCounts.Remove(tag);
   }

   private void ChangeHierarchyCounts(GameplayTag tag, int change)
   {
      for (GameplayTag current = tag; !current.IsEmpty; current = current.Parent)
      {
         int previousCount = GetCount(current);
         int count = previousCount + change;

         if (count == 0)
            m_EffectiveCounts.Remove(current);
         else
            m_EffectiveCounts[current] = count;

         EnqueueNotifications(current, previousCount, count);
      }
   }

   private void EnqueueNotifications(GameplayTag tag, int previousCount, int count)
   {
      if (!m_Subscriptions.TryGetValue(tag, out List<Subscription> subscriptions))
         return;

      bool presenceChanged = previousCount == 0 || count == 0;
      foreach (Subscription subscription in subscriptions)
      {
         if (presenceChanged || subscription.EventType == GameplayTagEventType.AnyCountChange)
            m_PendingNotifications.Add(new Notification(subscription, count));
      }
   }

   private void DispatchPendingNotifications()
   {
      if (m_IsDispatching || m_PendingNotifications.Count == 0)
         return;

      m_IsDispatching = true;
      ExceptionDispatchInfo firstFailure = null;
      try
      {
         // List entries capture the count before invoking listeners. Reentrant mutations append
         // notifications so every listener sees the original changes in the order they were queued.
         for (int index = 0; index < m_PendingNotifications.Count; index++)
         {
            Notification notification = m_PendingNotifications[index];
            try
            {
               notification.Subscription.Notify(notification.Count);
            }
            catch (Exception exception)
            {
               firstFailure ??= ExceptionDispatchInfo.Capture(exception);
            }
         }
      }
      finally
      {
         m_PendingNotifications.Clear();
         m_IsDispatching = false;
      }

      firstFailure?.Throw();
   }

   private void Unsubscribe(Subscription subscription)
   {
      List<Subscription> subscriptions = m_Subscriptions[subscription.Tag];
      subscriptions.Remove(subscription);
      if (subscriptions.Count == 0)
         m_Subscriptions.Remove(subscription.Tag);
   }

   private readonly struct Notification
   {
      public Subscription Subscription { get; }
      public int Count { get; }

      public Notification(Subscription subscription, int count)
      {
         Subscription = subscription;
         Count = count;
      }
   }

   private sealed class Subscription : IDisposable
   {
      public GameplayTag Tag { get; }
      public GameplayTagEventType EventType { get; }

      private GameplayTagCountContainer m_Owner;
      private Action<GameplayTag, int> m_Listener;

      public Subscription(GameplayTagCountContainer owner, GameplayTag tag, GameplayTagEventType eventType, Action<GameplayTag, int> listener)
      {
         m_Owner = owner;
         Tag = tag;
         EventType = eventType;
         m_Listener = listener;
      }

      public void Notify(int count)
      {
         m_Listener?.Invoke(Tag, count);
      }

      public void Dispose()
      {
         GameplayTagCountContainer owner = m_Owner;
         if (owner == null)
            return;

         m_Owner = null;
         m_Listener = null;
         owner.Unsubscribe(this);
      }
   }
}
