# GameplayAbilities

This repository is the Unity implementation introduced in [Gameplay Tags, Part 1: Core Concepts](https://felipemcoliveira.com/blog/gameplay-tags-data-driven-abilities/).

Part 1 looks at how Unreal's Gameplay Ability System uses tags and effects to express gameplay rules as data. Part 2 builds the gameplay tag runtime in Unity, including the registry, serialized tag containers, hierarchy queries, and counted tag events. Part 3 will build editor tools for those structures.

Open the repository root in Unity **6000.4.10f1**. The project uses **URP 17.4.0** and starts with `Assets/Scenes/Main.unity`, a scene with a camera and directional light.

## Run the gameplay tag tests

Open **Window → General → Test Runner** in Unity, select **EditMode**, and click **Run All**. To run only this suite, select the `GameplayTags.Tests` assembly in the tree and click **Run Selected**.

The tests use native tags declared in `GameplayTags.Tests` for all registered test data. Unity initializes the tag sources after the Editor's import pass, so the tests can use those declarations in EditMode. The suite covers tag identity, registration, serialized containers, exact and hierarchical queries, and counted tags with disposable listeners.
