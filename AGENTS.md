# GameplayAbilities coding style

Follow these conventions for new and modified Unity C# code. Keep unrelated code unchanged.

## Formatting

- Indent with three spaces per level. Do not use tabs.
- Put opening and closing braces on their own lines, aligned with the statement or declaration.
- Use spaces after commas, after control flow keywords, and around binary operators. Write calls as `ApplyEffect(effect)`.
- Separate methods, block properties, and groups of fields with one blank line. Keep related fields and short properties together.
- Use braces for blocks with multiple statements. A short guard with a single `return` or `throw` may omit braces, with the statement indented on the next line.

## Names and declarations

- Use PascalCase for types, methods, properties, events, and enum members. Prefix interfaces with `I`.
- Use camelCase for parameters and locals. Prefer explicit types, including in `foreach` and `out` declarations. Use `new()` when the target type is clear.
- Use `m_PascalCase` for private instance fields, `s_PascalCase` for private static state, and `k_PascalCase` for private constants and fixed internal values.
- Use expression-bodied properties for simple reads and block getters when the getter performs work.
- Prefer private fields with `[SerializeField]` for Inspector data. Put each attribute on its own line above the member.

## Organization

- Organize files by responsibility and feature. Keep related systems together and put editor-only code under `Editor/`. Use subfolders for cohesive groups such as tasks, operations, and handlers.
- Name each file after its main type, particularly for Unity components. Small helper types can share the file when they serve the same concept.
- Put `using` directives at the top followed by a blank line. Group related members together without imposing a fixed member order.

## Implementation

- Prefer early returns for guards and completed cases to keep the main path easy to follow.
- Use `Try...` and `out` for expected misses. Choose exceptions, assertions, or logging with an early return according to the contract and context.
- Respect Unity's destroyed-object semantics when checking `UnityEngine.Object` references. Cache reused components in `Awake` or on first access as appropriate for their lifecycle.
- Use comments to explain intent or decisions. Add XML documentation when it clarifies how callers should use an API.
