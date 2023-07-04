/*using System;
using Core.World_Map.Scripts;
using Main_Database.Visual_Novel;
using Main_Database.Visual_Novel.Enums;
using Save_Management;

namespace Main_Database
{
    public static class DatabaseHelper
    {
        

        public static string AsSymbol(this ComparisonType comparisonType)
        {
            return comparisonType switch
            {
                ComparisonType.Equal => "=",
                ComparisonType.Greater => ">",
                ComparisonType.GreaterOrEqual => ">=",
                ComparisonType.Less => "<",
                ComparisonType.LessOrEqual => "<=",
                ComparisonType.NotEqual => "!=",
                _ => throw new ArgumentOutOfRangeException(nameof(comparisonType), comparisonType, null)
            };
        }
    }
}*/