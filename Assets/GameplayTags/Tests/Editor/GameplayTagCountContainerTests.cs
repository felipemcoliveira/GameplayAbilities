using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class GameplayTagCountContainerTests
{
   [Test]
   public void CountsCombineDirectStacksAndDifferentDescendants()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTag suddenDeath = GameplayTagTestTags.SuddenDeath;
      GameplayTag status = GameplayTagTestTags.Status;
      GameplayTagCountContainer container = new();

      Assert.That(container.Add(flux), Is.True);
      Assert.That(container.Add(flux), Is.True);
      container.Add(suddenDeath);
      container.Add(status);

      Assert.That(container.Count, Is.EqualTo(3));
      Assert.That(container.GetCountExact(flux), Is.EqualTo(2));
      Assert.That(container.GetCountExact(status), Is.EqualTo(1));
      Assert.That(container.GetCount(status), Is.EqualTo(4));
      Assert.That(container.Remove(flux.Parent), Is.False);
      Assert.That(container.GetCount(status), Is.EqualTo(4));

      Assert.That(container.Remove(status), Is.True);
      Assert.That(container.Contains(status), Is.True);
      Assert.That(container.ContainsExact(status), Is.False);
      Assert.That(container.GetCount(status), Is.EqualTo(3));

      container.Remove(flux);
      Assert.That(container.GetCountExact(flux), Is.EqualTo(1));
      container.Remove(flux);
      Assert.That(container.Contains(flux), Is.False);
      Assert.That(container.GetCount(status), Is.EqualTo(1));
      Assert.That(container.GetExplicitTag(0), Is.EqualTo(suddenDeath));
      Assert.That(container.Remove(flux), Is.False);

      container.Remove(suddenDeath);
      Assert.That(container.Count, Is.Zero);
      Assert.That(container.Contains(status), Is.False);
   }

   [Test]
   public void AncestorListenersCanObserveEveryChangeOrOnlyZeroCrossings()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTag mark = flux.Parent;
      GameplayTagCountContainer container = new();
      List<int> countChanges = new();
      List<int> presenceChanges = new();
      List<GameplayTag> reportedTags = new();

      container.Add(flux);
      using IDisposable any = container.Subscribe(mark, GameplayTagEventType.AnyCountChange, (tag, count) =>
      {
         reportedTags.Add(tag);
         countChanges.Add(count);
      });
      using IDisposable presence = container.Subscribe(mark, GameplayTagEventType.NewOrRemoved, (_, count) =>
      {
         presenceChanges.Add(count);
      });
      Assert.That(countChanges, Is.Empty);
      Assert.That(presenceChanges, Is.Empty);

      container.Add(flux);
      container.Remove(flux);
      container.Remove(flux);
      container.Add(flux);

      Assert.That(countChanges, Is.EqualTo(new[] { 2, 1, 0, 1 }));
      Assert.That(presenceChanges, Is.EqualTo(new[] { 0, 1 }));
      Assert.That(reportedTags, Is.All.EqualTo(mark));
   }

   [Test]
   public void ClearReportsZeroForBothEventTypesAfterAllCountsAreCleared()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTag mark = flux.Parent;
      GameplayTagCountContainer container = new();
      List<int> childCounts = new();
      List<int> parentCounts = new();

      container.Add(flux);
      container.Add(flux);
      using IDisposable child = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, (_, count) =>
      {
         childCounts.Add(count);
         Assert.That(container.Contains(mark), Is.EqualTo(count > 0));
      });
      using IDisposable parent = container.Subscribe(mark, GameplayTagEventType.NewOrRemoved, (_, count) =>
      {
         parentCounts.Add(count);
         Assert.That(container.ContainsExact(flux), Is.EqualTo(count > 0));
      });

      container.Clear();
      container.Clear();
      Assert.That(container.Count, Is.Zero);
      Assert.That(childCounts, Is.EqualTo(new[] { 0 }));
      Assert.That(parentCounts, Is.EqualTo(new[] { 0 }));

      container.Add(flux);
      Assert.That(childCounts, Is.EqualTo(new[] { 0, 1 }));
      Assert.That(parentCounts, Is.EqualTo(new[] { 0, 1 }));
   }

   [Test]
   public void DuplicateDelegatesHaveIndependentIdempotentSubscriptions()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTagCountContainer container = new();
      List<int> counts = new();
      Action<GameplayTag, int> listener = (_, count) => counts.Add(count);

      using IDisposable first = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, listener);
      using IDisposable second = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, listener);
      container.Add(flux);
      Assert.That(counts, Is.EqualTo(new[] { 1, 1 }));

      first.Dispose();
      first.Dispose();
      Assert.That(container.GetCountExact(flux), Is.EqualTo(1));
      container.Add(flux);
      Assert.That(counts, Is.EqualTo(new[] { 1, 1, 2 }));

      second.Dispose();
      container.Remove(flux);
      Assert.That(counts, Is.EqualTo(new[] { 1, 1, 2 }));
      Assert.That(container.GetCountExact(flux), Is.EqualTo(1));
   }

   [Test]
   public void ReentrantMutationQueuesNotificationsAfterAlreadyQueuedChanges()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTag mark = flux.Parent;
      GameplayTagCountContainer container = new();
      List<int> ancestorCounts = new();

      using IDisposable child = container.Subscribe(flux, GameplayTagEventType.NewOrRemoved, (_, count) =>
      {
         if (count == 1)
         {
            Assert.That(container.GetCount(mark), Is.EqualTo(1));
            Assert.That(container.Remove(flux), Is.True);
         }
      });
      using IDisposable parent = container.Subscribe(mark, GameplayTagEventType.AnyCountChange, (_, count) =>
      {
         ancestorCounts.Add(count);
      });

      container.Add(flux);

      Assert.That(ancestorCounts, Is.EqualTo(new[] { 1, 0 }));
      Assert.That(container.Count, Is.Zero);
      Assert.That(container.GetCount(mark), Is.Zero);
   }

   [Test]
   public void DisposingAnotherListenerBeforeItsNotificationSkipsThatCall()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTagCountContainer container = new();
      int disposedListenerCalls = 0;
      IDisposable later = null;

      using IDisposable first = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, (_, _) => later.Dispose());
      using IDisposable second = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, (_, _) => disposedListenerCalls++);
      later = second;

      container.Add(flux);
      container.Remove(flux);

      Assert.That(disposedListenerCalls, Is.Zero);
   }

   [Test]
   public void ThrowingListenerDoesNotBlockOtherListenersOrLaterMutations()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTagCountContainer container = new();
      List<int> counts = new();
      InvalidOperationException failure = new("Listener failed.");

      using IDisposable throwing = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, (_, _) =>
      {
         throw failure;
      });
      using IDisposable other = container.Subscribe(flux, GameplayTagEventType.AnyCountChange, (_, count) => counts.Add(count));

      Assert.That(Assert.Throws<InvalidOperationException>(() => container.Add(flux)), Is.SameAs(failure));
      Assert.That(counts, Is.EqualTo(new[] { 1 }));
      Assert.That(container.GetCountExact(flux), Is.EqualTo(1));

      throwing.Dispose();
      container.Remove(flux);
      Assert.That(counts, Is.EqualTo(new[] { 1, 0 }));
   }

   [Test]
   public void ReadOnlyQueriesInteroperateWithRegularContainers()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTagCountContainer storage = new();
      GameplayTagContainer required = new();
      IReadOnlyGameplayTagContainer active = storage;

      storage.Add(flux);
      storage.Add(flux);
      required.Add(flux.Parent);

      Assert.That(active.HasAll(required), Is.True);
      Assert.That(active.HasAny(required), Is.True);
      Assert.That(active.HasAllExact(required), Is.False);
      Assert.That(active.HasAnyExact(required), Is.False);
      Assert.That(required.HasAny(active), Is.False);
      Assert.That(active.Count, Is.EqualTo(1));
   }
}
