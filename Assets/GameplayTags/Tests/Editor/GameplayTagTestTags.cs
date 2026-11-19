internal static class GameplayTagTestTags
{
   internal const string k_StatusName = "Tests.GameplayTags.Status";
   internal const string k_MarkName = k_StatusName + ".Mark";
   internal const string k_FluxName = k_MarkName + ".Flux";
   internal const string k_FloraName = k_MarkName + ".Flora";
   internal const string k_RetiredName = k_MarkName + ".Retired";
   internal const string k_SuddenDeathName = k_StatusName + ".Buff.SuddenDeath";
   internal const string k_SourceOnlyName = "Tests.GameplayTags.State.SourceOnly";
   internal const string k_FireName = "Tests.GameplayTags.Damage.Fire";
   internal const string k_FluxComment = "Native mark used by the serialization and container tests.";

   [NativeGameplayTag(k_FluxName, k_FluxComment)]
   internal static GameplayTag Flux { get; private set; }

   [NativeGameplayTag(k_FloraName)]
   internal static GameplayTag Flora { get; private set; }

   [NativeGameplayTag(k_SuddenDeathName)]
   internal static GameplayTag SuddenDeath { get; private set; }

   [NativeGameplayTag(k_SourceOnlyName)]
   internal static GameplayTag SourceOnly { get; private set; }

   [NativeGameplayTag(k_FireName)]
   internal static GameplayTag Fire;

   internal static GameplayTag Mark => Flux.Parent;
   internal static GameplayTag Status => Mark.Parent;
}
