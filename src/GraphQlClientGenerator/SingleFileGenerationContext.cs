using System.IO;

namespace GraphQlClientGenerator
{
    public class SingleFileGenerationContext : GenerationContext
    {
        private bool _isNullableReferenceScopeEnabled;
        private int _enums;
        private int _directives;
        private int _queryBuilders;
        private int _dataClasses;

        public override TextWriter Writer { get; }

        public SingleFileGenerationContext(GraphQlSchema schema, TextWriter writer, GenerationOptions options = GenerationOptions.DataClasses | GenerationOptions.QueryBuilders)
            : base(schema, options) =>
            Writer = writer;

        public override void BeforeGeneration(GraphQlGeneratorConfiguration configuration)
        {
            _enums = _directives = _queryBuilders = _dataClasses = 0;
            base.BeforeGeneration(configuration);
        }

        public override void BeforeBaseClassGeneration()
        {
            Writer.WriteLine("#region base classes");
        }

        public override void AfterBaseClassGeneration()
        {
            Writer.WriteLine("#endregion");
            Writer.WriteLine();
        }

        public override void BeforeEnumsGeneration() => Writer.WriteLine("#region shared types");

        public override void BeforeEnumGeneration(string enumName)
        {
            if (_enums > 0)
                Writer.WriteLine();
        }

        public override void AfterEnumGeneration(string enumName) => _enums++;

        public override void AfterEnumsGeneration()
        {
            Writer.WriteLine("#endregion");
            Writer.WriteLine();
        }

        public override void BeforeDirectivesGeneration()
        {
            EnterNullableReferenceScope();
            Writer.WriteLine("#region directives");
        }

        public override void BeforeDirectiveGeneration(string className)
        {
            if (_directives > 0)
                Writer.WriteLine();
        }

        public override void AfterDirectiveGeneration(string className) => _directives++;

        public override void AfterDirectivesGeneration()
        {
            Writer.WriteLine("#endregion");
            Writer.WriteLine();
        }

        public override void BeforeQueryBuildersGeneration()
        {
            EnterNullableReferenceScope();
            Writer.WriteLine("#region builder classes");
        }

        public override void BeforeQueryBuilderGeneration(string className)
        {
            if (_queryBuilders > 0)
                Writer.WriteLine();
        }

        public override void AfterQueryBuilderGeneration(string className) => _queryBuilders++;

        public override void AfterQueryBuildersGeneration()
        {
            Writer.WriteLine("#endregion");
            Writer.WriteLine();
        }

        public override void BeforeInputClassesGeneration()
        {
            EnterNullableReferenceScope();
            Writer.WriteLine("#region input classes");
        }

        public override void AfterInputClassesGeneration()
        {
            Writer.WriteLine("#endregion");
            Writer.WriteLine();
        }

        public override void BeforeDataClassesGeneration()
        {
            _dataClasses = 0;
            EnterNullableReferenceScope();
            Writer.WriteLine("#region data classes");
        }

        public override void BeforeDataClassGeneration(string className)
        {
            if (_dataClasses > 0)
                Writer.WriteLine();
        }

        public override void AfterDataClassGeneration(string className) => _dataClasses++;

        public override void AfterDataClassesGeneration() => Writer.WriteLine("#endregion");

        public override void AfterGeneration() => ExitNullableReferenceScope();

        private void EnterNullableReferenceScope()
        {
            if (_isNullableReferenceScopeEnabled || Configuration.CSharpVersion != CSharpVersion.NewestWithNullableReferences)
                return;

            Writer.WriteLine("#nullable enable");
            _isNullableReferenceScopeEnabled = true;
        }

        private void ExitNullableReferenceScope()
        {
            if (!_isNullableReferenceScopeEnabled)
                return;

            Writer.WriteLine("#nullable restore");
            _isNullableReferenceScopeEnabled = false;
        }
    }
}