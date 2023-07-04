using Core.Combat.Scripts;
using Core.World_Map.Scripts;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Main_Database.Combat
{
    [CreateAssetMenu]
    public sealed class MonsterTeam : SerializedScriptableObject
    {
        [OdinSerialize] 
        public readonly BothWays Location;
        
        [OdinSerialize, Required]
        public readonly NonGirlScript[] Monsters;
        
        [OdinSerialize]
        public readonly float Threat;

        public void Deconstruct(out NonGirlScript[] monsters, out float threat)
        {
            monsters = Monsters;
            threat = Threat;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Monsters == null)
                return;

            int sizeCount = 0;
            foreach (NonGirlScript monsterScript in Monsters) 
                sizeCount += monsterScript.Size;

            if (sizeCount > 4)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(assetObject: this);
                Debug.Log($"Monster Team is greater than allowed size at: {path}");
            }
        }
#endif
    }
}