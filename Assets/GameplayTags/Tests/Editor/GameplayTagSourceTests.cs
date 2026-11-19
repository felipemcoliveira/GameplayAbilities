using System.Collections.Generic;
using NUnit.Framework;

public sealed class GameplayTagSourceTests
{
   [NativeGameplayTag("Tests.Native.Source.Field")]
   private static GameplayTag s_Field;

   [NativeGameplayTag("Tests.Native.Source.Property")]
   private static GameplayTag NativeProperty { get; set; }

   [Test]
   public void LibraryRegistersFluxAndItsImplicitAncestors()
   {
      GameplayTag flux = GameplayTagManager.GetTag("Status.Mark.Flux");
      GameplayTag mark = GameplayTagManager.GetTag("Status.Mark");
      GameplayTag status = GameplayTagManager.GetTag("Status");

      Assert.That(flux.Parent, Is.EqualTo(mark));
      Assert.That(mark.Parent, Is.EqualTo(status));
      Assert.That(ContainsChild(mark.Children, flux), Is.True);
      Assert.That(GameplayTagManager.GetRegisteredTags(), Does.Contain(mark));
      Assert.That(GameplayTagManager.GetMetadata(flux).Comments,
         Does.Contain("Marks a target for Ryze's Overload interactions."));
   }

   [Test]
   public void NativeSourceFillsStaticFieldAndProperty()
   {
      GameplayTag field = GameplayTagManager.GetTag("Tests.Native.Source.Field");
      GameplayTag property = GameplayTagManager.GetTag("Tests.Native.Source.Property");

      Assert.That(s_Field, Is.EqualTo(field));
      Assert.That(NativeProperty, Is.EqualTo(property));
      Assert.That(field.Parent, Is.EqualTo(property.Parent));
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
