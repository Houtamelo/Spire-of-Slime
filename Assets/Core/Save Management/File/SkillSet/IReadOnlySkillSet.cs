namespace Save_Management
{
    public interface IReadOnlySkillSet
    {
        CleanString One { get; }
        CleanString Two { get; }
        CleanString Three { get; }
        CleanString Four { get; }
        CleanString Get(int index);
        SkillSetEnumerator GetEnumerator();
    }
}