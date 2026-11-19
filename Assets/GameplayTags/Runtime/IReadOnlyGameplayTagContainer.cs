public interface IReadOnlyGameplayTagContainer
{
   /// <summary>The number of distinct explicitly added tags.</summary>
   int Count { get; }

   /// <summary>Returns an explicit tag by index. The order may change after a mutation.</summary>
   GameplayTag GetExplicitTag(int index);

   /// <summary>Matches an explicit tag or an ancestor implied by one.</summary>
   bool Contains(GameplayTag tag);

   /// <summary>Matches only an explicitly added tag.</summary>
   bool ContainsExact(GameplayTag tag);

   /// <summary>Checks whether every explicit tag in required matches, including implied matches.</summary>
   bool HasAll(IReadOnlyGameplayTagContainer required);

   /// <summary>Checks whether at least one explicit tag in required matches, including implied matches.</summary>
   bool HasAny(IReadOnlyGameplayTagContainer required);

   bool HasAllExact(IReadOnlyGameplayTagContainer required);
   bool HasAnyExact(IReadOnlyGameplayTagContainer required);
}
