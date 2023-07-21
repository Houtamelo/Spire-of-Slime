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
        private const float BarFillDuration = 0.5f;
        
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
            
            float fill = GetRecoveryOrStunFill(remaining, total);
            float difference = Mathf.Abs(fill - recoveryBar.fillAmount);
            
            if (combatManager.PauseHandler.Value || difference < 0.0001f)
                recoveryBar.fillAmount = fill;
            else
                _recoveryTween = recoveryBar.DOFillAmount(endValue: fill, duration: difference * BarFillDuration);
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
            float difference = Mathf.Abs(fill - chargeBar.fillAmount);
            
            if (combatManager.PauseHandler.Value || difference < 0.0001f)
                chargeBar.fillAmount = fill;
            else
                _chargeTween = chargeBar.DOFillAmount(endValue: fill, duration: difference * BarFillDuration);
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
            float difference = Mathf.Abs(fill - downedBar.fillAmount);
            
            if (combatManager.PauseHandler.Value || difference < 0.0001f)
                downedBar.fillAmount = fill;
            else
                _downedTween = downedBar.DOFillAmount(endValue: fill, duration: difference * BarFillDuration);
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
            float fill = GetRecoveryOrStunFill(remaining, total);
            float difference = Mathf.Abs(fill - stunBar.fillAmount);
            
            if (combatManager.PauseHandler.Value || difference < 0.0001f)
                stunBar.fillAmount = fill;
            else
                _stunTween = stunBar.DOFillAmount(endValue: fill, duration: difference * BarFillDuration);
        }

        private void UpdateValueText()
        {
            _stringBuilder.Clear();
            bool anyBehind = false;
            
            if (_recovery.Ticks > 0)
            {
                _stringBuilder.Append(ColorReferences.RecoveryRichText.start, _recovery.Seconds.ToString("0.0"), ColorReferences.RecoveryRichText.end);
                anyBehind = true;
            }
            
            if (_charge.Ticks > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.ChargeRichText.start, _charge.Seconds.ToString("0.0"), ColorReferences.ChargeRichText.end);
                anyBehind = true;
            }
            
            if (_downed.Ticks > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.KnockedDownRichText.start, _downed.Seconds.ToString("0.0"), ColorReferences.KnockedDownRichText.start);
                anyBehind = true;
            }
            
            if (_stun.Ticks > 0)
            {
                if (anyBehind)
                    _stringBuilder.Append(" | ");
                
                _stringBuilder.Append(ColorReferences.StunRichText.start, _stun.Seconds.ToString("0.0"), ColorReferences.StunRichText.end);
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
        
        private static float GetRecoveryOrStunFill(TSpan remaining, TSpan total)
        {
            float fill = remaining.FloatSeconds / total.FloatSeconds;
            if (total.Ticks <= 0 || float.IsNaN(fill) || float.IsInfinity(fill))
                fill = 0;
            
            return fill;
        }
    }
}