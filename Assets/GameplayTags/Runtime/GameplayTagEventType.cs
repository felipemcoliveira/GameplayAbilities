public enum GameplayTagEventType
{
   /// <summary>Notify when a tag's effective count crosses zero.</summary>
   NewOrRemoved,

   /// <summary>Notify on every effective count change, including entering or leaving the container.</summary>
   AnyCountChange
}
