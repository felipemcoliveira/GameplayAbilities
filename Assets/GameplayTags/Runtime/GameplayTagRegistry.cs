using System;
using System.Collections.Generic;

// The manager holds the registry lock when it reads or changes these maps.
internal sealed class GameplayTagRegistry
{
   private readonly Dictionary<GameplayTag, GameplayTagMetadata> m_Registered = new();
   private readonly Dictionary<GameplayTag, GameplayTagMetadata> m_Temporary = new();
   private readonly HashSet<GameplayTag> m_SerializedNames = new();

   internal bool TryGetRegistered(GameplayTag tag, out GameplayTagMetadata metadata)
   {
      return m_Registered.TryGetValue(tag, out metadata);
   }

   internal bool TryGetMetadata(GameplayTag tag, out GameplayTagMetadata metadata)
   {
      return m_Registered.TryGetValue(tag, out metadata) || m_Temporary.TryGetValue(tag, out metadata);
   }

   internal bool TryRegisterSerializedTag(string name, out GameplayTag tag)
   {
      if (!IsValidName(name))
      {
         tag = default;
         return false;
      }

      tag = new GameplayTag(name);
      m_SerializedNames.Add(tag);
      RegisterHierarchy(tag, isSource: false);
      return true;
   }

   internal void RegisterSourceTag(string name, string comment)
   {
      if (!IsValidName(name))
         throw new ArgumentException($"Gameplay tag '{name}' must contain nonempty segments separated by dots, without whitespace.", nameof(name));

      GameplayTag tag = new(name);
      RegisterHierarchy(tag, isSource: true);
      m_Registered[tag].Declare(comment);
   }

   internal GameplayTag[] GetRegisteredTags()
   {
      return CopyNames(m_Registered);
   }

   internal GameplayTag[] GetTemporaryTags()
   {
      return CopyNames(m_Temporary);
   }

   internal void ResetSources()
   {
      m_Registered.Clear();
      m_Temporary.Clear();
      foreach (GameplayTag tag in m_SerializedNames)
         RegisterHierarchy(tag, isSource: false);
   }

   private void RegisterHierarchy(GameplayTag tag, bool isSource)
   {
      GameplayTag childToLink = default;
      for (GameplayTag current = tag; !current.IsEmpty;)
      {
         bool exists = TryGetMetadata(current, out GameplayTagMetadata metadata);
         bool promote = isSource && exists && m_Temporary.Remove(current);
         if (!exists)
         {
            int lastDot = current.Name.LastIndexOf('.');
            GameplayTag parent = lastDot < 0 ? default : new GameplayTag(current.Name[..lastDot]);
            metadata = new GameplayTagMetadata(current, parent);
            Dictionary<GameplayTag, GameplayTagMetadata> destination = isSource ? m_Registered : m_Temporary;
            destination.Add(current, metadata);
         }
         else if (promote)
         {
            m_Registered.Add(current, metadata);
         }

         if (!childToLink.IsEmpty)
            metadata.AddChild(childToLink);

         if (exists && !promote)
            break;

         childToLink = exists ? default : current;
         current = metadata.Parent;
      }
   }

   private static GameplayTag[] CopyNames(Dictionary<GameplayTag, GameplayTagMetadata> dictionary)
   {
      GameplayTag[] tags = new GameplayTag[dictionary.Count];
      dictionary.Keys.CopyTo(tags, 0);
      Array.Sort(tags, CompareNames);
      return tags;
   }

   private static int CompareNames(GameplayTag left, GameplayTag right)
   {
      return string.CompareOrdinal(left.Name, right.Name);
   }

   internal static bool IsValidName(string name)
   {
      if (string.IsNullOrEmpty(name) || name[0] == '.' || name[^1] == '.' || name.Contains(".."))
         return false;

      foreach (char character in name)
      {
         if (char.IsWhiteSpace(character))
            return false;
      }

      return true;
   }
}
