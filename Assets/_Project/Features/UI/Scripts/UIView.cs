using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace MyGame.Features.UI
{
    /// <summary>
    /// A component responsible for the visibility state of a UI element or a group of elements.
    /// If no UIDocument is found on this object, it controls all UIDocuments in children.
    /// </summary>
    public class UIView : MonoBehaviour
    {
        [SerializeField] private string viewName = "Unnamed View";
        [SerializeField] private bool startHidden = false;

        // Support controlling multiple documents (for grouping objects)
        private List<UIDocument> _documents = new List<UIDocument>();

        public string ViewName => viewName;
        public bool IsVisible { get; private set; } = true;

        private void OnEnable()
        {
            // Refresh documents list on enable to handle dynamic children or initialization order
            _documents.Clear();
            
            var localDoc = GetComponent<UIDocument>();
            if (localDoc != null)
            {
                _documents.Add(localDoc);
            }
            else
            {
                _documents.AddRange(GetComponentsInChildren<UIDocument>(true));
            }
        }

        private void Start()
        {
            if (startHidden)
            {
                Hide(true);
            }
        }

        /// <summary>
        /// Shows the view(s).
        /// </summary>
        public void Show()
        {
            ToggleVisibility(true);
            IsVisible = true;
        }

        /// <summary>
        /// Hides the view(s) without destroying the Visual Tree.
        /// </summary>
        public void Hide(bool immediate = false)
        {
            ToggleVisibility(false);
            IsVisible = false;
        }

        private void ToggleVisibility(bool visible)
        {
            foreach (var doc in _documents)
            {
                if (doc == null || doc.rootVisualElement == null) continue;
                doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
