using System;
using System.Collections.Generic;

public static class GameplayTagManager
{
   private static readonly Dictionary<GameplayTag, GameplayTagMetadata> s_Registry = BuildRegistry();

   public static GameplayTag GetTag(string name)
   {
      if (TryGetTag(name, out GameplayTag tag))
         return tag;

      throw new KeyNotFoundException($"Gameplay tag '{name}' is not registered.");
   }

   public static bool TryGetTag(string name, out GameplayTag tag)
   {
      if (!string.IsNullOrEmpty(name) && s_Registry.TryGetValue(new GameplayTag(name), out GameplayTagMetadata metadata))
      {
         tag = metadata.Tag;
         return true;
      }

      tag = default;
      return false;
   }

   public static GameplayTagMetadata GetMetadata(GameplayTag tag)
   {
      return s_Registry[tag];
   }

   private static Dictionary<GameplayTag, GameplayTagMetadata> BuildRegistry()
   {
      SortedDictionary<string, string> declarations = new(StringComparer.Ordinal);
      AddDeclaration(declarations, "Damage.Fire", "Damage dealt by fire.");
      AddDeclaration(declarations, "Status.Buff.SuddenDeath", "Allows the Warrior's next Execute regardless of the target's health.");
      AddDeclaration(declarations, "Status.Mark.Flux", "Marks a target for Ryze's Overload interactions.");

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
