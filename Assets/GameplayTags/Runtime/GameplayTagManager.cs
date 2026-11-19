using System.Collections.Generic;
using UnityEngine;

public static class GameplayTagManager
{
   private const string k_LibraryPath = "GameplayTags";
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

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
   private static void ResetSources()
   {
      lock (s_Registry)
      {
         s_Registry.ResetSources();
         s_SourcesLoaded = false;
      }
   }

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   private static void Initialize()
   {
      if (s_SourcesLoaded)
         return;

      // Loading an asset can invoke serialization callbacks on another thread.
      GameplayTagLibrary[] libraries = Resources.LoadAll<GameplayTagLibrary>(k_LibraryPath);
      lock (s_Registry)
      {
         try
         {
            RegisterLibraries(libraries);
            s_SourcesLoaded = true;
         }
         catch
         {
            s_Registry.ResetSources();
            throw;
         }
      }
   }

   private static void RegisterLibraries(GameplayTagLibrary[] libraries)
   {
      foreach (GameplayTagLibrary library in libraries)
      {
         foreach (GameplayTagLibrary.Entry entry in library.Entries)
            s_Registry.RegisterSourceTag(entry.Name, entry.Comment);
      }
   }
}
