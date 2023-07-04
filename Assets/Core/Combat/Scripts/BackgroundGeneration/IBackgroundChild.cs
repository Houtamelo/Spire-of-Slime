using System.Text;

namespace Core.Combat.Scripts.BackgroundGeneration
{
    public interface IBackgroundChild
    {
        void Generate();
        void GenerateFromRecord(BackgroundChildRecord record);
        BackgroundChildRecord GetRecord();
        bool IsDataValid(BackgroundChildRecord data, StringBuilder errors);
    }
}