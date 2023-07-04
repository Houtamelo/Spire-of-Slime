using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Animation
{
    public class OnEnterVariableSetter : StateMachineBehaviour
    {
        [SerializeField]
        private VariableType variableType;
        
        [SerializeField]
        private string variableName;
        
        [SerializeField, ShowIf(nameof(ShowRandomize))] 
        private bool randomize;
        
        [SerializeField, ShowIf(nameof(ShowRandomize))]
        private bool weightedDistribution;
        
        [SerializeField, ShowIf(nameof(ShowAnimationCurve))] 
        private AnimationCurve distributionCurve;
        
        [SerializeField, ShowIf(nameof(ShowInt)), LabelText(@"$IntLabel")]
        private int minIntValue;
        
        [SerializeField, ShowIf(nameof(ShowFloat)), LabelText(@"$FloatLabel")]
        private float minFloatValue;
        
        [SerializeField, ShowIf(nameof(ShowBool))] 
        private bool boolValue;
        
        [SerializeField, ShowIf(nameof(ShowIntMaxValue)), LabelText("Max Value (Inclusive)")] 
        private int maxIntValue;
        
        [SerializeField, ShowIf(nameof(ShowFloatMaxValue)), LabelText("Max Value")]
        private float maxFloatValue;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            switch (variableType)
            {
                case VariableType.Int when randomize && weightedDistribution:
                    animator.SetInteger(variableName, Mathf.FloorToInt(Mathf.Lerp(minIntValue, maxIntValue, distributionCurve.Evaluate(Random.value))));
                    break;
                case VariableType.Int when randomize:
                    animator.SetInteger(variableName, Random.Range(minIntValue, maxIntValue + 1));
                    break;
                case VariableType.Int:
                    animator.SetInteger(variableName, minIntValue);
                    break;
                case VariableType.Float when randomize && weightedDistribution:
                    animator.SetFloat(variableName, Mathf.Lerp(minFloatValue, maxFloatValue, distributionCurve.Evaluate(Random.value)));
                    break;
                case VariableType.Float when randomize:
                    animator.SetFloat(variableName, Random.Range(minFloatValue, maxFloatValue));
                    break;
                case VariableType.Float:
                    animator.SetFloat(variableName, minFloatValue);
                    break;
                case VariableType.Bool when randomize && weightedDistribution:
                    animator.SetBool(variableName, distributionCurve.Evaluate(Random.value) > 0.5f);
                    break;
                case VariableType.Bool when randomize:
                    animator.SetBool(variableName, Random.value > 0.5f);
                    break;
                case VariableType.Bool:
                    animator.SetBool(variableName, boolValue);
                    break;
                case VariableType.Trigger:
                    animator.SetTrigger(variableName);
                    break;
            }
            
            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        private bool ShowInt => variableType == VariableType.Int;
        private bool ShowIntMaxValue => variableType == VariableType.Int && randomize;
        private bool ShowFloat => variableType == VariableType.Float;
        private bool ShowFloatMaxValue => variableType == VariableType.Float && randomize;
        private bool ShowBool => variableType == VariableType.Bool && !randomize;
        private bool ShowRandomize => variableType != VariableType.Trigger;
        private bool ShowAnimationCurve => weightedDistribution;
        
        [UsedImplicitly] private string IntLabel => randomize ? "Min Value" : "Value";
        [UsedImplicitly] private string FloatLabel => randomize ? "Min Value" : "Value";
        private enum VariableType
        {
            Bool,
            Int,
            Float,
            Trigger
        }
    }
}