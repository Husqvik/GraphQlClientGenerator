namespace GraphQlClientGenerator
{
    public static class GraphQlGeneratorConfiguration
    {
        public static CSharpVersion CSharpVersion { get; set; }

        public static string ClassPostfix { get; set; }

        public static void Reset()
        {
            ClassPostfix = null;
            CSharpVersion = CSharpVersion.Compatible;
        }
    }

    public enum CSharpVersion
    {
        Compatible,
        Newest
    }
}