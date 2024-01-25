using System.Runtime.CompilerServices;

namespace GraphQlClientGenerator.Test;

public static class GlobalFixture
{
    [ModuleInitializer]
    public static void Initialize()
    {
        UseProjectRelativeDirectory("VerifierExpectations");
    }
}