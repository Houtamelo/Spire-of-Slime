using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
    public class InheritedLayoutElement : MonoBehaviour, ILayoutElement
    {
        [SerializeField] private ReferenceType type = ReferenceType.None;
        [SerializeField, ShowIf(nameof(ShowTextMeshProUGUI))] private TextMeshProUGUI textMeshProUGUI;
        [SerializeField, ShowIf(nameof(ShowTextMeshPro))] private TextMeshPro textMeshPro;
        [SerializeField, ShowIf(nameof(ShowImage))] private Image image;
        [SerializeField, HideInInspector] private RectTransform rectTransform;
        [SerializeField, ShowIf(nameof(overridePriority))] private int selfPriority;
        [SerializeField] private bool overridePriority;

        private RectTransform RectTransform
        {
            get
            {
                if (ReferenceEquals(rectTransform, null))
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }
        private bool ShowTextMeshProUGUI => type == ReferenceType.TMPUGUI;
        private bool ShowTextMeshPro => type == ReferenceType.TMPMesh;
        private bool ShowImage => type == ReferenceType.Image;

        private ILayoutElement Reference => type switch
        {
            ReferenceType.Image => image,
            ReferenceType.TMPUGUI => textMeshProUGUI,
            ReferenceType.TMPMesh => textMeshPro,
            ReferenceType.None => new DummyLayout(),
            _ => throw new ArgumentOutOfRangeException()
        };

        public void CalculateLayoutInputHorizontal() => Reference.CalculateLayoutInputHorizontal();
        public void CalculateLayoutInputVertical() => Reference.CalculateLayoutInputVertical();
        public float minWidth => Reference.minWidth;
        public float preferredWidth => Reference.preferredWidth;
        public float flexibleWidth => Reference.flexibleWidth;
        public float minHeight => Reference.minHeight;
        public float preferredHeight => Reference.preferredHeight;
        public float flexibleHeight => Reference.flexibleHeight;
        public int layoutPriority => overridePriority ? selfPriority : Reference.layoutPriority;
        
        private enum ReferenceType
        {
            // ReSharper disable once InconsistentNaming
            TMPUGUI,
            TMPMesh,
            Image,
            None
        }

        private class DummyLayout : ILayoutElement
        {
            public void CalculateLayoutInputHorizontal() { }
            public void CalculateLayoutInputVertical() { }
            public float minWidth => 0;
            public float preferredWidth => 0;
            public float flexibleWidth => 0;
            public float minHeight => 0;
            public float preferredHeight => 0;
            public float flexibleHeight => 0;
            public int layoutPriority => 0;
        }

        private void OnTransformChildrenChanged()
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }

        private void OnValidate()
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
        
        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
        #endif
    }
}