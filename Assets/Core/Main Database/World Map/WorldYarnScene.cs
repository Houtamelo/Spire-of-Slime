using Core.World_Map.Scripts;
using Main_Database.Visual_Novel;
using UnityEngine;
using Save = Save_Management.Save;

namespace Main_Database.World_Map
{
    [CreateAssetMenu(fileName = "world-scene_", menuName = "Database/WorldMap/Yarn Scene")]
    public class WorldYarnScene : ScriptableObject
    {
        [SerializeField]
        private LocationEnum location;
        public LocationEnum Location => location;
        
        [SerializeField]
        private int priority;
        public int Priority => priority;

        [SerializeField]
        private VariableRequirement[] requirements;

        public string SceneName => name.Replace("world-scene_", string.Empty);

        public bool AreRequirementsMet(Save save)
        {
            foreach (VariableRequirement variable in requirements)
                if (variable.Validate(save) == false)
                    return false;

            return true;
        }
    }
}