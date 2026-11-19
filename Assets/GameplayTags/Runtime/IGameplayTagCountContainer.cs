using System;

public interface IGameplayTagCountContainer : IGameplayTagContainer
{
   /// <summary>Returns the count contributed by this tag and all its descendants.</summary>
   int GetCount(GameplayTag tag);

   /// <summary>Returns the number of times this exact tag was added.</summary>
   int GetCountExact(GameplayTag tag);

   /// <summary>Listens for changes to the effective count. Dispose the handle to unsubscribe.</summary>
   IDisposable Subscribe(GameplayTag tag, GameplayTagEventType eventType, Action<GameplayTag, int> listener);
}
