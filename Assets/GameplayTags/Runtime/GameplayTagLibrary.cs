using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay Tags/Tag Library", fileName = "GameplayTagLibrary")]
public sealed class GameplayTagLibrary : ScriptableObject
{
   internal IReadOnlyList<Entry> Entries => m_Entries;

   [SerializeField]
   private List<Entry> m_Entries = new();

   [Serializable]
   internal struct Entry
   {
      internal readonly string Name => m_Name;
      internal readonly string Comment => m_Comment;

      [SerializeField]
      private string m_Name;

      [SerializeField]
      [TextArea]
      private string m_Comment;
   }
}
