using Core.Combat.Scripts.Behaviour;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Managers.Enumerators
{
    
	public ref struct DirectCharacterEnumerator
	{
		private readonly IndexableHashSet<CharacterStateMachine> _leftCharacters;
		private readonly IndexableHashSet<CharacterStateMachine> _rightCharacters;
        
		public CharacterStateMachine Current { get; private set; }
		private int _leftIndex;
		private int _rightIndex;
        
		public DirectCharacterEnumerator(IndexableHashSet<CharacterStateMachine> leftCharacters, IndexableHashSet<CharacterStateMachine> rightCharacters)
		{
			_leftCharacters = leftCharacters;
			_rightCharacters = rightCharacters;
            
			_leftIndex = 0;
			_rightIndex = 0;
            
			Current = null;
		}
        
		public bool MoveNext()
		{
			if (_leftIndex < _leftCharacters.Count)
			{
				Current = _leftCharacters[_leftIndex];
				_leftIndex++;
				return true;
			}
            
			if (_rightIndex < _rightCharacters.Count)
			{
				Current = _rightCharacters[_rightIndex];
				_rightIndex++;
				return true;
			}
            
			return false;
		}
        
		public DirectCharacterEnumerator GetEnumerator() => this;
        
		public void Reset()
		{
			_leftIndex = 0;
			_rightIndex = 0;
		}
	}
}