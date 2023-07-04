/*using Unity.Barracuda;

namespace Combat.AI.Behaviour
{
    public class DeterministicModelOutput
    {
        public readonly int _action;
        
        private DeterministicModelOutput(int action)
        {
            _action = action;
        }

        public static Utility.Option<DeterministicModelOutput> FromTensor(Tensor output)
        {
            if (output == null)
                return Utility.Option<DeterministicModelOutput>.None;
            
            int action = (int) output[0, 0, 0, 0];

            return Utility.Option<DeterministicModelOutput>.Some(new DeterministicModelOutput(action));
        }
    }
}*/