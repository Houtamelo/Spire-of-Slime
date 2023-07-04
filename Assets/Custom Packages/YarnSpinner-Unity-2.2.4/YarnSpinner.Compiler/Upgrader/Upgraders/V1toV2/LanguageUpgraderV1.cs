using System.Linq;

namespace Yarn.Compiler.Upgrader
{
    public class LanguageUpgraderV1 : ILanguageUpgrader
    {
     
        public UpgradeResult Upgrade(UpgradeJob upgradeJob)
        {
            UpgradeResult[] results = new[] {
                                                new FormatFunctionUpgrader().Upgrade(upgradeJob),
                                                new VariableDeclarationUpgrader().Upgrade(upgradeJob),
                                                new OptionsUpgrader().Upgrade(upgradeJob),
                                            };

            return results.Aggregate((result, next) => UpgradeResult.Merge(result, next));
        }
    }
}
