using System.Collections;
using Core.Utils.Patterns;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Utils.Extensions
{
    public static class UnityExtensions
    {
        public static IEnumerator AsEnumerator(this YieldInstruction yieldInstruction)
        {
            yield return yieldInstruction;
        }
        
        public static Color WithAlpha(this Color color, float alpha) => new(color.r, color.g, color.b, alpha);
        
        private static readonly TweenCallback<int> EmptyCallback = _ => { };

        [NotNull]
        public static T InstantiateWithFixedLocalScale<T>([NotNull] this T prefab, Transform parent, bool worldPositionStays = false) where T : Component
        {
            Vector3 localScale = prefab.transform.localScale;
            T instance = Object.Instantiate(prefab, parent, worldPositionStays);
            instance.transform.localScale = localScale;
            return instance;
        }
        
        [NotNull]
        public static GameObject InstantiateWithFixedLocalScale([NotNull] this GameObject prefab, Transform parent, bool worldPositionStays = false)
        {
            Vector3 localScale = prefab.transform.localScale;
            GameObject instance = Object.Instantiate(prefab, parent, worldPositionStays);
            instance.transform.localScale = localScale;
            return instance;
        }
        
        [NotNull]
        public static T InstantiateWithFixedLocalScaleAndPosition<T>([NotNull] this T prefab, Transform parent) where T : Component
        {
            Transform prefabTransform = prefab.transform;
            Vector3 localScale = prefabTransform.localScale;
            Vector3 localPosition = prefabTransform.localPosition;
            
            T instance = Object.Instantiate(prefab, parent, false);
            Transform instanceTransform = instance.transform;
            instanceTransform.localScale = localScale;
            instanceTransform.localPosition = localPosition;
            return instance;
        }
        
        [NotNull]
        public static GameObject InstantiateWithFixedLocalScaleAndPosition([NotNull] this GameObject prefab, Transform parent)
        {
            Transform prefabTransform = prefab.transform;
            Vector3 localScale = prefabTransform.localScale;
            Vector3 localPosition = prefabTransform.localPosition;
            
            GameObject instance = Object.Instantiate(prefab, parent, worldPositionStays: false);
            Transform instanceTransform = instance.transform;
            instanceTransform.localScale = localScale;
            instanceTransform.localPosition = localPosition;
            return instance;
        }

        [NotNull]
        public static GameObject InstantiateWithFixedLocalScaleAndAnchoredPosition([NotNull] this GameObject prefab, Transform parent)
        {
            Transform prefabTransform = prefab.transform;
            if (prefabTransform is not RectTransform rectTransform)
            {
                Debug.Log("Soft error: prefab is not a RectTransform, using local position instead of anchored.");
                return InstantiateWithFixedLocalScaleAndPosition(prefab, parent);
            }

            Vector3 localScale = rectTransform.localScale;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            
            GameObject instance = Object.Instantiate(prefab, parent, worldPositionStays: false);
            RectTransform instanceRectTransform = (RectTransform) instance.transform;
            instanceRectTransform.localScale = localScale;
            instanceRectTransform.anchoredPosition = anchoredPosition;
            return instance;
        }
        
        [NotNull]
        public static T InstantiateWithFixedLocalScaleAndAnchoredPosition<T>([NotNull] this T prefab, Transform parent) where T : Component
        {
            Transform prefabTransform = prefab.transform;
            if (prefabTransform is not RectTransform rectTransform)
            {
                Debug.Log("Soft error: prefab is not a RectTransform, using local position instead of anchored.");
                return InstantiateWithFixedLocalScaleAndPosition(prefab, parent);
            }

            Vector3 localScale = rectTransform.localScale;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            
            T instance = Object.Instantiate(prefab, parent, false);
            RectTransform instanceRectTransform = (RectTransform) instance.transform;
            instanceRectTransform.localScale = localScale;
            instanceRectTransform.anchoredPosition = anchoredPosition;
            return instance;
        }

        public static void ClearBoolsAndTriggers([NotNull] this Animator animator)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(name: parameter.name, value: false);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        animator.ResetTrigger(name: parameter.name);
                        break;
                }
            }
        }

        public static void ClearTriggers([NotNull] this Animator animator)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                    animator.ResetTrigger(name: parameter.name);
            }
        }
        
        [NotNull]
        public static string GetPath([NotNull] this Transform current)
        {
            if (current.parent == null)
                return $"/{current.name}";
            
            return $"{current.parent.GetPath()}/{current.name}";
        }

        [NotNull]
        public static string GetPathUntil([NotNull] this Transform current, Transform maxParent)
        {
            if (current.parent == null)
                return $"/{current.name}";
            
            if (current.parent == maxParent)
                return $"{maxParent.name}/{current.name}";
            
            return $"{current.parent.GetPath()}/{current.name}";
        }

        public static Option<Transform> FindChildByPath(this Transform self, [CanBeNull] string path)
        {
            if (string.IsNullOrEmpty(path))
                return Option<Transform>.None;

            string[] pathSplit = path.Split(separator: '/');
            if (pathSplit.Length == 0)
                return Option<Transform>.None;

            if (pathSplit[0] != self.name)
                return Option<Transform>.None;

            Transform current = self;
            for (int i = 1; i < pathSplit.Length; i++)
            {
                bool found = false;
                foreach (Transform ch in current)
                {
                    if (ch.name == pathSplit[i])
                    {
                        current = ch;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return Option<Transform>.None;
            }

            return current;
        }
        
        public static void SetColorIgnoringAlpha([NotNull] this Graphic graphic, Color color)
        {
            Color currentColor = graphic.color;
            graphic.color = new Color(color.r, color.g, color.b, currentColor.a);
        }

        public static void KillIfActive([CanBeNull] this Tween tween)
        {
            if (tween is { active: true })
                tween.Kill();
        }
        
        public static void CompleteIfActive([CanBeNull] this Tween tween)
        {
            if (tween is { active: true })
                tween.Complete();
        }
        
        public static void SetAlpha([NotNull] this SpriteRenderer spriteRenderer, float alpha)
        {
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        public static void SetAlpha([NotNull] this Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            graphic.color = new Color(color.r, color.g, color.b, alpha);
        }

        public static Tween EmptyTween() => DOVirtual.Int(0, 1, 0.001f, EmptyCallback);
    }
}