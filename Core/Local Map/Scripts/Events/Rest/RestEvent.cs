using Core.Local_Map.Scripts.Enums;
using UnityEngine;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events.Rest
{
    [CreateAssetMenu(fileName = "New Rest Event", menuName = "Database/Local Map/Events/Rest Event")]
    public class RestEvent : ScriptableLocalMapEvent
    {
        public override IconType GetIconType(in Option<float> multiplier) => IconType.Rest;

        [SerializeField]
        private RestEventBackground backgroundPrefab;

        protected virtual float RestMultiplier => DefaultRestEvent.RestMultiplier;
        protected virtual float RestMultiplierDelta => DefaultRestEvent.RestMultiplierDelta;
        protected virtual int LustDecrease => DefaultRestEvent.LustDecrease;
        protected virtual int OrgasmRestore => DefaultRestEvent.OrgasmRestore;
        protected virtual float ExhaustionDecrease => DefaultRestEvent.ExhaustionDecrease;

        public override bool AllowSaving => false;
        
        public override CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier) => RestEventHandler.HandleRest(RestMultiplier, RestMultiplierDelta, LustDecrease, ExhaustionDecrease, backgroundPrefab);
    }
}