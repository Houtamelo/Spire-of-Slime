using Core.Local_Map.Scripts.Enums;
using Core.Utils.Async;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Local_Map.Scripts.Events.Rest
{
    [CreateAssetMenu(fileName = "New Rest Event", menuName = "Database/Local Map/Events/Rest Event")]
    public class RestEvent : ScriptableLocalMapEvent
    {
        public override IconType GetIconType(in Option<float> multiplier) => IconType.Rest;

        [SerializeField]
        private RestEventBackground backgroundPrefab;

        protected virtual float RestMultiplier => DefaultRestEvent.RestMultiplier;
        protected virtual float RestMultiplierAmplitude => DefaultRestEvent.RestMultiplierAmplitude;
        protected virtual int LustDecrease => DefaultRestEvent.LustDecrease;
        protected virtual int OrgasmRestore => DefaultRestEvent.OrgasmRestore;
        protected virtual int ExhaustionDecrease => DefaultRestEvent.ExhaustionDecrease;

        public override bool AllowSaving => false;
        
        [NotNull]
        public override CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier) 
            => RestEventHandler.HandleRest(RestMultiplier, RestMultiplierAmplitude, LustDecrease, ExhaustionDecrease, OrgasmRestore, backgroundPrefab);
    }
}