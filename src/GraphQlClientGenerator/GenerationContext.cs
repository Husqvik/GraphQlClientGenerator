using System;
using System.IO;

namespace GraphQlClientGenerator
{
    [Flags]
    public enum GenerationOptions
    {
        QueryBuilders = 1,
        DataClasses = 2
    }

    public abstract class GenerationContext
    {
        public GraphQlSchema Schema { get; }

        public GenerationOptions Options { get; }

        public abstract TextWriter Writer { get; }

        protected GraphQlGeneratorConfiguration Configuration { get; private set; }

        protected GenerationContext(GraphQlSchema schema, GenerationOptions options)
        {
            var optionsInteger = (int)options;
            if (optionsInteger != 1 && optionsInteger != 2 && optionsInteger != 3)
                throw new ArgumentException("invalid value", nameof(options));

            Schema = schema;
            Options = options;
        }

        public virtual void BeforeGeneration(GraphQlGeneratorConfiguration configuration) => Configuration = configuration;

        public abstract void BeforeBaseClassGeneration();

        public abstract void AfterBaseClassGeneration();

        public abstract void BeforeEnumsGeneration();

        public abstract void BeforeEnumGeneration(string enumName);

        public abstract void AfterEnumGeneration(string enumName);

        public abstract void AfterEnumsGeneration();

        public abstract void BeforeDirectivesGeneration();

        public abstract void BeforeDirectiveGeneration(string className);

        public abstract void AfterDirectiveGeneration(string className);

        public abstract void AfterDirectivesGeneration();

        public abstract void BeforeQueryBuildersGeneration();

        public abstract void BeforeQueryBuilderGeneration(string className);

        public abstract void AfterQueryBuilderGeneration(string className);

        public abstract void AfterQueryBuildersGeneration();

        public abstract void BeforeInputClassesGeneration();

        public abstract void AfterInputClassesGeneration();

        public abstract void BeforeDataClassesGeneration();

        public abstract void BeforeDataClassGeneration(string className);

        public abstract void AfterDataClassGeneration(string className);

        public abstract void AfterDataClassesGeneration();

        public abstract void AfterGeneration();
    }
}