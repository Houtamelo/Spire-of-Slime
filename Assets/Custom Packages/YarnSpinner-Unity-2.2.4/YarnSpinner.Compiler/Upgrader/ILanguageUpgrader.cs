namespace Yarn.Compiler.Upgrader
{
    public interface ILanguageUpgrader
    {
        UpgradeResult Upgrade(UpgradeJob upgradeJob);
    }
}
