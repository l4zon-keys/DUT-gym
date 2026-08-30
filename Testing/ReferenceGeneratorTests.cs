using System.Text.RegularExpressions;
using LoginFormASPCore6.Services;

namespace Testing
{
    public class ReferenceGeneratorTests
    {
        [Fact]
        public void Generate_UsesPrefixDateAndSixHexChars()
        {
            var reference = ReferenceGenerator.Generate("PAY");
            var datePart = DateTime.UtcNow.ToString("yyMMdd");

            Assert.Matches(new Regex($"^PAY{datePart}[0-9a-f]{{6}}$"), reference);
        }

        [Fact]
        public void Generate_ProducesDifferentReferencesEachCall()
        {
            var first = ReferenceGenerator.Generate("PAY");
            var second = ReferenceGenerator.Generate("PAY");

            Assert.NotEqual(first, second);
        }
    }
}
