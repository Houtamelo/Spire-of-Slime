using Core.Combat.Scripts.Effects.BaseTypes;

namespace Core.Combat.Scripts.Effects.Interfaces
{
    public interface ISerializeStatus
    {
        IBaseStatusScript Status { get; }
    }
}