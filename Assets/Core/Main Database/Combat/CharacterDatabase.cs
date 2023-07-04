using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Core.Combat.Scripts;
using Core.Main_Characters.Nema.Combat;
using Data.Main_Characters.Ethel;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

namespace Main_Database.Combat
{
    public sealed class CharacterDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private CharacterScriptable[] allCharacters;

        [SerializeField]
        private Ethel defaultEthel;
        public static Ethel DefaultEthel => Instance.CharacterDatabase.defaultEthel;

        [SerializeField]
        private Nema defaultNema;
        public static Nema DefaultNema => Instance.CharacterDatabase.defaultNema;

        private readonly Dictionary<CleanString, CharacterScriptable> _mappedCharacters = new();

        [Pure]
        public static Option<CharacterScriptable> GetCharacter(CleanString key) 
            => Instance.CharacterDatabase._mappedCharacters.TryGetValue(key, out CharacterScriptable character) ? character : Option<CharacterScriptable>.None;

        public void Initialize()
        {
            foreach (CharacterScriptable character in allCharacters)
                _mappedCharacters.Add(character.Key, character);
            
            _mappedCharacters[defaultEthel.Key] = defaultEthel;
            _mappedCharacters[defaultNema.Key] = defaultNema;
            _mappedCharacters.TrimExcess();
        }

#if UNITY_EDITOR  
        public void AssignData(IEnumerable<CharacterScriptable> characters)
        {
            allCharacters = characters.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}