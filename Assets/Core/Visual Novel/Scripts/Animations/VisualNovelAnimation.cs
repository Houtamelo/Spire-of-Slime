using UnityEngine;

namespace Core.Visual_Novel.Scripts.Animations
{
    public abstract class VisualNovelAnimation : MonoBehaviour
    {
        public abstract YieldInstruction Play();
        public abstract void ForceFinalState();
    }
}