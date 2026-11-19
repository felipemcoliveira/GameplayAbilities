using System.Collections.Generic;

public sealed class GameplayTagMetadata
{
   public GameplayTag Tag { get; }
   public GameplayTag Parent { get; }
   public IReadOnlyList<GameplayTag> Children { get; }
   public bool IsExplicit { get; }
   public string Comment { get; }

   internal GameplayTagChildren ChildView => new(m_Children);

   private readonly List<GameplayTag> m_Children = new();

   internal GameplayTagMetadata(GameplayTag tag, GameplayTag parent, string declarationComment)
   {
      Tag = tag;
      Parent = parent;
      Children = m_Children.AsReadOnly();
      IsExplicit = declarationComment != null;
      Comment = declarationComment ?? string.Empty;
   }

   internal void AddChild(GameplayTag child)
   {
      m_Children.Add(child);
   }
}
