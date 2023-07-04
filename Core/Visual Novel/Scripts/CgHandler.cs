using System.Collections;
using System.Collections.Generic;
using Core.Visual_Novel.Scripts.Animations;
using DG.Tweening;
using Main_Database.Visual_Novel;
using ResourceManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Handlers;
using Utils.Patterns;

// ReSharper disable StringLiteralTypo

namespace Core.Visual_Novel.Scripts
{
    public class CgHandler : Singleton<CgHandler>
    {
        private static readonly Color ClearWhite = new(1, 1, 1, 0);
        private const float FadeDuration = 0.5f;

        [SerializeField, Required]
        private SpriteRenderer one, two;
         
        [SerializeField, Required]
        private SpriteRenderer mistRenderer;
        
        [SerializeField, Required]
        private Transform animationParent;
        
        private readonly ValueHandler<Sprite> _spriteHandler = new();
        private readonly List<VisualNovelAnimation> _spawnedAnimations = new();

        private SpriteRenderer _activeSpriteRenderer, _inactiveSpriteRenderer;
        private Tween _tween;
        private TweenCallback _disableSpriteRendererOne, _disableSpriteRendererTwo;

        protected override void Awake()
        {
            base.Awake();
            _spriteHandler.Changed += CgChanged;
            _activeSpriteRenderer = one;
            _inactiveSpriteRenderer = two;
            
            _disableSpriteRendererOne = () =>
            {
                one.sprite = null;
                one.color = ClearWhite;
            };
            
            _disableSpriteRendererTwo = () =>
            {
                two.sprite = null;
                two.color = ClearWhite;
            };
        }

        protected override void OnDestroy()
        {
            _spriteHandler.Changed -= CgChanged;
            _tween.KillIfActive();
            base.OnDestroy();
        }
        
        private void CgChanged(Sprite cg)
        {
            _tween.KillIfActive();
            if (_activeSpriteRenderer.color.a <= 0f || DialogueDisplay.SkipHandler.Value || cg == _activeSpriteRenderer.sprite)
            {
                SetActiveImmediate(cg);
                return;
            }

            if (_activeSpriteRenderer == two)
                SwitchOneToActive(cg);
            else
                SwitchTwoToActive(cg);
        }

        private void SetActiveImmediate(Sprite cg)
        {
            _activeSpriteRenderer.color = Color.white;
            _activeSpriteRenderer.sprite = cg;
            _activeSpriteRenderer.sortingOrder = 1;
            _inactiveSpriteRenderer.sprite = null;
            _inactiveSpriteRenderer.color = ClearWhite;
            _inactiveSpriteRenderer.sortingOrder = 0;
        }

        private void SwitchOneToActive(Sprite cg)
        {
            one.color = ClearWhite;
            one.sortingOrder = 1;
            one.sprite = cg;
            two.sortingOrder = 0;
            _tween = one.DOFade(endValue: 1f, FadeDuration).OnComplete(_disableSpriteRendererTwo);
            (_activeSpriteRenderer, _inactiveSpriteRenderer) = (one, two);
        }
        
        private void SwitchTwoToActive(Sprite cg)
        {
            two.color = ClearWhite;
            two.sortingOrder = 1;
            two.sprite = cg;
            one.sortingOrder = 0;
            _tween = two.DOFade(endValue: 1f, FadeDuration).OnComplete(_disableSpriteRendererOne);
            (_activeSpriteRenderer, _inactiveSpriteRenderer) = (two, one);
        }

        public void Set(string fileName)
        {
            ClearSpawnedAnimations();
            if (CgDatabase.GetCg(fileName).TrySome(out Sprite cg))
                _spriteHandler.SetValue(cg);
            else
            {
                Debug.LogWarning($"CG {fileName} not found.");
                _spriteHandler.SetValue(null);
            }
        }
        
        public void End()
        {
            ClearSpawnedAnimations();
            _spriteHandler.SetValue(null);
        }

        public IEnumerator SetAnim(string fileName)
        {
            _spriteHandler.SetValue(null);
            ClearSpawnedAnimations();
            Option<ResourceHandle<VisualNovelAnimation>> animationPrefab = CgDatabase.GetCgAnimationPrefab(fileName);
            if (animationPrefab.TrySome(out ResourceHandle<VisualNovelAnimation> handle) && handle.HasResult)
            {
                VisualNovelAnimation animationObject = animationPrefab.Value.Resource.Value.InstantiateWithFixedLocalScaleAndPosition(animationParent);
                _spawnedAnimations.Add(animationObject);

                return new YieldableCommandWrapper(animationObject.Play().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: () =>
                {
                    if (animationObject != null)
                        animationObject.ForceFinalState();
                });
            }

            Debug.LogWarning($"Cg animation {fileName} not found.");
            return null;
        }

        public void SetAnimAsync(string fileName)
        {
            _spriteHandler.SetValue(null);
            ClearSpawnedAnimations();
            Option<ResourceHandle<VisualNovelAnimation>> animationPrefab = CgDatabase.GetCgAnimationPrefab(fileName);
            if (animationPrefab.TrySome(out ResourceHandle<VisualNovelAnimation> handle) && handle.HasResult)
            {
                VisualNovelAnimation animationObject = Instantiate(animationPrefab.Value.Resource.Value, animationParent);
                _spawnedAnimations.Add(animationObject);
                animationObject.Play();
            }
            else
            {
                Debug.LogWarning($"Cg animation {fileName} not found.");
            }
        }

        private void ClearSpawnedAnimations()
        {
            foreach (VisualNovelAnimation animationObject in _spawnedAnimations)
            {
                animationObject.ForceFinalState();
                Destroy(animationObject.gameObject);
            }

            _spawnedAnimations.Clear();
        }

        public void SetMist(float transparencyPercentage)
        {
            mistRenderer.enabled = true;
            Color color = mistRenderer.color;
            color.a = transparencyPercentage;
            mistRenderer.DOColor(color, FadeDuration);
        }

        public void EndMist()
        {
            mistRenderer.enabled = false;
        }
    }
}