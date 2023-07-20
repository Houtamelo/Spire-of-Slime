using System.Text;
using Core.Combat.Scripts.Managers;
using Core.Utils.Extensions;
using Core.Utils.Math;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class ActionBars : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Required]
        private TMP_Text valueTmp;

        [SerializeField, Required]
        private Image recoveryBar, chargeBar, downedBar, stunBar;
        
        [SerializeField, Required]
        private GameObject recoveryBarGameObject, chargeBarGameObject, downedBarGameObject, stunBarGameObject;

        private readonly StringBuilder _stringBuilder = new();

        private Tween _recoveryTween, _chargeTween, _downedTween, _stunTween;
        private TSpan _recovery, _charge, _downed, _stun;

        public void SetRecovery(bool active, TSpan remaining, TSpan total, CombatManager combatManager)
        {
            _recoveryTween.CompleteIfActive();
            recoveryBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _recovery = TSpan.Zero;
                return;
            }
            
            _recovery = remaining;
            UpdateValueText();
            float fill = GetRecoveryStunFill(remaining, total);
            TSpan timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep.Ticks <= 0 || speed <= 0f)
            {
                recoveryBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep.Divide(speed);
            _recoveryTween = recoveryBar.DOFillAmount(endValue: fill, duration: timeUntilNextStep.FloatSeconds);
        }

        public void SetCharge(bool active, TSpan remaining, TSpan total, CombatManager combatManager)
        {
            _chargeTween.CompleteIfActive();
            chargeBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _charge = TSpan.Zero;
                return;
            }
            
            _charge = remaining;
            UpdateValueText();
            float fill = GetChargeDownedFill(remaining, total);
            TSpan timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep.Ticks <= 0 || speed <= 0f)
            {
                chargeBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep.Divide(speed);
            _chargeTween = chargeBar.DOFillAmount(endValue: fill, duration: timeUntilNextStep.FloatSeconds);
        }

        public void SetDowned(bool active, TSpan remaining, TSpan total, CombatManager combatManager)
        {
            _downedTween.CompleteIfActive();
            downedBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _downed = TSpan.Zero;
                return;
            }
            
            _downed = remaining;
            UpdateValueText();
            float fill = GetChargeDownedFill(remaining, total);
            TSpan timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep.Ticks <= 0 || speed <= 0f)
            {
                downedBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep.Divide(speed);
            _downedTween = downedBar.DOFillAmount(endValue: fill, duration: timeUntilNextStep.FloatSeconds);
        }

        public void SetStun(bool active, TSpan remaining, TSpan total, CombatManager combatManager)
        {
            _stunTween.CompleteIfActive();
            stunBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _stun = TSpan.Zero;
                return;
            }
            
            _stun = remaining;
            UpdateValueText();
            float fill = GetRecoveryStunFill(remaining, total);
            TSpan timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep.Ticks <= 0 || speed <= 0f)
            {
                stunBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep.Divide(speed);
            _stunTween = stunBar.DOFillAmount(endValue: fill, duration: timeUntilNextStep.FloatSeconds);
        }

        private void UpdateValueText()
        {
            _stringBuilder.Clear();
            bool anyBehind = false;
            
            if (_recovery.Ticks > 0)
            {
                _stringBuilder.Append(ColorReferences.RecoveryRichText.start);
                _stringBuilder.Append(_recovery.Seconds.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.RecoveryRichText.end);
                anyBehind = true;
            }
            
            if (_charge.Ticks > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.ChargeRichText.start);
                _stringBuilder.Append(_charge.Seconds.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.ChargeRichText.end);
                anyBehind = true;
            }
            
            if (_downed.Ticks > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.KnockedDownRichText.start);
                _stringBuilder.Append(_downed.Seconds.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.KnockedDownRichText.start);
                anyBehind = true;
            }
            
            if (_stun.Ticks > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.StunRichText.start);
                _stringBuilder.Append(_stun.Seconds.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.StunRichText.end);
            }
            
            valueTmp.text = _stringBuilder.ToString();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            valueTmp.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            valueTmp.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _recoveryTween.KillIfActive();
            _chargeTween.KillIfActive();
            _downedTween.KillIfActive();
            _stunTween.KillIfActive();
        }

        private void OnDestroy()
        {
            _recoveryTween.KillIfActive();
            _chargeTween.KillIfActive();
            _downedTween.KillIfActive();
            _stunTween.KillIfActive();
        }

        private static float GetChargeDownedFill(TSpan remaining, TSpan total)
        {
            float fill = (total - remaining).FloatSeconds / total.FloatSeconds;
            if (total.Ticks <= 0 || float.IsNaN(fill) || float.IsInfinity(fill))
                fill = 0f;
            
            return fill;
        }
        
        private static float GetRecoveryStunFill(TSpan remaining, TSpan total)
        {
            float fill = remaining.FloatSeconds / total.FloatSeconds;
            if (total.Ticks <= 0 || float.IsNaN(fill) || float.IsInfinity(fill))
                fill = 0;
            
            return fill;
        }
    }
}