using System;

internal static class GameplayTagContainerQueries
{
   internal static bool HasAll(IReadOnlyGameplayTagContainer source, IReadOnlyGameplayTagContainer required, bool exact)
   {
      return Match(source, required, exact, requireAll: true);
   }

   internal static bool HasAny(IReadOnlyGameplayTagContainer source, IReadOnlyGameplayTagContainer required, bool exact)
   {
      return Match(source, required, exact, requireAll: false);
   }

   private static bool Match(IReadOnlyGameplayTagContainer source, IReadOnlyGameplayTagContainer required, bool exact, bool requireAll)
   {
      if (required == null)
         throw new ArgumentNullException(nameof(required));

      for (int i = 0; i < required.Count; i++)
      {
         GameplayTag tag = required.GetExplicitTag(i);
         bool matches = exact ? source.ContainsExact(tag) : source.Contains(tag);
         if (matches != requireAll)
            return !requireAll;
      }

      return requireAll;
   }
}
