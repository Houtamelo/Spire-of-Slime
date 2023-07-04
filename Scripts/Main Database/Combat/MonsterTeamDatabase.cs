using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using Core.Combat.Scripts;
using Core.World_Map.Scripts;
using ListPool;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;

namespace Main_Database.Combat
{
    public sealed class MonsterTeamDatabase : SerializedScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;

        [SerializeField, Required]
        private MonsterTeam[] monsterTeams;

        private Dictionary<BothWays, MonsterTeam[]> _mappedTeams;

        [Pure]
        public static Option<CharacterScriptable[]> GetEnemyTeam(in BothWays tileIdentifier, float multiplier)
        {
            MonsterTeamDatabase database = Instance.MonsterTeamDatabase;
            if (database._mappedTeams.TryGetValue(tileIdentifier, out MonsterTeam[] possibilities) == false)
                return Option<CharacterScriptable[]>.None;

            ValueListPool<(CharacterScriptable[], float)> enemyWeights = new();
            foreach ((CharacterScriptable[] monsterTeam, float threat) in possibilities)
            {
                float distance = Mathf.Abs(multiplier - threat);
                if (distance > 0.5f)
                    continue;

                distance = Mathf.SmoothStep(distance, 0f, 0.5f);
                float weight = 1f - distance;
                if (weight <= 0f)
                    continue;

                enemyWeights.Add((monsterTeam, weight * weight));
            }

            if (enemyWeights.Count == 0)
            {
                Debug.LogWarning($"No enemies within desired threat : {multiplier.ToString(provider: CultureInfo.InvariantCulture)}, returning empty enemy team.", database);
                return Option<CharacterScriptable[]>.None;
            }

            return Option<CharacterScriptable[]>.Some(enemyWeights.GetWeightedRandom());
        }

        public void Initialize()
        {
            Dictionary<BothWays, List<MonsterTeam>> dictionary = new();
            foreach (MonsterTeam monsterTeam in monsterTeams)
            {
                if (dictionary.TryGetValue(monsterTeam.Location, out List<MonsterTeam> list) == false)
                {
                    list = new List<MonsterTeam>();
                    dictionary.Add(monsterTeam.Location, list);
                }

                list.Add(monsterTeam);
            }
            
            _mappedTeams = dictionary.ToDictionary(pair => pair.Key, kvp => kvp.Value.ToArray());
        }

#if UNITY_EDITOR        
        public void AssignData(IEnumerable<MonsterTeam> monsters)
        {
            monsterTeams = monsters.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}