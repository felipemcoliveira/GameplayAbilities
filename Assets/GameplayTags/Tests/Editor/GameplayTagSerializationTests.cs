using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public sealed class GameplayTagSerializationTests
{
   [Test]
   public void StoredTagRestoresCachedHash()
   {
      GameplayTag original = GameplayTagTestTags.Flux;
      string json = JsonUtility.ToJson(original);
      GameplayTag restored = JsonUtility.FromJson<GameplayTag>(json);
      Dictionary<GameplayTag, int> lookup = new() { [original] = 1 };

      Assert.That(json, Does.Contain("m_Name").And.Not.Contain("m_NameHash"));
      Assert.That(restored, Is.EqualTo(original));
      Assert.That(restored.GetHashCode(), Is.EqualTo(original.GetHashCode()));
      Assert.That(lookup.ContainsKey(restored), Is.True);
   }

   [Test]
   public void ContainerFieldRoundTripStoresExplicitTagsOnly()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTag mark = flux.Parent;
      StoredTarget source = new();
      source.Tags.Add(flux);

      string json = JsonUtility.ToJson(source);
      StoredTarget restored = JsonUtility.FromJson<StoredTarget>(json);

      Assert.That(json, Does.Contain("\"" + GameplayTagTestTags.k_FluxName + "\"")
         .And.Not.Contain("\"" + GameplayTagTestTags.k_MarkName + "\""));
      Assert.That(json, Does.Not.Contain("m_ExplicitIndices").And.Not.Contain("m_InheritedCounts"));
      Assert.That(restored.Tags.Count, Is.EqualTo(1));
      Assert.That(restored.Tags.ContainsExact(flux), Is.True);
      Assert.That(restored.Tags.Contains(mark), Is.True);
      Assert.That(restored.Tags.ContainsExact(mark), Is.False);
   }

   [Test]
   public void DeserializationRebuildsLookupsAndKeepsUnconfirmedNames()
   {
      const string k_LegacyName = "Tests.Serialization.Removed.Mark";
      const string k_Json = "{\"m_ExplicitTags\":[{\"m_Name\":\"" + k_LegacyName + "\"}," +
         "{\"m_Name\":\"" + k_LegacyName + "\"},{\"m_Name\":\".Invalid\"},{\"m_Name\":\"\"}]}";

      GameplayTagContainer restored = Task.Run(() => JsonUtility.FromJson<GameplayTagContainer>(k_Json)).GetAwaiter().GetResult();
      Assert.That(restored.Count, Is.EqualTo(1));

      GameplayTag legacy = restored.GetExplicitTag(0);
      Assert.That(legacy.Name, Is.EqualTo(k_LegacyName));
      Assert.That(legacy.Parent.Name, Is.EqualTo("Tests.Serialization.Removed"));
      Assert.That(restored.Contains(legacy.Parent), Is.True);
      Assert.That(GameplayTagManager.TryGetTag(k_LegacyName, out _), Is.False);
      Assert.That(GameplayTagManager.GetTemporaryTags(), Does.Contain(legacy));
      Assert.That(JsonUtility.ToJson(restored), Does.Contain(k_LegacyName).And.Not.Contain(".Invalid"));
      Assert.That(restored.Remove(legacy), Is.True);
      Assert.That(restored.Contains(legacy.Parent), Is.False);
   }

   [Serializable]
   private sealed class StoredTarget
   {
      public GameplayTagContainer Tags => m_Tags;

      [SerializeField]
      private GameplayTagContainer m_Tags = new();
   }
}
