using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record LustRecord : ModuleRecord
    {
        public abstract ILustModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface ILustModule : IModule
    {
        protected CharacterStateMachine Owner { get; }

    #region Lust
        public const int MaxLust = 200;
        public const int MinLust = 0;
        public static int ClampLust(int value) => value.Clamp(MinLust, MaxLust);
        
        protected int Lust { get; }
        public int GetLust() => ClampLust(Lust);

        protected void SetLustInternal(int clampedValue);
        public void SetLust(int value)
        {
            if (value == GetLust())
                return;
            
            SetLustInternal(ClampLust(value));
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.SetLust(stats.Key, GetLust());
        }
        
        protected void ChangeLustInternal(int clampedDelta);
        public void ChangeLust(int delta)
        {
            int oldValue = GetLust();
            int newValue = ClampLust(oldValue + delta);
            delta = newValue - oldValue;
            if (delta == 0)
                return;
            
            ChangeLustInternal(delta);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.SetLust(stats.Key, GetLust());
        }
        
        protected void ChangeLustViaActionInternal(int value, int clampedDelta);
        public void ChangeLustViaAction(int delta)
        {
            int oldValue = GetLust();
            int newValue = ClampLust(oldValue + delta);
            int clampedDelta = newValue - oldValue;
            
            ChangeLustViaActionInternal(delta, clampedDelta);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.SetLust(stats.Key, GetLust());
        }
        
    #endregion

    #region Orgasm

        public const int MinOrgasmCount = 0;
        public static int ClampOrgasmCount(int orgasmCount, int orgasmLimit) => orgasmCount.Clamp(MinOrgasmCount, Mathf.Max(MinOrgasmLimit, orgasmLimit));
        
        public const int MinOrgasmLimit = 1;
        public const int MaxOrgasmLimit = 10;
        public static int ClampOrgasmLimit(int orgasmLimit) => orgasmLimit.Clamp(MinOrgasmLimit, MaxOrgasmLimit);

        protected int OrgasmCount { get; }
        public int GetOrgasmCount() => ClampOrgasmCount(OrgasmCount, OrgasmLimit);

        protected void SetOrgasmCountInternal(int clampedValue);
        protected void SetOrgasmCount(int value)
        {
            value = ClampOrgasmCount(value, OrgasmLimit);
            SetOrgasmCountInternal(value);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats readonlyCharacterStats) && Save.AssertInstance(out Save save))
                save.SetOrgasmCount(readonlyCharacterStats.Key, GetOrgasmCount());
        }

        protected int OrgasmLimit { get; }
        public int GetOrgasmLimit() => ClampOrgasmLimit(OrgasmLimit);

        protected void SetOrgasmLimitInternal(int clampedValue);
        protected void SetOrgasmLimit(int value)
        {
            SetOrgasmLimitInternal(value);
            SetOrgasmCount(GetOrgasmCount());
        }
    #endregion

    #region Composure
        public const int MaxComposure = IStunModule.MaxStunMitigation;
        public const int MinComposure = IStunModule.MinStunMitigation;
        public static int ClampComposure(int composure) => composure.Clamp(MinComposure, MaxComposure);

        int BaseComposure { get; set; }
        void SubscribeComposure(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeComposure(IBaseAttributeModifier modifier);

        protected int GetComposureInternal();
        public int GetComposure() => ClampComposure(GetComposureInternal());
    #endregion

    #region Temptation
        public const int TemptationDeltaPerSecondOnMaxLust = 2;
        public const int TemptationDeltaOnOrgasm = -40;

        public const int MinTemptation = 0;
        public const int MaxTemptation = 100;
        public static int ClampTemptation(int temptation) => temptation.Clamp(MinTemptation, MaxTemptation);

        protected int Temptation { get; }
        public int GetTemptation() => Temptation; //ClampedPercentage handles the clamping internally
        
        protected void SetTemptationInternal(int clampedValue);
        void SetTemptation(int value)
        {
            value = ClampTemptation(value);
            if (value == GetTemptation())
                return;
            
            SetTemptationInternal(value);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats readonlyCharacterStats) && Save.AssertInstance(out Save save))
                save.SetTemptation(readonlyCharacterStats.Key, GetTemptation());
        }
        
        protected void ChangeTemptationInternal(int clampedDelta);
        public void ChangeTemptation(int delta)
        {
            int oldValue = GetTemptation();
            int newValue = ClampTemptation(oldValue + delta);
            delta = newValue - oldValue;
            if (delta == 0)
                return;
            
            ChangeTemptationInternal(delta);
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats readonlyCharacterStats) && Save.AssertInstance(out Save save))
                save.SetTemptation(readonlyCharacterStats.Key, GetTemptation());
        }

        public void ApplyTemptationPower(int power)
        {
            if (power <= 0)
                return;
            
            int lust = GetLust();
            int temptationDelta = CalculateAppliedTemptation(power, GetComposure(), lust);
            
            if (temptationDelta <= 0)
                return;
            
            ChangeTemptation(temptationDelta);
        }
        
        [Pure]
        public static int CalculateAppliedTemptation(int power, int composure, int lust)
        {
            double lustSquared = lust * lust;
            double extraPowerFromLust = lustSquared / 500.0;
            double multiplierFromLust = 1.0 + (lustSquared / 80000.0);
            
            double powerD = (power + extraPowerFromLust) * multiplierFromLust;
            double composureD = composure;

            double dividend = 10.0 * (powerD + (powerD * powerD / 500.0) - composureD - (composureD * composureD / 500.0));
            double divisor = 125.0 + (powerD * 0.25) + (composureD * 0.25) + (powerD * composureD * 0.0005);
            
            return (int)(dividend / divisor);
        }
        
    #endregion

        void Tick(TSpan timeStep);

#region Orgasm

        public const int CorruptionDeltaOnOrgasm = +2;
        public const int SexualExpDeltaOnOrgasm = +5;
        public const int SexualExpDeltaPerSexSecond = +1;

        protected void OrgasmInternal();
        public void Orgasm()
        {
            SetOrgasmCount(GetOrgasmCount() + 1);
            OrgasmInternal();

            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) == false || Save.AssertInstance(out Save save) == false)
                return;

            save.SetLust(stats.Key, GetLust());
            save.SetOrgasmCount(stats.Key, GetOrgasmCount());
            save.ChangeCorruption(stats.Key, CorruptionDeltaOnOrgasm);

            if (Owner.StatusReceiverModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled))
                save.IncrementSexualExp(stats.Key, lustGrappled.Restrainer.Script.Race, SexualExpDeltaOnOrgasm);
        }

#endregion

        public void IncrementSexualExp(Race race, int amount)
        {
            if (Owner.OutsideCombatSyncedStats.TrySome(out IReadonlyCharacterStats stats) && Save.AssertInstance(out Save save))
                save.IncrementSexualExp(stats.Key, race, amount);
        }

        public const int MinCorruption = 0;
        public const int MaxCorruption = 100;

        public static int ClampCorruption(int corruption) => corruption.Clamp(MinCorruption, MaxCorruption);

        LustRecord GetRecord();
    }
}