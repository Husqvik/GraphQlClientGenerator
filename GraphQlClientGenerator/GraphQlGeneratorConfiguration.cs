namespace GraphQlClientGenerator
{
    public static class GraphQlGeneratorConfiguration
    {
        public static CSharpVersion CSharpVersion { get; set; }

        public static string ClassPostfix { get; set; }
    }

    public enum CSharpVersion
    {
        Compatible,
        Newest
    }
}