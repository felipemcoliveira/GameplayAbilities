using System.Collections.Generic;

public static class GameplayTagManager
{
   private static readonly GameplayTagRegistry s_Registry = new();
   private static bool s_SourcesLoaded;

   public static GameplayTag GetTag(string name)
   {
      if (TryGetTag(name, out GameplayTag tag))
         return tag;

      throw new KeyNotFoundException($"Gameplay tag '{name}' is not registered by a source.");
   }

   public static bool TryGetTag(string name, out GameplayTag tag)
   {
      if (!string.IsNullOrEmpty(name))
      {
         Initialize();
         lock (s_Registry)
         {
            if (s_Registry.TryGetRegistered(new GameplayTag(name), out GameplayTagMetadata metadata))
            {
               tag = metadata.Tag;
               return true;
            }
         }
      }

      tag = default;
      return false;
   }

   public static GameplayTagMetadata GetMetadata(GameplayTag tag)
   {
      if (TryGetMetadata(tag, out GameplayTagMetadata metadata))
         return metadata;

      throw new KeyNotFoundException($"Gameplay tag '{tag.Name}' is unknown.");
   }

   public static bool TryGetMetadata(GameplayTag tag, out GameplayTagMetadata metadata)
   {
      lock (s_Registry)
         return s_Registry.TryGetMetadata(tag, out metadata);
   }

   /// <summary>Copies the tags confirmed by the sources loaded so far, including their ancestors.</summary>
   public static GameplayTag[] GetRegisteredTags()
   {
      lock (s_Registry)
         return s_Registry.GetRegisteredTags();
   }

   /// <summary>Copies the recovered names that have not yet been confirmed by a source.</summary>
   public static GameplayTag[] GetTemporaryTags()
   {
      lock (s_Registry)
         return s_Registry.GetTemporaryTags();
   }

   // Unity may deserialize a container on a loading thread. This path only accesses managed data.
   internal static bool TryRegisterSerializedTag(string name, out GameplayTag tag)
   {
      lock (s_Registry)
         return s_Registry.TryRegisterSerializedTag(name, out tag);
   }

   private static void Initialize()
   {
      lock (s_Registry)
      {
         if (s_SourcesLoaded)
            return;

         s_Registry.RegisterSourceTag("Damage.Fire", "Damage dealt by fire.");
         s_Registry.RegisterSourceTag("Status.Buff.SuddenDeath", "Allows the Warrior's next Execute regardless of the target's health.");
         s_Registry.RegisterSourceTag("Status.Mark.Flux", "Marks a target for Ryze's Overload interactions.");
         s_SourcesLoaded = true;
      }
   }
}
