# GameplayAbilities

This repository is the Unity implementation introduced in [Gameplay Tags, Part 1: Core Concepts](https://felipemcoliveira.com/blog/gameplay-tags-data-driven-abilities/).

Part 1 looks at how Unreal's Gameplay Ability System uses tags and effects to express gameplay rules as data. Part 2 builds the gameplay tag runtime in Unity, including the registry, serialized tag containers, hierarchy queries, and counted tag events. Part 3 builds the Unity editor tools for those structures.

Open the repository root in Unity **6000.4.10f1**. The project uses **URP 17.4.0** and starts with `Assets/Scenes/Main.unity`, a scene with a camera and directional light.

## Edit gameplay tags

Open **Window → Gameplay Tags → Catalog** to search the hierarchy, read developer comments, and inspect unresolved names recovered from loaded data. The **Libraries** tab lets you create and edit libraries under a `Resources/GameplayTags` folder.

Use a serialized `GameplayTag` or `GameplayTagContainer` field on a component or asset to pick registered tags in the Inspector. A container stores the explicit selections and derives their inherited parent tags.

## Run the gameplay tag tests

Open **Window → General → Test Runner** in Unity, select **EditMode**, and click **Run All**. To run only this suite, select the `GameplayTags.Tests` assembly in the tree and click **Run Selected**.

The runtime tests use native tags declared in `GameplayTags.Tests`. Unity initializes the tag sources after the Editor's import pass, so the tests can use those declarations in EditMode. The editor tests cover library asset discovery, unresolved names, and editing a container through Unity's serialized properties and Undo.
