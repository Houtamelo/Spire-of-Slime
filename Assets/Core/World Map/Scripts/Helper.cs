using JetBrains.Annotations;

namespace Core.World_Map.Scripts
{
    public static class Helper
    {
        [NotNull]
        public static string GetTitle(this LocationEnum location) => location.ToString();
    }
}