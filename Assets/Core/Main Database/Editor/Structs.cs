using Core.Combat.Scripts.Barks;
using JetBrains.Annotations;

namespace Core.Main_Database.Editor
{
    public static partial class DataBuilder
    {
        [UsedImplicitly]
        private struct DescriptionData
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string FlavorText { get; set; }
        }

        [UsedImplicitly]
        private struct BarkData
        {
            public BarkType BarkType { get; set; }
            public string CharacterKeyOne { get; set; }
            public string BarkOne { get; set; }
            public string CharacterKeyTwo { get; set; }
            public string BarkTwo { get; set; }
        }
    }
}