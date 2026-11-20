using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

[CustomPropertyDrawer(typeof(GameplayTag))]
internal sealed class GameplayTagPropertyDrawer : PropertyDrawer
{
   public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
   {
      position.height = EditorGUIUtility.singleLineHeight;
      using EditorGUI.PropertyScope scope = new(position, label, property);
      Rect field = label == GUIContent.none ? position : EditorGUI.PrefixLabel(position, scope.content);
      using EditorGUI.IndentLevelScope indent = new(-EditorGUI.indentLevel);
      using SerializedProperty name = property.FindPropertyRelative(GameplayTagPicker.NameProperty);
      bool multiple = property.serializedObject.isEditingMultipleObjects;
      GUIContent value = property.hasMultipleDifferentValues
         ? new GUIContent("Multiple values")
         : GameplayTagPicker.DescribeTag(name.stringValue);
      if (multiple)
         value.tooltip = "Select one object to edit its tag.";

      using (new EditorGUI.DisabledScope(multiple))
      {
         if (GUI.Button(field, new GUIContent(string.Empty, value.tooltip), EditorStyles.popup))
            PopupWindow.Show(field, new GameplayTagPicker(property, isContainer: false));

         GameplayTagEditorGUI.DrawIcon(new Rect(field.x + 4, field.center.y - 7, 14, 14), "tag");
         Rect text = new(field.x + 23, field.y, Mathf.Max(0, field.width - 42), field.height);
         GUI.Label(text, value, GameplayTagEditorGUI.ValueStyle);
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
   private const float k_HeaderHeight = 26;
   private const float k_RowHeight = 24;

   public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
   {
      using SerializedProperty tags = property.FindPropertyRelative(GameplayTagPicker.ExplicitTagsProperty);
      int count = property.serializedObject.isEditingMultipleObjects ? 0 : tags.arraySize;
      return k_HeaderHeight + k_RowHeight * Mathf.Max(1, count) + 4;
   }

   public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
   {
      using EditorGUI.PropertyScope scope = new(position, label, property);
      Rect box = position;
      if (label != GUIContent.none)
      {
         Rect labelRect = new(position.x, position.y + 3, position.width, EditorGUIUtility.singleLineHeight);
         box.xMin = EditorGUI.PrefixLabel(labelRect, scope.content).xMin;
      }

      using EditorGUI.IndentLevelScope indent = new(-EditorGUI.indentLevel);
      using SerializedProperty tags = property.FindPropertyRelative(GameplayTagPicker.ExplicitTagsProperty);
      bool multiple = property.serializedObject.isEditingMultipleObjects;
      GameplayTagEditorGUI.Panel(box);
      Rect header = new(box.x + 7, box.y + 3, Mathf.Max(0, box.width - 14), 20);
      Rect labelArea = new(header.x, header.y, Mathf.Max(0, header.width - 54), header.height);
      int count = multiple ? 0 : tags.arraySize;
      string summary = multiple ? "Multiple objects" : count == 1 ? "1 tag" : $"{count} tags";
      GUI.Label(labelArea, summary, EditorStyles.miniLabel);

      using (new EditorGUI.DisabledScope(multiple))
      {
         Rect add = new(header.xMax - 24, header.y, 24, 20);
         if (GameplayTagEditorGUI.IconButton(add, "add", "Add or edit selected tags"))
            PopupWindow.Show(add, new GameplayTagPicker(property, isContainer: true));

         using (new EditorGUI.DisabledScope(count == 0))
         {
            Rect clear = new(add.x - 27, add.y, 24, 20);
            if (GameplayTagEditorGUI.IconButton(clear, "clear", "Clear all selected tags"))
               tags.ClearArray();
         }
      }

      GameplayTagEditorGUI.Separator(new Rect(box.x + 1, box.y + k_HeaderHeight, box.width - 2, 1));
      Rect row = new(box.x + 7, box.y + k_HeaderHeight + 2, header.width, k_RowHeight);
      if (multiple)
         GUI.Label(row, "Select one object to edit.", EditorStyles.centeredGreyMiniLabel);
      else
         DrawTags(row, tags);
   }

   public override VisualElement CreatePropertyGUI(SerializedProperty property)
   {
      return new GameplayTagInspectorField(property, this);
   }

   private static void DrawTags(Rect row, SerializedProperty tags)
   {
      if (tags.arraySize == 0)
      {
         GUI.Label(row, "No tags selected", EditorStyles.centeredGreyMiniLabel);
         return;
      }

      for (int i = 0; i < tags.arraySize; i++)
      {
         string name = GameplayTagPicker.GetExplicitTagName(tags, i);
         GameplayTagEditorGUI.DrawIcon(new Rect(row.x, row.center.y - 7, 14, 14), "tag");
         Rect nameRect = new(row.x + 21, row.y, Mathf.Max(0, row.width - 48), row.height);
         GUI.Label(nameRect, GameplayTagPicker.DescribeTag(name), GameplayTagEditorGUI.ValueStyle);
         Rect remove = new(row.xMax - 22, row.y + 2, 22, 20);
         if (GameplayTagEditorGUI.IconButton(remove, "remove", $"Remove {name}"))
         {
            tags.DeleteArrayElementAtIndex(i);
            break;
         }

         row.y += k_RowHeight;
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
      style.flexGrow = 1;
      labelElement.style.alignSelf = Align.FlexStart;
      labelElement.style.paddingTop = 2;
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
      input.TrackSerializedObjectValue(serializedObject, _ => input.MarkDirtyLayout());
      return input;
   }
}
