using Core.Combat.Scripts.Cues;
using Core.Localization.Scripts;
using Core.Main_Database.Audio;
using Core.Main_Database.Combat;
using Core.Main_Database.Local_Map;
using Core.Main_Database.Visual_Novel;
using Core.Main_Database.World_Map;
using Core.Visual_Novel.Scripts;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Core.Main_Database
{
    public sealed class DatabaseManager : SerializedScriptableObject
    {
        public static DatabaseManager Instance { get; private set; }

        [OdinSerialize, Required]
        public readonly TranslationDatabase TranslationDatabase;

        [OdinSerialize, Required]
        public readonly YarnDatabase YarnDatabase;

        [OdinSerialize, Required]
        public readonly StatusEffectsDatabase StatusEffectsDatabase;

        [OdinSerialize, Required]
        public readonly AudioPathsDatabase AudioPathsDatabase;

        [OdinSerialize, Required]
        public readonly WorldScenesDatabase WorldScenesDatabase;

        [OdinSerialize, Required]
        public readonly RestEventsDatabase RestEventsDatabase;

        [OdinSerialize, Required]
        public readonly BarkDatabase BarkDatabase;

        [OdinSerialize, Required]
        public readonly CgDatabase CgDatabase;

        [OdinSerialize, Required]
        public readonly PortraitDatabase PortraitDatabase;

        [OdinSerialize, Required]
        public readonly PerkDatabase PerkDatabase;

        [OdinSerialize, Required]
        public readonly MapEventDatabase MapEventDatabase;

        [OdinSerialize, Required]
        public readonly WorldPathDatabase WorldPathDatabase;

        [OdinSerialize, Required]
        public readonly TileInfoDatabase TileInfoDatabase;

        [OdinSerialize, Required]
        public readonly MonsterTeamDatabase MonsterTeamDatabase;

        [OdinSerialize, Required]
        public readonly PathDatabase PathDatabase;

        [OdinSerialize, Required]
        public readonly BackgroundDatabase BackgroundDatabase;

        [OdinSerialize, Required]
        public readonly CharacterDatabase CharacterDatabase;

        [OdinSerialize, Required]
        public readonly SkillDatabase SkillDatabase;

        [OdinSerialize, Required]
        public readonly CombatScriptDatabase CombatScriptsDatabase;
        
        [OdinSerialize, Required]
        public readonly VariableDatabase VariableDatabase;
        
        [OdinSerialize, Required]
        public readonly MusicDatabase MusicDatabase;

        public void SetReference()
        {
            Instance = this;
            TranslationDatabase.Initialize();
            WorldScenesDatabase.Initialize();
            MonsterTeamDatabase.Initialize();
            VariableDatabase.Initialize();
            WorldPathDatabase.Initialize();
            BackgroundDatabase.Initialize();
            MapEventDatabase.Initialize();
            SkillDatabase.Initialize();
            CharacterDatabase.Initialize();
            PerkDatabase.Initialize();
            TileInfoDatabase.Initialize();
            CombatScriptsDatabase.Initialize();
            MusicDatabase.Initialize();
        }
    }
}