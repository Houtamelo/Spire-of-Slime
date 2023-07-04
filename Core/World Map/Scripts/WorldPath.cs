using Core.Audio.Scripts.MusicControllers;
using Core.Local_Map.Scripts.PathCreating;
using Main_Database.Visual_Novel;
using Sirenix.OdinInspector;
using UnityEngine;
using Save = Save_Management.Save;

namespace Core.World_Map.Scripts
{
    [CreateAssetMenu(menuName = "Database/World Path", fileName = "world_path-")]
    public class WorldPath : SerializedScriptableObject
    {
        [SerializeField]
        public LocationEnum origin;

        [SerializeField]
        public LocationEnum destination;

        [SerializeField]
        private bool isBothWays;
        public bool IsBothWays => isBothWays;

        [SerializeField]
        public VariableRequirement[] requirements = new VariableRequirement[0];

        [SerializeField]
        public PathBetweenNodesGenerator[] nodes = new PathBetweenNodesGenerator[0];

        [SerializeField]
        private MusicController musicController;
        public MusicController MusicController => musicController;

        [SerializeField]
        public int priority;
        
        public bool AreRequirementsMet(Save save)
        {
            foreach (VariableRequirement variable in requirements)
                if (variable.Validate(save) == false)
                    return false;

            return true;
        }
    }
}