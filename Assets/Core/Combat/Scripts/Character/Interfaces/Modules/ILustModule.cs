using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface ILustModule : IModule
    {
        protected CharacterStateMachine Owner { get; }

    #region Lust
        public const uint MaxLust = 200;
        public static uint ClampLust(uint value) => value.Clamp(0, MaxLust);
        
        protected uint Lust { get; }
        public uint GetLust() => ClampLust(Lust);

        protected void SetLustInternal(uint value);
        public void SetLust(uint value)
        {
            if (value == GetLust())
                return;
            
            SetLustInternal(ClampLust(value));
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.SetLust(stats.Key, GetLust());
        }
        
        protected void ChangeLustInternal(int delta);
        public void ChangeLust(int delta)
        {
            uint oldValue = GetLust();
            uint newValue = ClampLust((uint)((int)oldValue + delta));
            delta = (int)newValue - (int)oldValue;
            if (delta == 0)
                return;
            
            ChangeLustInternal(delta);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.SetLust(stats.Key, GetLust());
        }
        
        protected void ChangeLustViaActionInternal(int rawDelta, int actualDelta);
        public void ChangeLustViaAction(int delta)
        {
            uint oldValue = GetLust();
            uint newValue = ClampLust((uint)((int)oldValue + delta));
            int actualDelta = (int)newValue - (int)oldValue;
            
            ChangeLustViaActionInternal(rawDelta: delta, actualDelta);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.SetLust(stats.Key, GetLust());
        }
        
    #endregion

    #region Orgasm
        public static uint ClampOrgasmCount(uint orgasmCount, uint orgasmLimit) => orgasmCount.Clamp(0, orgasmLimit);
        
        protected uint OrgasmCount { get; }
        public uint GetOrgasmCount() => ClampOrgasmCount(OrgasmCount, OrgasmLimit);

        protected void SetOrgasmCountInternal(uint value);
        protected void SetOrgasmCount(uint value)
        {
            value = ClampOrgasmCount(value, OrgasmLimit);
            SetOrgasmCountInternal(value);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats readonlyCharacterStats) && Save.AssertInstance(out Save save))
                save.SetOrgasmCount(readonlyCharacterStats.Key, GetOrgasmCount());
        }

        uint OrgasmLimit { get; }

        protected void SetOrgasmLimitInternal(uint value);
        protected void SetOrgasmLimit(uint value)
        {
            SetOrgasmLimitInternal(value);
            SetOrgasmCount(GetOrgasmCount());
        }
    #endregion

    #region Composure
        public const float MaxComposure = 1f;
        public const float MinComposure = -1f;
        public static float ClampComposure(float composure) => composure.Clamp(MinComposure, MaxComposure);

        float BaseComposure { get; set; }
        void SubscribeComposure(IBaseFloatAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeComposure(IBaseFloatAttributeModifier modifier);

        protected float GetComposureInternal();
        public float GetComposure() => ClampComposure(GetComposureInternal());
    #endregion

    #region Temptation
        public const float TemptationDeltaPerStepOnMaxLust = 0.02f;
        public const float TemptationDeltaOnOrgasm = -0.40f;

        protected ClampedPercentage Temptation { get; }
        public ClampedPercentage GetTemptation() => Temptation; //ClampedPercentage handles the clamping internally
        
        protected void SetTemptationInternal(ClampedPercentage value);
        void SetTemptation(ClampedPercentage value)
        {
            if (value == GetTemptation())
                return;
            
            SetTemptationInternal(value);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats readonlyCharacterStats) && Save.AssertInstance(out Save save))
                save.SetTemptation(readonlyCharacterStats.Key, GetTemptation());
        }
        
        protected void ChangeTemptationInternal(float delta);
        public void ChangeTemptation(float delta)
        {
            if (delta > 0)
                delta *= 1f - GetComposureInternal();

            ClampedPercentage oldValue = GetTemptation();
            ClampedPercentage newValue = oldValue + delta;
            delta = newValue - oldValue;
            if (delta == 0f)
                return;
            
            ChangeTemptationInternal(delta);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats readonlyCharacterStats) && Save.AssertInstance(out Save save))
                save.SetTemptation(readonlyCharacterStats.Key, GetTemptation());
        }
        
    #endregion

        void Tick(float timeStep);

        public const float CorruptionDeltaOnOrgasm = +0.02f;
        public const uint SexualExpDeltaOnOrgasm = +5;
        public const uint SexualExpDeltaPerSexSecond = +1;

        protected void OrgasmInternal();
        public void Orgasm()
        {
            SetOrgasmCount(GetOrgasmCount() + 1);
            OrgasmInternal();
            
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
            {
                save.SetLust(stats.Key, GetLust());
                save.SetOrgasmCount(stats.Key, GetOrgasmCount());
                save.ChangeCorruption(stats.Key, CorruptionDeltaOnOrgasm);

                if (Owner.StatusModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled))
                {
                    save.IncrementSexualExp(stats.Key, lustGrappled.Restrainer.Script.Race, SexualExpDeltaOnOrgasm);
                }
            }
        }

        public void IncrementSexualExp(Race race, uint amount)
        {
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.IncrementSexualExp(stats.Key, race, amount);
        }
    }
}