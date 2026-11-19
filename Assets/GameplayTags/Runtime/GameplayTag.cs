using System;
using UnityEngine;

[Serializable]
public struct GameplayTag : IEquatable<GameplayTag>, ISerializationCallbackReceiver
{
   public readonly string Name => m_Name ?? string.Empty;
   public readonly bool IsEmpty => string.IsNullOrEmpty(m_Name);

   public readonly GameplayTag Parent =>
      GameplayTagManager.TryGetMetadata(this, out GameplayTagMetadata metadata) ? metadata.Parent : default;

   public readonly GameplayTagChildren Children =>
      GameplayTagManager.TryGetMetadata(this, out GameplayTagMetadata metadata) ? metadata.Children : default;

   [SerializeField]
   private string m_Name;

   [NonSerialized]
   private int m_NameHash;

   internal GameplayTag(string name)
   {
      m_Name = name;
      m_NameHash = ComputeNameHash(name);
   }

   public readonly bool Equals(GameplayTag other)
   {
      return string.Equals(Name, other.Name, StringComparison.Ordinal);
   }

   public override readonly bool Equals(object obj)
   {
      return obj is GameplayTag other && Equals(other);
   }

   public override readonly int GetHashCode()
   {
      return m_NameHash;
   }

   public override readonly string ToString()
   {
      return Name;
   }

   public static bool operator ==(GameplayTag left, GameplayTag right)
   {
      return left.Equals(right);
   }

   public static bool operator !=(GameplayTag left, GameplayTag right)
   {
      return !left.Equals(right);
   }

   void ISerializationCallbackReceiver.OnBeforeSerialize()
   {
   }

   void ISerializationCallbackReceiver.OnAfterDeserialize()
   {
      m_NameHash = ComputeNameHash(m_Name);
   }

   private static int ComputeNameHash(string name)
   {
      return string.IsNullOrEmpty(name) ? 0 : StringComparer.Ordinal.GetHashCode(name);
   }
}
