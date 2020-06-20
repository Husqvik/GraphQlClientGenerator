using Shouldly;
using Xunit;

namespace GraphQlClientGenerator.Test
{
    public class NamingHelperTest
    {
        [Fact]
        public void ToPascalCase()
        {
            NamingHelper.ToPascalCase("WARD_VS_VITAL_SIGNS").ShouldBe("WardVsVitalSigns");
            NamingHelper.ToPascalCase("Who am I?").ShouldBe("WhoAmI");
            NamingHelper.ToPascalCase("Hello|Who|Am|I?").ShouldBe("HelloWhoAmI");
            NamingHelper.ToPascalCase("Lorem ipsum dolor...").ShouldBe("LoremIpsumDolor");
            NamingHelper.ToPascalCase("CoolSP").ShouldBe("CoolSp");
            NamingHelper.ToPascalCase("AB9CD").ShouldBe("Ab9Cd");
            NamingHelper.ToPascalCase("CCCTrigger").ShouldBe("CccTrigger");
            NamingHelper.ToPascalCase("CIRC").ShouldBe("Circ");
            NamingHelper.ToPascalCase("ID_SOME").ShouldBe("IdSome");
            NamingHelper.ToPascalCase("ID_SomeOther").ShouldBe("IdSomeOther");
            NamingHelper.ToPascalCase("ID_SOMEOther").ShouldBe("IdSomeOther");
            NamingHelper.ToPascalCase("CCC_SOME_2Phases").ShouldBe("CccSome2Phases");
            NamingHelper.ToPascalCase("AlreadyGoodPascalCase").ShouldBe("AlreadyGoodPascalCase");
            NamingHelper.ToPascalCase("999 999 99 9 ").ShouldBe("999999999");
            NamingHelper.ToPascalCase(" 1 2 3 ").ShouldBe("123");
            NamingHelper.ToPascalCase("1 AB cd EFDDD 8").ShouldBe("1AbCdEfddd8");
            NamingHelper.ToPascalCase("INVALID VALUE AND _2THINGS").ShouldBe("InvalidValueAnd2Things");
            NamingHelper.ToPascalCase("_").ShouldBe("_");
            NamingHelper.ToPascalCase(" _ _ ? ").ShouldBe("__");
            NamingHelper.ToPascalCase(" _ _ ? x ").ShouldBe("X");
        }

        [Fact]
        public void LowerFirst()
        {
            NamingHelper.LowerFirst("PropertyName").ShouldBe("propertyName");
        }
    }
}