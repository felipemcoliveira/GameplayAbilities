using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class GameplayTagRegistryTests
{
   [TestCase(null)]
   [TestCase("")]
   [TestCase(".Status")]
   [TestCase("Status.")]
   [TestCase("Status..Flux")]
   [TestCase("Status.Mark Flux")]
   [TestCase("Status.\tFlux")]
   public void InvalidNamesNeverEnterEitherRegistrationList(string name)
   {
      GameplayTagRegistry registry = new();

      Assert.That(registry.TryRegisterSerializedTag(name, out GameplayTag recovered), Is.False);
      Assert.That(recovered.IsEmpty, Is.True);
      Assert.That(() => registry.RegisterSourceTag(name, null), Throws.TypeOf<ArgumentException>());
      Assert.That(registry.GetRegisteredTags(), Is.Empty);
      Assert.That(registry.GetTemporaryTags(), Is.Empty);
   }

   [Test]
   public void SourceDeclarationRegistersTheLeafAndItsDirectAncestry()
   {
      GameplayTagRegistry registry = new();
      GameplayTag root = new("Status");
      GameplayTag parent = new("Status.Mark");
      GameplayTag leaf = new("Status.Mark.Flux");

      registry.RegisterSourceTag(leaf.Name, "Applied by Spell Flux.");

      Assert.That(registry.GetRegisteredTags(), Is.EqualTo(new[] { root, parent, leaf }));
      Assert.That(registry.GetTemporaryTags(), Is.Empty);
      Assert.That(registry.TryGetRegistered(leaf, out GameplayTagMetadata leafMetadata), Is.True);
      Assert.That(leafMetadata.IsExplicit, Is.True);
      Assert.That(leafMetadata.Parent, Is.EqualTo(parent));
      Assert.That(leafMetadata.Children.Count, Is.Zero);
      Assert.That(registry.TryGetRegistered(parent, out GameplayTagMetadata parentMetadata), Is.True);
      Assert.That(parentMetadata.IsExplicit, Is.False);
      Assert.That(parentMetadata.Parent, Is.EqualTo(root));
      Assert.That(parentMetadata.Children.Count, Is.EqualTo(1));
      Assert.That(parentMetadata.Children[0], Is.EqualTo(leaf));
      Assert.That(registry.TryGetRegistered(root, out GameplayTagMetadata rootMetadata), Is.True);
      Assert.That(rootMetadata.Parent.IsEmpty, Is.True);
      Assert.That(rootMetadata.Children[0], Is.EqualTo(parent));
   }

   [Test]
   public void ConfirmingOneRecoveredTagPromotesItsAncestorsButLeavesItsSiblingTemporary()
   {
      GameplayTagRegistry registry = new();
      Assert.That(registry.TryRegisterSerializedTag("Status.Mark.Flux", out GameplayTag flux), Is.True);
      Assert.That(registry.TryRegisterSerializedTag("Status.Mark.Retired", out GameplayTag retired), Is.True);
      GameplayTag parent = new("Status.Mark");
      GameplayTag root = new("Status");

      Assert.That(registry.TryGetRegistered(flux, out _), Is.False);
      Assert.That(registry.TryGetMetadata(flux, out GameplayTagMetadata recoveredMetadata), Is.True);
      Assert.That(recoveredMetadata.IsExplicit, Is.False);
      Assert.That(recoveredMetadata.Parent, Is.EqualTo(parent));
      Assert.That(registry.GetTemporaryTags(), Is.EqualTo(new[] { root, parent, flux, retired }));

      registry.RegisterSourceTag(flux.Name, "Declared by a source.");

      Assert.That(registry.GetRegisteredTags(), Is.EqualTo(new[] { root, parent, flux }));
      Assert.That(registry.GetTemporaryTags(), Is.EqualTo(new[] { retired }));
      Assert.That(registry.TryGetRegistered(flux, out GameplayTagMetadata confirmedMetadata), Is.True);
      Assert.That(confirmedMetadata.IsExplicit, Is.True);
      Assert.That(confirmedMetadata.Parent, Is.EqualTo(parent));
      Assert.That(registry.TryGetRegistered(parent, out GameplayTagMetadata parentMetadata), Is.True);
      Assert.That(parentMetadata.Children.Count, Is.EqualTo(2));
      Assert.That(parentMetadata.Children[0], Is.EqualTo(flux));
      Assert.That(parentMetadata.Children[1], Is.EqualTo(retired));
   }

   [Test]
   public void MultipleSourcesKeepDistinctCommentsInDeclarationOrder()
   {
      GameplayTagRegistry registry = new();
      GameplayTag flux = new("Status.Mark.Flux");

      registry.RegisterSourceTag(flux.Name, "Comment from the library.");
      registry.RegisterSourceTag(flux.Name, "Comment from C#.");
      registry.RegisterSourceTag(flux.Name, "Comment from the library.");
      registry.RegisterSourceTag(flux.Name, "");
      registry.RegisterSourceTag(flux.Name, null);

      Assert.That(registry.TryGetRegistered(flux, out GameplayTagMetadata metadata), Is.True);
      Assert.That(metadata.Comments, Is.EqualTo(new[] { "Comment from the library.", "Comment from C#." }));
      Assert.That(registry.TryGetRegistered(new GameplayTag("Status.Mark"), out GameplayTagMetadata parent), Is.True);
      Assert.That(parent.Comments, Is.Empty);
      Assert.That(parent.Children.Count, Is.EqualTo(1));
   }

   [Test]
   public void ResetPreservesOnlyNamesObservedDuringDeserialization()
   {
      GameplayTagRegistry registry = new();
      registry.RegisterSourceTag("Status.Mark.Flux", "Source comment.");
      registry.RegisterSourceTag("State.SourceOnly", null);
      Assert.That(registry.TryRegisterSerializedTag("Status.Mark.Flux", out GameplayTag flux), Is.True);
      Assert.That(registry.TryRegisterSerializedTag("Status.Mark.Retired", out GameplayTag retired), Is.True);

      registry.ResetSources();

      Assert.That(registry.GetRegisteredTags(), Is.Empty);
      Assert.That(registry.GetTemporaryTags(), Is.EqualTo(new[]
      {
         new GameplayTag("Status"),
         new GameplayTag("Status.Mark"),
         flux,
         retired
      }));
      Assert.That(registry.TryGetMetadata(new GameplayTag("State.SourceOnly"), out _), Is.False);
      Assert.That(registry.TryGetMetadata(flux, out GameplayTagMetadata metadata), Is.True);
      Assert.That(metadata.IsExplicit, Is.False);
      Assert.That(metadata.Comments, Is.Empty);
      Assert.That(metadata.Parent, Is.EqualTo(new GameplayTag("Status.Mark")));
   }

   [Test]
   public void RegistryAndChildSnapshotsRemainIndependentOfLaterRegistrations()
   {
      GameplayTagRegistry registry = new();
      GameplayTag flora = new("Status.Mark.Flora");
      GameplayTag flux = new("Status.Mark.Flux");
      GameplayTag parent = new("Status.Mark");
      registry.RegisterSourceTag(flux.Name, null);
      GameplayTag[] before = registry.GetRegisteredTags();
      Assert.That(registry.TryGetRegistered(parent, out GameplayTagMetadata metadata), Is.True);
      GameplayTagChildren oldChildren = metadata.Children;

      registry.RegisterSourceTag(flora.Name, null);

      Assert.That(before, Is.EqualTo(new[] { new GameplayTag("Status"), parent, flux }));
      Assert.That(oldChildren.Count, Is.EqualTo(1));
      Assert.That(oldChildren[0], Is.EqualTo(flux));
      Assert.That(metadata.Children.Count, Is.EqualTo(2));
      Assert.That(metadata.Children[0], Is.EqualTo(flora));
      Assert.That(metadata.Children[1], Is.EqualTo(flux));
   }

   [Test]
   public void TagEqualityUsesExactNamesAndSupportsHashedCollections()
   {
      GameplayTag flux = new("Status.Mark.Flux");
      GameplayTag sameName = new("Status.Mark.Flux");
      GameplayTag differentCase = new("Status.Mark.flux");
      GameplayTag parent = new("Status.Mark");
      HashSet<GameplayTag> tags = new() { flux };

      Assert.That(flux == sameName, Is.True);
      Assert.That(flux.GetHashCode(), Is.EqualTo(sameName.GetHashCode()));
      Assert.That(flux != differentCase, Is.True);
      Assert.That(flux != parent, Is.True);
      Assert.That(tags.Contains(sameName), Is.True);
      Assert.That(tags.Contains(differentCase), Is.False);
      Assert.That(tags.Contains(parent), Is.False);
   }

   [Test]
   public void DefaultTagHasTheSameIdentityAsAnEmptyName()
   {
      GameplayTag tag = default;
      GameplayTag emptyName = new(string.Empty);

      Assert.That(tag.IsEmpty, Is.True);
      Assert.That(tag.Name, Is.Empty);
      Assert.That(tag, Is.EqualTo(emptyName));
      Assert.That(tag.GetHashCode(), Is.EqualTo(emptyName.GetHashCode()));
   }
}
