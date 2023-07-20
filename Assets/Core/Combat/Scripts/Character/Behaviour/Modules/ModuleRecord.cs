using System.Text;

namespace Core.Combat.Scripts.Behaviour.Modules
{
	public abstract record ModuleRecord
	{
		public abstract bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters);
	}
}