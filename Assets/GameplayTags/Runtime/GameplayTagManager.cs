using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameplayTagManager
{
   private static Dictionary<GameplayTag, GameplayTagMetadata> Registry
   {
      get
      {
         Initialize();
         return s_Registry;
      }
   }

   private const string k_LibraryPath = "GameplayTags";
   private static Dictionary<GameplayTag, GameplayTagMetadata> s_Registry;

   public static GameplayTag GetTag(string name)
   {
      if (TryGetTag(name, out GameplayTag tag))
         return tag;

      throw new KeyNotFoundException($"Gameplay tag '{name}' is not registered.");
   }

   public static bool TryGetTag(string name, out GameplayTag tag)
   {
      if (!string.IsNullOrEmpty(name) && Registry.TryGetValue(new GameplayTag(name), out GameplayTagMetadata metadata))
      {
         tag = metadata.Tag;
         return true;
      }

      tag = default;
      return false;
   }

   public static GameplayTagMetadata GetMetadata(GameplayTag tag)
   {
      return Registry[tag];
   }

   public static bool TryGetMetadata(GameplayTag tag, out GameplayTagMetadata metadata)
   {
      if (!tag.IsEmpty)
         return Registry.TryGetValue(tag, out metadata);

      metadata = null;
      return false;
   }

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
   private static void ResetRegistry()
   {
      s_Registry = null;
   }

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   private static void Initialize()
   {
      if (s_Registry != null)
         return;

      SortedDictionary<string, string> declarations = new(StringComparer.Ordinal);
      IReadOnlyList<NativeGameplayTagSource.Binding> bindings = NativeGameplayTagSource.Discover();
      foreach (NativeGameplayTagSource.Binding binding in bindings)
         AddDeclaration(declarations, binding.Name, binding.Comment);

      foreach (GameplayTagLibrary library in Resources.LoadAll<GameplayTagLibrary>(k_LibraryPath))
      {
         foreach (GameplayTagLibrary.Entry entry in library.Entries)
            AddDeclaration(declarations, entry.Name, entry.Comment);
      }

      // Make the complete hierarchy available if an annotated setter queries the manager.
      s_Registry = BuildHierarchy(declarations);
      try
      {
         foreach (NativeGameplayTagSource.Binding binding in bindings)
            binding.Assign(GetTag(binding.Name));
      }
      catch
      {
         s_Registry = null;
         throw;
      }
   }

   private static Dictionary<GameplayTag, GameplayTagMetadata> BuildHierarchy(SortedDictionary<string, string> declarations)
   {
      Dictionary<GameplayTag, GameplayTagMetadata> registry = new(declarations.Count);
      foreach (KeyValuePair<string, string> declaration in declarations)
      {
         GameplayTag tag = new(declaration.Key);
         int lastDot = tag.Name.LastIndexOf('.');
         GameplayTag parent = lastDot < 0 ? default : new GameplayTag(tag.Name[..lastDot]);
         GameplayTagMetadata metadata = new(tag, parent, declaration.Value);
         registry.Add(tag, metadata);

         // Ordinal order puts every parent before its children.
         if (!parent.IsEmpty)
            registry[parent].AddChild(tag);
      }

      return registry;
   }

   private static void AddDeclaration(SortedDictionary<string, string> declarations, string name, string comment)
   {
      if (string.IsNullOrEmpty(name) || name[0] == '.' || name[^1] == '.' || name.Contains(".."))
         throw new ArgumentException($"Gameplay tag '{name}' must contain nonempty segments separated by dots.", nameof(name));

      foreach (char character in name)
      {
         if (char.IsWhiteSpace(character))
            throw new ArgumentException($"Gameplay tag '{name}' must not contain whitespace.", nameof(name));
      }

      comment ??= string.Empty;
      if (!declarations.TryGetValue(name, out string existingComment) || string.IsNullOrEmpty(existingComment))
      {
         declarations[name] = comment;
      }
      else if (comment.Length > 0 && !string.Equals(comment, existingComment, StringComparison.Ordinal))
      {
         throw new InvalidOperationException($"Gameplay tag '{name}' has conflicting developer comments.");
      }

      int dot = name.LastIndexOf('.');
      while (dot >= 0)
      {
         name = name[..dot];
         if (declarations.ContainsKey(name))
            break;

         declarations.Add(name, null);
         dot = name.LastIndexOf('.');
      }
   }
}
