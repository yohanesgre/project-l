using UnityEditor;
using UnityEngine;
using MyGame.Core;

namespace MyGame.Core.Editor
{
    /// <summary>
    /// Custom property drawer for Components that implement IPathProvider.
    /// Provides validation and helpful error messages when assigning path providers.
    /// </summary>
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "RequireInterface attribute can only be used on object references.");
                return;
            }

            var requiredAttribute = attribute as RequireInterfaceAttribute;
            
            // Draw the object field
            EditorGUI.BeginProperty(position, label, property);
            
            var currentObject = property.objectReferenceValue;
            var newObject = EditorGUI.ObjectField(position, label, currentObject, typeof(Object), true);
            
            if (newObject != currentObject)
            {
                // Validate the new object
                if (newObject == null)
                {
                    property.objectReferenceValue = null;
                }
                else if (ValidateObject(newObject, requiredAttribute.RequiredType, out var validComponent))
                {
                    property.objectReferenceValue = validComponent;
                }
                else
                {
                    // Show error dialog
                    string typeName = requiredAttribute.RequiredType.Name;
                    EditorUtility.DisplayDialog(
                        "Invalid Assignment",
                        $"The assigned object does not implement {typeName}.\n\n" +
                        $"Valid types:\n- CustomPath\n- RoadGenerator\n- Any component implementing {typeName}",
                        "OK"
                    );
                }
            }
            
            EditorGUI.EndProperty();
        }
        
        private bool ValidateObject(Object obj, System.Type requiredType, out Object validComponent)
        {
            validComponent = null;
            
            // If it's already the correct type
            if (requiredType.IsInstanceOfType(obj))
            {
                validComponent = obj;
                return true;
            }
            
            // If it's a GameObject, try to find the component
            if (obj is GameObject go)
            {
                var component = go.GetComponent(requiredType);
                if (component != null)
                {
                    validComponent = component;
                    return true;
                }
            }
            
            // If it's a Component, check its GameObject
            if (obj is Component comp)
            {
                var component = comp.GetComponent(requiredType);
                if (component != null)
                {
                    validComponent = component;
                    return true;
                }
            }
            
            return false;
        }
    }
}
