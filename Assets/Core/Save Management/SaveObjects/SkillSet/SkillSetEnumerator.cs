using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Save_Management.SaveObjects
{
    public ref struct SkillSetEnumerator
    {
        private readonly IReadOnlySkillSet _skillSet;
        private int _index;
        public CleanString Current { get; private set; }

        public SkillSetEnumerator(IReadOnlySkillSet skillSet)
        {
            _skillSet = skillSet;
            _index = -1;
            Current = string.Empty;
        }

        public bool MoveNext()
        {
            _index++;
            if (_index > 3)
                return false;

            Current = _skillSet.Get(_index);
            return Current.IsSome() || MoveNext();
        }

        public void Reset() => _index = -1;

        public void Dispose() => Current = string.Empty;
    }
    
    public ref struct SkillSetStructEnumerator
    {
        private readonly ReadOnlySkillSet _skillSet;
        private int _index;
        public CleanString Current { get; private set; }

        public SkillSetStructEnumerator(ReadOnlySkillSet skillSet)
        {
            _skillSet = skillSet;
            _index = -1;
            Current = string.Empty;
        }

        public bool MoveNext()
        {
            while (true)
            {
                _index++;
                if (_index > 3)
                    return false;

                Option<CleanString> option = _skillSet[_index];
                if (option.IsNone)
                    continue;

                Current = option.Value;
                return true;
            }
        }

        public void Reset() => _index = -1;

        public void Dispose() => Current = string.Empty;
    }
}