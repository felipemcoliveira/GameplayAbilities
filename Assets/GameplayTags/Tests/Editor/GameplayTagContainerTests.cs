using NUnit.Framework;

public sealed class GameplayTagContainerTests
{
   private GameplayTag m_Flux;
   private GameplayTag m_Mark;
   private GameplayTag m_SuddenDeath;
   private GameplayTag m_Status;
   private GameplayTag m_Fire;

   [SetUp]
   public void ResolveExampleTags()
   {
      m_Flux = GameplayTagManager.GetTag("Status.Mark.Flux");
      m_Mark = GameplayTagManager.GetTag("Status.Mark");
      m_SuddenDeath = GameplayTagManager.GetTag("Status.Buff.SuddenDeath");
      m_Status = GameplayTagManager.GetTag("Status");
      m_Fire = GameplayTagManager.GetTag("Damage.Fire");
   }

   [Test]
   public void AddMakesAncestorsVisibleWithoutAddingExplicitEntries()
   {
      GameplayTagContainer container = new();

      Assert.That(container.Add(m_Flux), Is.True);
      Assert.That(container.Count, Is.EqualTo(1));
      Assert.That(container.GetExplicitTag(0), Is.EqualTo(m_Flux));
      Assert.That(container.ContainsExact(m_Flux), Is.True);
      Assert.That(container.Contains(m_Mark), Is.True);
      Assert.That(container.Contains(m_Status), Is.True);
      Assert.That(container.ContainsExact(m_Mark), Is.False);
      Assert.That(container.ContainsExact(m_Status), Is.False);
      Assert.That(container.Contains(m_SuddenDeath), Is.False);
   }

   [Test]
   public void RemovingOneDescendantKeepsTheSharedAncestorUntilItsLastReference()
   {
      GameplayTagContainer container = WithTags(m_Flux, m_SuddenDeath);

      Assert.That(container.Remove(m_Flux), Is.True);
      Assert.That(container.Contains(m_Mark), Is.False);
      Assert.That(container.Contains(m_Status), Is.True);
      Assert.That(container.Remove(m_SuddenDeath), Is.True);
      Assert.That(container.Contains(m_Status), Is.False);
   }

   [TestCase(true)]
   [TestCase(false)]
   public void AddingAnImpliedParentExplicitlyKeepsTheTwoContributionsSeparate(bool removeParentFirst)
   {
      GameplayTagContainer container = WithTags(m_Flux);

      Assert.That(container.Remove(m_Mark), Is.False);
      Assert.That(container.Add(m_Mark), Is.True);
      Assert.That(container.ContainsExact(m_Mark), Is.True);
      Assert.That(container.Count, Is.EqualTo(2));

      if (removeParentFirst)
      {
         Assert.That(container.Remove(m_Mark), Is.True);
         Assert.That(container.Contains(m_Mark), Is.True);
         Assert.That(container.ContainsExact(m_Mark), Is.False);
         Assert.That(container.Remove(m_Flux), Is.True);
      }
      else
      {
         Assert.That(container.Remove(m_Flux), Is.True);
         Assert.That(container.ContainsExact(m_Mark), Is.True);
         Assert.That(container.Remove(m_Mark), Is.True);
      }

      Assert.That(container.Contains(m_Mark), Is.False);
      Assert.That(container.Contains(m_Status), Is.False);
   }

   [Test]
   public void DuplicateAndEmptyTagsDoNotChangeMembership()
   {
      GameplayTagContainer container = WithTags(m_Flux);

      Assert.That(container.Add(m_Flux), Is.False);
      Assert.That(container.Add(default), Is.False);
      Assert.That(container.Remove(default), Is.False);
      Assert.That(container.Contains(default), Is.False);
      Assert.That(container.Count, Is.EqualTo(1));
      Assert.That(container.Remove(m_Flux), Is.True);
      Assert.That(container.Contains(m_Mark), Is.False);
   }

   [Test]
   public void QueriesUseOnlyTheExplicitEntriesInTheRequirementContainer()
   {
      IReadOnlyGameplayTagContainer container = WithTags(m_Flux);
      IReadOnlyGameplayTagContainer ancestor = WithTags(m_Mark);
      IReadOnlyGameplayTagContainer mixed = WithTags(m_Flux, m_Fire);

      Assert.That(container.HasAllExact(WithTags(m_Flux)), Is.True);
      Assert.That(container.HasAll(ancestor), Is.True);
      Assert.That(container.HasAny(ancestor), Is.True);
      Assert.That(container.HasAllExact(ancestor), Is.False);
      Assert.That(container.HasAnyExact(ancestor), Is.False);
      Assert.That(ancestor.HasAny(container), Is.False);
      Assert.That(container.HasAll(mixed), Is.False);
      Assert.That(container.HasAny(mixed), Is.True);
      Assert.That(container.HasAllExact(mixed), Is.False);
      Assert.That(container.HasAnyExact(mixed), Is.True);
   }

   [Test]
   public void EmptyRequirementsMatchAllButDoNotMatchAny()
   {
      GameplayTagContainer container = WithTags(m_Flux);
      GameplayTagContainer empty = new();

      Assert.That(container.HasAll(empty), Is.True);
      Assert.That(container.HasAllExact(empty), Is.True);
      Assert.That(container.HasAny(empty), Is.False);
      Assert.That(container.HasAnyExact(empty), Is.False);
   }

   [Test]
   public void RemovingNonLastEntriesKeepsTheRemainingEntriesRemovable()
   {
      GameplayTagContainer container = WithTags(m_Flux, m_SuddenDeath, m_Fire);

      Assert.That(container.Remove(m_Flux), Is.True);
      Assert.That(container.Remove(m_Fire), Is.True);
      Assert.That(container.GetExplicitTag(0), Is.EqualTo(m_SuddenDeath));
      Assert.That(container.Contains(m_Status), Is.True);
      Assert.That(container.Contains(m_Fire.Parent), Is.False);
      Assert.That(container.Count, Is.EqualTo(1));
   }

   [Test]
   public void ClearRemovesExplicitAndImpliedMembership()
   {
      GameplayTagContainer container = WithTags(m_Flux, m_Fire);

      container.Clear();

      Assert.That(container.Count, Is.Zero);
      Assert.That(container.ContainsExact(m_Flux), Is.False);
      Assert.That(container.Contains(m_Mark), Is.False);
      Assert.That(container.Contains(m_Fire.Parent), Is.False);
      Assert.That(container.Add(m_Flux), Is.True);
      Assert.That(container.Contains(m_Mark), Is.True);
   }

   private static GameplayTagContainer WithTags(params GameplayTag[] tags)
   {
      GameplayTagContainer container = new(tags.Length);
      foreach (GameplayTag tag in tags)
         container.Add(tag);

      return container;
   }
}
