using System;
using UnityEngine.Scripting;

/// <summary>Declares a tag that the manager registers and assigns to a writable static member.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class NativeGameplayTagAttribute : PreserveAttribute
{
   public string Name { get; }
   public string Comment { get; }

   public NativeGameplayTagAttribute(string name, string comment = null)
   {
      Name = name;
      Comment = comment;
   }
}
