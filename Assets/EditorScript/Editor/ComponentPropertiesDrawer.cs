using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer (typeof (ComponentProperties))]
public class ComponentPropertiesDrawer : PropertyDrawer 
{

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		//Get selected property
		int selectedValue = property.FindPropertyRelative("current").intValue;

		// Draw label
		label.text = "Property";
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		//Get array of properties
		SerializedProperty sProp = property.FindPropertyRelative("properties");
		string[] properties = new string[sProp.arraySize];
		int[] values = new int[properties.Length];
		for (int i = 0; i < sProp.arraySize; i++)
		{
			properties[i] = sProp.GetArrayElementAtIndex(i).stringValue;
			values[i] = i;
		}

		//Draw enum
		selectedValue = EditorGUI.IntPopup(position, selectedValue, properties, values);

		//Save selected property
		property.FindPropertyRelative("current").intValue = selectedValue;


		EditorGUI.EndProperty();
	}
}