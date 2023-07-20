using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Core.World_Map.Scripts
{
    [Serializable]
    public enum LocationEnum
    {
        Chapel = 0,
        BellPlantGrove = 1,
    }

    public static class LocationHelper
    {
        private static readonly IReadOnlyDictionary<LocationEnum, string> LocationNames;

        static LocationHelper()
        {
            LocationNames = Enum.GetValues(enumType: typeof(LocationEnum)).Cast<LocationEnum>().ToDictionary(keySelector: e => e, elementSelector: e => Regex.Replace(input: e.ToString(), pattern: "[A-Z]", replacement: " $0").Trim());
        }

        public static string FormattedName(this LocationEnum locationEnum) => LocationNames[key: locationEnum];

        [NotNull]
        public static string Description(this LocationEnum locationEnum)
        {
            return locationEnum switch
            {
                LocationEnum.Chapel => "A small church in the middle of nowhere",
                LocationEnum.BellPlantGrove => "A quiet grove",
                //LocationEnum.BellPlantForest => "A thin forest",
                //LocationEnum.CrabdraCave => "A seemingly deserted cave",
                _ => ""
            };
        }
    }
}