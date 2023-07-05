using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Managers;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Objects;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Cues
{
    public sealed class StatusVFXManager : Singleton<StatusVFXManager>
    {
        private static readonly ListPool<StatusCueHandler> ReusableList = new(32);
        private static readonly StringBuilder Builder = new();

        [OdinSerialize, Required]
        private AudioClip _resistClip;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource poisonTickSource;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource lustIncreaseSource;
        public CustomAudioSource LustIncreaseSource => lustIncreaseSource;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource lustDecreaseSource;
        public CustomAudioSource LustDecreaseSource => lustDecreaseSource;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource temptationDecreaseSource;
        public CustomAudioSource TemptationDecreaseSource => temptationDecreaseSource;
        
        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource temptationIncreaseSource;
        public CustomAudioSource TemptationIncreaseSource => temptationIncreaseSource;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource healSource;
        public CustomAudioSource HealSource => healSource;

        [OdinSerialize, SceneObjectsOnly, Required]
        private Transform _statusEffectCuesParent;

        [OdinSerialize, AssetsOnly, Required]
        private StatusEffectCue _statusEffectCuePrefab;

        [OdinSerialize, Required, SceneObjectsOnly]
        private readonly CombatManager _combatManager;

        private readonly HashSet<StatusEffectCue> _idleCues = new(32);
        private readonly Dictionary<CharacterStateMachine, StatusEffectCue> _playingCues = new(32);
        private readonly Dictionary<CharacterStateMachine, List<StatusCueHandler>> _pendingCues = new();

        public bool IsBusy() => _playingCues.Values.Any(cue => cue.BeingUsed);
        public bool AnyPending() => _pendingCues.Any(pair => pair.Value.Count > 0);

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < 6; i++)
                CreateCue();
        }

        public void Enqueue(StatusCueHandler handler)
        {
            if (_pendingCues.TryGetValue(handler.Character, out List<StatusCueHandler> list) == false)
                _pendingCues[handler.Character] = list = new List<StatusCueHandler>(8);

            list.Add(handler);
        }
        
        /// <returns>If we are busy</returns>
        public bool PlayPendingCues()
        {
            if (IsBusy())
                return true;

            bool anyPlayed = false;
            foreach ((CharacterStateMachine character, List<StatusCueHandler> queue) in _pendingCues)
            {
                if (queue.Count == 0)
                    continue;
                
                ReusableList.Clear();
                Option<StatusCueHandler> firstOption = Option.None;
                for (int index = 0; index < queue.Count; index++)
                {
                    StatusCueHandler candidate = queue[index];
                    if (candidate.IsValid() == false)
                    {
                        queue.RemoveAt(index);
                        index--;
                    }
                    else
                    {
                        firstOption = candidate;
                        break;
                    }
                }
                
                if (firstOption.IsNone)
                    continue;

                anyPlayed = true;
                StatusCueHandler first = firstOption.Value;
                for (int index = 0; index < queue.Count; index++)
                {
                    StatusCueHandler element = queue[index];
                    if (first.CanGroupWith(element))
                    {
                        ReusableList.Add(element);
                        queue.RemoveAt(index);
                        index--;
                    }
                }

                for (int index = 0; index < ReusableList.Count; index++)
                {
                    StatusCueHandler handler = ReusableList[index];
                    if (handler.IsValid() == false)
                    {
                        ReusableList.RemoveAt(index);
                        index--;
                    }
                }
                
                if (ReusableList.Count == 0)
                {
                    PlayCue(first);
                    continue;
                }

                ReusableList.Add(first);
                PlayGrouped(ReusableList.AsSpan(), character);
                ReusableList.Clear();
            }

            return anyPlayed || IsBusy();
        }

        private void PlayCue(StatusCueHandler cueHandler)
        {
            if (cueHandler.Character.Display.TrySome(out CharacterDisplay display) == false)
                return;

            Option<StatusEffectCue> cue = cueHandler.Type switch
            {
                StatusCueHandler.CueType.EffectApplied  => AnimateStatusEffect(display, cueHandler.GetEffectType(), cueHandler.GetSuccess(), instanceCount: 1),
                StatusCueHandler.CueType.PoisonTick     => AnimatePoisonTick(display, cueHandler.GetPoisonAmount()),
                StatusCueHandler.CueType.LustTick       => AnimateLustTick(display, cueHandler.GetLustDelta()),
                StatusCueHandler.CueType.TemptationTick => AnimateTemptationTick(display, cueHandler.GetTemptationAmount()),
                _                                       => throw new ArgumentOutOfRangeException(nameof(cueHandler.Type), $"Unhandled cue type: {cueHandler.Type}")
            };
            
            if (cue.IsNone)
                return;
            
            _playingCues[cueHandler.Character] = cue.Value;
            cueHandler.NotifyStart();
        }

        private void PlayGrouped(ReadOnlySpan<StatusCueHandler> handlers, CharacterStateMachine character)
        {
            if (character.Display.TrySome(out CharacterDisplay display) == false)
                return;
            
            StatusCueHandler first = handlers[0];
            Option<StatusEffectCue> cue;
            switch (first.Type)
            {
                case StatusCueHandler.CueType.EffectApplied: 
                    cue = AnimateStatusEffect(display, first.GetEffectType(), first.GetSuccess(), (uint)handlers.Length); break;
                case StatusCueHandler.CueType.PoisonTick:
                    uint totalPoison = 0;
                    for (int i = 0; i < handlers.Length; i++)
                        totalPoison += handlers[i].GetPoisonAmount();
                    
                    cue = AnimatePoisonTick(display, totalPoison);
                    break;
                case StatusCueHandler.CueType.LustTick:
                    int totalLust = 0;
                    for (int i = 0; i < handlers.Length; i++)
                        totalLust += handlers[i].GetLustDelta();
                    
                    cue = AnimateLustTick(display, totalLust);
                    break;
                case StatusCueHandler.CueType.TemptationTick:
                    float totalTemptation = 0f;
                    for (int i = 0; i < handlers.Length; i++)
                        totalTemptation += handlers[i].GetTemptationAmount();
                    
                    cue = AnimateTemptationTick(display, totalTemptation);
                    break;
                default: 
                    throw new ArgumentOutOfRangeException(nameof(first.Type), $"Unhandled cue type: {first.Type}");
            }
            
            if (cue.IsNone)
                return;
            
            _playingCues[character] = cue.Value;
            foreach (StatusCueHandler handler in handlers)
                handler.NotifyStart();
        }

        private Option<StatusEffectCue> AnimateStatusEffect(CharacterDisplay character, EffectType effectType, bool success, uint instanceCount = 1)
        {
            Option<(Sprite sprite, AudioClip clip)> spriteAndAudio = StatusEffectsDatabase.GetStatusSpriteAndSfx(effectType);
            if (spriteAndAudio.IsNone)
                return Option.None;

            Builder.Clear();
            AudioClip audioClip = spriteAndAudio.Value.clip;
            Vector3 direction = Vector3.up * 0.5f;
            if (success == false)
            {
                Builder.Append("Resist");
                audioClip = _resistClip;
            }
            else
            {
                Builder.Append(GetEffectDisplayName(effectType));
            }

            if (instanceCount != 1)
                Builder.Append(" (", instanceCount.ToString(), ')');

            if (character.GetCuePosition().TrySome(out Vector3 position) == false)
                return Option.None;

            string text = Builder.ToString();
            StatusEffectCue cue = GetAvailableCue();
            cue.Animate(Option<Sprite>.Some(spriteAndAudio.Value.sprite), text, effectType.GetColor(), position, direction, Option<AudioClip>.Some(audioClip));
            return cue;
        }

        private Option<StatusEffectCue> AnimatePoisonTick(CharacterDisplay character, uint damageDealt)
        {
            if (character.GetCuePosition().AssertSome(out Vector3 position) == false)
                return Option.None;

            StatusEffectCue cue = GetAvailableCue();
            cue.Animate(Option<Sprite>.None, damageDealt.ToString("0"), EffectType.Poison.GetColor(), position, Vector3.up * 0.5f, Option<AudioClip>.None);
            poisonTickSource.transform.position = character.transform.position;
            poisonTickSource.PlayOneShot(poisonTickSource.Clip);
            return cue;
        }

        private Option<StatusEffectCue> AnimateLustTick(CharacterDisplay character, int lustDelta)
        {
            if (character.GetCuePosition().AssertSome(out Vector3 position) == false)
                return Option.None;
            
            StatusEffectCue cue = GetAvailableCue();
            cue.Animate(Option<Sprite>.None, lustDelta.WithSymbol(), EffectType.Lust.GetColor(), position, Vector3.up * 0.5f, Option<AudioClip>.None);
            CustomAudioSource targetSource = lustDelta > 0 ? lustIncreaseSource : lustDecreaseSource;
            targetSource.transform.position = character.transform.position;
            targetSource.PlayOneShot(targetSource.Clip);
            return cue;
        }

        private Option<StatusEffectCue> AnimateTemptationTick(CharacterDisplay character, float temptation)
        {
            if (character.GetCuePosition().AssertSome(out Vector3 position) == false)
                return Option.None;
            
            StatusEffectCue cue = GetAvailableCue();
            cue.Animate(Option<Sprite>.None, temptation.ToPercentlessStringWithSymbol(digits: 1, decimalDigits: 0), EffectType.Temptation.GetColor(), position, Vector3.up * 0.5f, Option<AudioClip>.None);
            CustomAudioSource targetSource = temptation > 0 ? temptationIncreaseSource : temptationDecreaseSource;
            targetSource.transform.position = character.transform.position;
            targetSource.PlayOneShot(targetSource.Clip);
            return cue;
        }

        private StatusEffectCue CreateCue()
        {
            StatusEffectCue cue = _statusEffectCuePrefab.InstantiateWithFixedLocalScale(_statusEffectCuesParent);
            cue.Initialize(manager: this);
            _idleCues.Add(cue);
            return cue;
        }

        private StatusEffectCue GetAvailableCue()
        {
            if (_idleCues.Count > 0)
                return _idleCues.First();

            return CreateCue();
        }

        public void NotifyFadeFinished(StatusEffectCue cue)
        {
            foreach ((CharacterStateMachine character, StatusEffectCue setCue) in _playingCues.FixedEnumerate())
            {
                if (setCue == cue)
                {
                    _playingCues.Remove(character);
                    break;
                }
            }
        }

        public void NotifyAudioFinished(StatusEffectCue cue)
        {
            _idleCues.Add(cue);
        }

        public void PlayImmediate(CharacterDisplay character, EffectType effectType, bool success)
        {
            if (character == null)
                return;

            Option<(Sprite sprite, AudioClip clip)> spriteAndAudio = StatusEffectsDatabase.GetStatusSpriteAndSfx(effectType);
            if (spriteAndAudio.IsNone)
                return;

            Sprite sprite = spriteAndAudio.Value.sprite;
            Option<AudioClip> audioClip = spriteAndAudio.Value.clip != null ? spriteAndAudio.Value.clip : Option<AudioClip>.None;

            Vector3 direction = Vector3.up * 0.5f;
            string text = success == false ? "Resist" : GetEffectDisplayName(effectType);
            
            if (character.GetCuePosition().TrySome(out Vector3 position) == false)
                return;

            //Vector3 position = bounds.center + new Vector3(0, bounds.extents.y * 0.9f);
            StatusEffectCue cue = GetAvailableCue();
            cue.Animate(sprite, text, effectType.GetColor(), position, direction, audioClip);
        }

        private static string GetEffectDisplayName(EffectType effectType) =>
            effectType switch
            {
                EffectType.Buff           => "Buff",
                EffectType.Debuff         => "Debuff",
                EffectType.Poison         => "Poison",
                EffectType.Arousal        => "Arousal",
                EffectType.Riposte        => "Riposte",
                EffectType.OvertimeHeal   => "Heal",
                EffectType.Marked         => "Mark",
                EffectType.Stun           => "Stun",
                EffectType.Guarded        => "Guard",
                EffectType.LustGrappled   => "Grapple",
                EffectType.Summon         => "Summon",
                EffectType.Temptation     => string.Empty,
                EffectType.Move           => string.Empty,
                EffectType.Perk           => string.Empty,
                EffectType.HiddenPerk     => string.Empty,
                EffectType.Heal           => string.Empty,
                EffectType.Lust           => string.Empty,
                EffectType.NemaExhaustion => string.Empty,
                EffectType.Mist           => string.Empty,
                _                         => string.Empty
            };
    }
}