using Shouldly;
using Xunit;

namespace GraphQlClientGenerator.Test
{
    public class NamingHelperTest
    {
        [Theory]
        [InlineData("WARD_VS_VITAL_SIGNS", "WardVsVitalSigns")]
        [InlineData("Who am I?", "WhoAmI")]
        [InlineData("Hello|Who|Am|I?", "HelloWhoAmI")]
        [InlineData("Lorem ipsum dolor...", "LoremIpsumDolor")]
        [InlineData("CoolSP", "CoolSp")]
        [InlineData("AB9CD", "Ab9Cd")]
        [InlineData("CCCTrigger", "CccTrigger")]
        [InlineData("CIRC", "Circ")]
        [InlineData("ID_SOME", "IdSome")]
        [InlineData("ID_SomeOther", "IdSomeOther")]
        [InlineData("ID_SOMEOther", "IdSomeOther")]
        [InlineData("CCC_SOME_2Phases", "CccSome2Phases")]
        [InlineData("AlreadyGoodPascalCase", "AlreadyGoodPascalCase")]
        [InlineData("999 999 99 9 ", "999999999")]
        [InlineData(" 1 2 3 ", "123")]
        [InlineData("1 AB cd EFDDD 8", "1AbCdEfddd8")]
        [InlineData("INVALID VALUE AND _2THINGS", "InvalidValueAnd2Things")]
        [InlineData("_", "_")]
        [InlineData(" _ _ ? ", "__")]
        [InlineData(" _ _ ? x ", "X")]
        public void ToPascalCase(string text, string expectedText) => NamingHelper.ToPascalCase(text).ShouldBe(expectedText);

        [Fact]
        public void LowerFirst() => NamingHelper.LowerFirst("PropertyName").ShouldBe("propertyName");
    }
}