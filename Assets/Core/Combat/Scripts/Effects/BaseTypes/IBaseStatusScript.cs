using Core.Combat.Scripts.Behaviour.UI;
using Core.Utils.Patterns;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public interface IBaseStatusScript
    {
        EffectType EffectType { get; }
        bool IsPositive { get; }
        IActualStatusScript GetActual { get; }
        Option<PredictionIconsDisplay.IconType> GetPredictionIconType();
    }
}