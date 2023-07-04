using System.Text;
using Core.Combat.Scripts.Managers;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;

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
        private float _recovery, _charge, _downed, _stun;

        public void SetRecovery(bool active, float remaining, float total, CombatManager combatManager)
        {
            _recoveryTween.CompleteIfActive();
            recoveryBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _recovery = -999f;
                return;
            }
            
            _recovery = remaining;
            UpdateValueText();
            float fill = GetRecoveryStunFill(remaining, total);
            float timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep <= 0f || speed <= 0f)
            {
                recoveryBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep /= speed;
            _recoveryTween = recoveryBar.DOFillAmount(fill, timeUntilNextStep);
        }

        public void SetCharge(bool active, float remaining, float total, CombatManager combatManager)
        {
            _chargeTween.CompleteIfActive();
            chargeBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _charge = -999f;
                return;
            }
            
            _charge = remaining;
            UpdateValueText();
            float fill = GetChargeDownedFill(remaining, total);
            float timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep <= 0f || speed <= 0f)
            {
                chargeBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep /= speed;
            _chargeTween = chargeBar.DOFillAmount(fill, timeUntilNextStep);
        }

        public void SetDowned(bool active, float remaining, float total, CombatManager combatManager)
        {
            _downedTween.CompleteIfActive();
            downedBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _downed = -999f;
                return;
            }
            
            _downed = remaining;
            UpdateValueText();
            float fill = GetChargeDownedFill(remaining, total);
            float timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep <= 0f || speed <= 0f)
            {
                downedBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep /= speed;
            _downedTween = downedBar.DOFillAmount(fill, timeUntilNextStep);
        }

        public void SetStun(bool active, float remaining, float total, CombatManager combatManager)
        {
            _stunTween.CompleteIfActive();
            stunBarGameObject.SetActive(active);
            gameObject.SetActive(recoveryBarGameObject.activeSelf || chargeBarGameObject.activeSelf || downedBarGameObject.activeSelf || stunBarGameObject.activeSelf);
            if (active == false)
            {
                _stun = -999f;
                return;
            }
            
            _stun = remaining;
            UpdateValueText();
            float fill = GetRecoveryStunFill(remaining, total);
            float timeUntilNextStep = CombatManager.TimePerStep - combatManager.AccumulatedStepTime;
            float speed = combatManager.SpeedHandler.Value;
            if (combatManager.PauseHandler.Value || timeUntilNextStep <= 0f || speed <= 0f)
            {
                stunBar.fillAmount = fill;
                return;
            }

            timeUntilNextStep /= speed;
            _stunTween = stunBar.DOFillAmount(fill, timeUntilNextStep);
        }

        private void UpdateValueText()
        {
            _stringBuilder.Clear();
            bool anyBehind = false;
            
            if (_recovery > 0)
            {
                _stringBuilder.Append(ColorReferences.RecoveryRichText.start);
                _stringBuilder.Append(_recovery.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.RecoveryRichText.end);
                anyBehind = true;
            }
            
            if (_charge > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.ChargeRichText.start);
                _stringBuilder.Append(_charge.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.ChargeRichText.end);
                anyBehind = true;
            }
            
            if (_downed > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.KnockedDownRichText.start);
                _stringBuilder.Append(_downed.ToString("0.0"));
                _stringBuilder.Append(ColorReferences.KnockedDownRichText.start);
                anyBehind = true;
            }
            
            if (_stun > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.StunRichText.start);
                _stringBuilder.Append(_stun.ToString("0.0"));
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

        private static float GetChargeDownedFill(float remaining, float total)
        {
            float fill = (total - remaining) / total;
            if (total <= 0f || float.IsNaN(f: fill) || float.IsInfinity(f: fill))
                fill = 0f;
            
            return fill;
        }
        
        private static float GetRecoveryStunFill(float remaining, float total)
        {
            float fill = remaining / total;
            if (total <= 0 || float.IsNaN(f: fill) || float.IsInfinity(f: fill))
                fill = 0;
            
            return fill;
        }
    }
}