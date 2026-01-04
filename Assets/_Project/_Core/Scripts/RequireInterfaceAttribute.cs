using System;

namespace MyGame.Core
{
    /// <summary>
    /// Attribute to require that a serialized field references a component implementing a specific interface.
    /// Use with RequireInterfaceDrawer to provide validation in the Unity Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RequireInterfaceAttribute : UnityEngine.PropertyAttribute
    {
        /// <summary>
        /// The type of interface required.
        /// </summary>
        public Type RequiredType { get; }
        
        /// <summary>
        /// Creates a new RequireInterfaceAttribute.
        /// </summary>
        /// <param name="requiredType">The interface type that the assigned component must implement.</param>
        public RequireInterfaceAttribute(Type requiredType)
        {
            RequiredType = requiredType;
        }
    }
}
