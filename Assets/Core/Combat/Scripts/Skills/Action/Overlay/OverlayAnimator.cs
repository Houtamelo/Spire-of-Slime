using Core.Combat.Scripts.Managers;
using DG.Tweening;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills.Action.Overlay
{
    public abstract class OverlayAnimator : MonoBehaviour
    {
        public abstract Sequence Announce(Announcer announcer, PlannedSkill plan, float startDuration, float popDuration, float speed);
        
        public abstract void FadeUp(float duration, PlannedSkill plan);
        public abstract void FadeDown(float duration);
    }
}