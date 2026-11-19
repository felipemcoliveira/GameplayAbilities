using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

public sealed class GameplayTagSourceTests
{
   [Test]
   public void NativeSourceRegistersFluxAndItsImplicitAncestors()
   {
      GameplayTag flux = GameplayTagTestTags.Flux;
      GameplayTag mark = GameplayTagTestTags.Mark;
      GameplayTag status = GameplayTagTestTags.Status;

      Assert.That(flux.Name, Is.EqualTo(GameplayTagTestTags.k_FluxName));
      Assert.That(mark.Name, Is.EqualTo(GameplayTagTestTags.k_MarkName));
      Assert.That(status.Name, Is.EqualTo(GameplayTagTestTags.k_StatusName));
      Assert.That(flux.Parent, Is.EqualTo(mark));
      Assert.That(mark.Parent, Is.EqualTo(status));
      Assert.That(ContainsChild(mark.Children, flux), Is.True);
      Assert.That(GameplayTagManager.GetRegisteredTags(), Does.Contain(mark));
      Assert.That(GameplayTagManager.GetMetadata(mark).IsExplicit, Is.False);
      Assert.That(GameplayTagManager.GetMetadata(flux).Comments, Does.Contain(GameplayTagTestTags.k_FluxComment));
   }

   [Test]
   public void EditorBootstrapFillsNativeFieldAndPropertyWithoutPlayMode()
   {
      Assert.That(EditorApplication.isPlaying, Is.False);
      Assert.That(GameplayTagTestTags.Fire.Name, Is.EqualTo(GameplayTagTestTags.k_FireName));
      Assert.That(GameplayTagTestTags.Flux.Name, Is.EqualTo(GameplayTagTestTags.k_FluxName));
   }

   [Test]
   public void UnknownNameCanBeProbedOrReportedAsAnError()
   {
      const string k_Unknown = "Tests.NonexistentSource.Name";

      Assert.That(GameplayTagManager.TryGetTag(k_Unknown, out GameplayTag missing), Is.False);
      Assert.That(missing.IsEmpty, Is.True);
      Assert.Throws<KeyNotFoundException>(() => GameplayTagManager.GetTag(k_Unknown));
   }

   private static bool ContainsChild(GameplayTagChildren children, GameplayTag expected)
   {
      foreach (GameplayTag child in children)
      {
         if (child == expected)
            return true;
      }

      return false;
   }
}
