using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class NativeGameplayTagSource
{
   private const BindingFlags k_MemberFlags = BindingFlags.Public | BindingFlags.NonPublic |
      BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

   internal static IReadOnlyList<Binding> Discover()
   {
      List<Binding> bindings = new();
      Assembly attributeAssembly = typeof(NativeGameplayTagAttribute).Assembly;
      AssemblyName attributeAssemblyName = attributeAssembly.GetName();

      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.FullName, StringComparer.Ordinal))
      {
         // An assembly cannot use the attribute unless it defines or references it.
         if (assembly != attributeAssembly && !assembly.GetReferencedAssemblies().Any(reference =>
               string.Equals(reference.FullName, attributeAssemblyName.FullName, StringComparison.Ordinal)))
            continue;

         foreach (Type type in assembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
         {
            foreach (MemberInfo member in type.GetMembers(k_MemberFlags)
                  .Where(member => member is FieldInfo or PropertyInfo)
                  .OrderBy(member => member.Name, StringComparer.Ordinal)
                  .ThenBy(member => member.MetadataToken))
            {
               NativeGameplayTagAttribute attribute = member.GetCustomAttribute<NativeGameplayTagAttribute>(inherit: false);
               if (attribute == null)
                  continue;

               bindings.Add(new Binding(member, attribute));
            }
         }
      }

      return bindings;
   }

   internal sealed class Binding
   {
      internal string Name { get; }
      internal string Comment { get; }

      private readonly MemberInfo m_Member;

      internal Binding(MemberInfo member, NativeGameplayTagAttribute attribute)
      {
         bool writable = member switch
         {
            FieldInfo field => field.IsStatic && !field.IsInitOnly && !field.IsLiteral && field.FieldType == typeof(GameplayTag),
            PropertyInfo property => property.GetSetMethod(nonPublic: true)?.IsStatic == true &&
               property.GetIndexParameters().Length == 0 && property.PropertyType == typeof(GameplayTag),
            _ => false
         };

         if (!writable || member.DeclaringType.ContainsGenericParameters)
            throw new InvalidOperationException($"{member.DeclaringType.FullName}.{member.Name} must be a writable static GameplayTag field or property on a non-generic or closed type.");

         m_Member = member;
         Name = attribute.Name;
         Comment = attribute.Comment;
      }

      internal void Assign(GameplayTag tag)
      {
         if (m_Member is FieldInfo field)
         {
            field.SetValue(null, tag);
            return;
         }

         ((PropertyInfo)m_Member).SetValue(null, tag);
      }
   }
}
