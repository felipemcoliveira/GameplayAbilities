using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

[CustomPropertyDrawer(typeof(GameplayTag))]
internal sealed class GameplayTagPropertyDrawer : PropertyDrawer
{
   public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
   {
      using EditorGUI.PropertyScope scope = new(position, label, property);
      Rect field = label == GUIContent.none ? position : EditorGUI.PrefixLabel(position, scope.content);
      using SerializedProperty name = property.FindPropertyRelative(GameplayTagPicker.NameProperty);
      bool multiple = property.serializedObject.isEditingMultipleObjects;
      GUIContent value = property.hasMultipleDifferentValues
         ? new GUIContent("Multiple values")
         : GameplayTagPicker.DescribeTag(name.stringValue);
      if (multiple)
         value.tooltip = "Select one object to edit its tag.";

      using (new EditorGUI.DisabledScope(multiple))
      {
         if (GUI.Button(field, value, EditorStyles.popup))
            PopupWindow.Show(field, new GameplayTagPicker(property, isContainer: false));
      }
   }

   public override VisualElement CreatePropertyGUI(SerializedProperty property)
   {
      return new GameplayTagInspectorField(property, this);
   }
}

[CustomPropertyDrawer(typeof(GameplayTagContainer))]
internal sealed class GameplayTagContainerPropertyDrawer : PropertyDrawer
{
   private const float k_AddWidth = 66;
   private const float k_RemoveWidth = 24;
   private const float k_Padding = 3;

   public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
   {
      int rows = 1;
      if (property.isExpanded && !property.serializedObject.isEditingMultipleObjects)
      {
         using SerializedProperty tags = property.FindPropertyRelative(GameplayTagPicker.ExplicitTagsProperty);
         rows += Mathf.Max(1, tags.arraySize);
      }

      return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * EditorGUIUtility.standardVerticalSpacing;
   }

   public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
   {
      using EditorGUI.PropertyScope scope = new(position, label, property);
      Rect header = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
      Rect field = label == GUIContent.none ? header : EditorGUI.PrefixLabel(header, scope.content);
      if (property.serializedObject.isEditingMultipleObjects)
      {
         GUI.Label(field, "Select one object to edit its tags.", EditorStyles.miniLabel);
         return;
      }

      using SerializedProperty tags = property.FindPropertyRelative(GameplayTagPicker.ExplicitTagsProperty);
      Rect add = new(field.xMax - k_AddWidth, field.y, k_AddWidth, field.height);
      Rect foldout = new(field.x + 11, field.y, Mathf.Max(0, field.width - k_AddWidth - 14), field.height);
      GUIContent count = new($"{tags.arraySize} explicit", "Show tags added directly to this container.");
      using EditorGUI.IndentLevelScope indent = new(-EditorGUI.indentLevel);
      bool wasExpanded = property.isExpanded;
      property.isExpanded = EditorGUI.Foldout(foldout, wasExpanded, count, toggleOnLabelClick: true);
      if (GUI.Button(add, new GUIContent("Edit…", "Choose the tags added directly to this container."), EditorStyles.miniButton))
      {
         property.isExpanded = true;
         PopupWindow.Show(add, new GameplayTagPicker(property, isContainer: true));
      }
      if (wasExpanded && property.isExpanded)
         DrawTags(field, tags);
   }

   public override VisualElement CreatePropertyGUI(SerializedProperty property)
   {
      return new GameplayTagInspectorField(property, this);
   }

   private static void DrawTags(Rect header, SerializedProperty tags)
   {
      float gap = EditorGUIUtility.standardVerticalSpacing;
      Rect row = new(header.x, header.yMax + gap, header.width, header.height);
      if (tags.arraySize == 0)
      {
         GUI.Label(row, "No tags selected", EditorStyles.centeredGreyMiniLabel);
         return;
      }

      for (int i = 0; i < tags.arraySize; i++)
      {
         string name = GameplayTagPicker.GetExplicitTagName(tags, i);
         Rect delete = new(row.xMax - k_RemoveWidth, row.y, k_RemoveWidth, row.height);
         Rect nameRect = new(row.x + k_Padding, row.y, Mathf.Max(0, row.width - k_RemoveWidth - 2 * k_Padding), row.height);
         GUI.Label(nameRect, GameplayTagPicker.DescribeTag(name));
         if (GUI.Button(delete, new GUIContent("−", $"Remove {name}"), EditorStyles.miniButton))
         {
            tags.DeleteArrayElementAtIndex(i);
            break;
         }

         row.y += row.height + gap;
      }
   }
}

// Use the same control in the default UI Toolkit Inspector and in IMGUI Inspectors.
internal sealed class GameplayTagInspectorField : BaseField<string>
{
   internal GameplayTagInspectorField(SerializedProperty property, PropertyDrawer drawer)
      : base(property.displayName, CreateInput(property, drawer))
   {
      tooltip = property.tooltip;
      AddToClassList(alignedFieldUssClassName);
      labelElement.style.alignSelf = Align.FlexStart;
   }

   private static IMGUIContainer CreateInput(SerializedProperty property, PropertyDrawer drawer)
   {
      SerializedObject serializedObject = property.serializedObject;
      string propertyPath = property.propertyPath;
      IMGUIContainer input = new(() =>
      {
         if (serializedObject.targetObject == null)
            return;

         serializedObject.UpdateIfRequiredOrScript();
         using SerializedProperty current = serializedObject.FindProperty(propertyPath);
         if (current == null)
            return;

         float height = drawer.GetPropertyHeight(current, GUIContent.none);
         Rect position = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
         drawer.OnGUI(position, current, GUIContent.none);
         serializedObject.ApplyModifiedProperties();
      });
      input.style.flexGrow = 1;
      input.style.minWidth = 0;
      return input;
   }
}
