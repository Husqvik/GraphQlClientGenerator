﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace GraphQlClientGenerator.Test
{
    public class GraphQlGeneratorTest
    {
        private static readonly GraphQlSchema TestSchema = DeserializeTestSchema("TestSchema");

        private readonly ITestOutputHelper _outputHelper;

        private static GraphQlSchema DeserializeTestSchema(string resourceName) =>
            GraphQlGenerator.DeserializeGraphQlSchema(GetTestResource(resourceName));

        private static GenerationContext CreateGenerationContext(
            StringBuilder builder,
            GraphQlSchema schema,
            GeneratedObjectType options = GeneratedObjectType.QueryBuilders | GeneratedObjectType.DataClasses) =>
            new SingleFileGenerationContext(schema, new StringWriter(builder), options);

        public GraphQlGeneratorTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void MultipleFileGeneration()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    CommentGeneration = CommentGenerationOption.CodeSummary | CommentGenerationOption.DescriptionAttribute,
                    CSharpVersion = CSharpVersion.Newest
                };

            var generator = new GraphQlGenerator(configuration);

            var tempPath = Path.GetTempPath();
            var directoryInfo = Directory.CreateDirectory(Path.Combine(tempPath, "GraphQlGeneratorTest"));

            try
            {
                var context = new MultipleFileGenerationContext(DeserializeTestSchema("TestSchema2"), directoryInfo.FullName, "GraphQlGeneratorTest", "GraphQlGeneratorTest.csproj");
                generator.Generate(context);

                var files = directoryInfo.GetFiles().OrderBy(f => f.Name).ToArray();
                var fileNames = files.Select(f => f.Name);
                fileNames.ShouldBe(
                    new[]
                    {
                        "About.cs",
                        "AboutItem.cs",
                        "AboutItemQueryBuilder.cs",
                        "AboutQueryBuilder.cs",
                        "Address.cs",
                        "AddressQueryBuilder.cs",
                        "AppState.cs",
                        "AppStateFronScreen.cs",
                        "AppStateFronScreenMutation.cs",
                        "AppStateFronScreenQueryBuilder.cs",
                        "AppStateJourney.cs",
                        "AppStateJourneyMutation.cs",
                        "AppStateJourneyQueryBuilder.cs",
                        "AppStateMutation.cs",
                        "AppStateQueryBuilder.cs",
                        "Avatar.cs",
                        "AwayMode.cs",
                        "AwayModeQueryBuilder.cs",
                        "AwayModeSettings.cs",
                        "AwayModeSettingsQueryBuilder.cs",
                        "BaseClasses.cs",
                        "Comparison.cs",
                        "ComparisonData.cs",
                        "ComparisonDataQueryBuilder.cs",
                        "ComparisonQueryBuilder.cs",
                        "Consumption.cs",
                        "ConsumptionMonth.cs",
                        "ConsumptionMonthQueryBuilder.cs",
                        "ConsumptionQueryBuilder.cs",
                        "CreditCard.cs",
                        "CreditCardQueryBuilder.cs",
                        "DayNightSchedule.cs",
                        "DayNightScheduleQueryBuilder.cs",
                        "DayNightScheduleSettings.cs",
                        "DayNightScheduleSettingsQueryBuilder.cs",
                        "Disaggregation.cs",
                        "DisaggregationQueryBuilder.cs",
                        "EnergyDeal.cs",
                        "EnergyDealQueryBuilder.cs",
                        "Feed.cs",
                        "FeedItem.cs",
                        "FeedItemQueryBuilder.cs",
                        "FeedQueryBuilder.cs",
                        "GqlMutationError.cs",
                        "GqlMutationErrorQueryBuilder.cs",
                        "GqlMutationGeneralResponse.cs",
                        "GqlMutationGeneralResponseQueryBuilder.cs",
                        "GraphQlGeneratorTest.csproj",
                        "GraphQlTypes.cs",
                        "Greeting.cs",
                        "GreetingQueryBuilder.cs",
                        "Home.cs",
                        "HomeMutation.cs",
                        "HomeMutationQueryBuilder.cs",
                        "HomeProfileQuestion.cs",
                        "HomeProfileQuestionAnswer.cs",
                        "HomeProfileQuestionInput.cs",
                        "HomeProfileQuestionInputQueryBuilder.cs",
                        "HomeProfileQuestionQueryBuilder.cs",
                        "HomeQueryBuilder.cs",
                        "IncludeDirective.cs",
                        "Invoice.cs",
                        "InvoicePayment.cs",
                        "InvoicePaymentQueryBuilder.cs",
                        "InvoiceQueryBuilder.cs",
                        "InvoiceSection.cs",
                        "InvoiceSectionQueryBuilder.cs",
                        "Me.cs",
                        "MeMutation.cs",
                        "MeMutationQueryBuilder.cs",
                        "MeQueryBuilder.cs",
                        "Mutation.cs",
                        "MutationQueryBuilder.cs",
                        "PairableDevice.cs",
                        "PairableDeviceOAuth.cs",
                        "PairableDeviceOAuthQueryBuilder.cs",
                        "PairableDeviceQueryBuilder.cs",
                        "PairDeviceResult.cs",
                        "PairDeviceResultQueryBuilder.cs",
                        "PaymentMethod.cs",
                        "PaymentMethodQueryBuilder.cs",
                        "PreLiveComparison.cs",
                        "PreLiveComparisonQueryBuilder.cs",
                        "PriceRating.cs",
                        "PriceRatingColorOffset.cs",
                        "PriceRatingColorOffsetQueryBuilder.cs",
                        "PriceRatingEntry.cs",
                        "PriceRatingEntryQueryBuilder.cs",
                        "PriceRatingQueryBuilder.cs",
                        "PriceRatingRoot.cs",
                        "PriceRatingRootQueryBuilder.cs",
                        "ProcessStep.cs",
                        "ProcessStepQueryBuilder.cs",
                        "Producer.cs",
                        "ProducerBullet.cs",
                        "ProducerBulletQueryBuilder.cs",
                        "ProducerQueryBuilder.cs",
                        "Production.cs",
                        "ProductionMonth.cs",
                        "ProductionMonthQueryBuilder.cs",
                        "ProductionQueryBuilder.cs",
                        "ProductionValue.cs",
                        "ProductionValueQueryBuilder.cs",
                        "PushNotification.cs",
                        "PushNotificationQueryBuilder.cs",
                        "Query.cs",
                        "QueryQueryBuilder.cs",
                        "Report.cs",
                        "ReportCell.cs",
                        "ReportCellQueryBuilder.cs",
                        "ReportQueryBuilder.cs",
                        "ReportRoot.cs",
                        "ReportRootQueryBuilder.cs",
                        "Resolution.cs",
                        "Sensor.cs",
                        "SensorHistory.cs",
                        "SensorHistoryQueryBuilder.cs",
                        "SensorHistoryValue.cs",
                        "SensorHistoryValueQueryBuilder.cs",
                        "SensorQueryBuilder.cs",
                        "SignupStatus.cs",
                        "SignupStatusQueryBuilder.cs",
                        "SkipDirective.cs",
                        "Subscription.cs",
                        "SubscriptionQueryBuilder.cs",
                        "Thermostat.cs",
                        "ThermostatCapability.cs",
                        "ThermostatCapabilityQueryBuilder.cs",
                        "ThermostatMeasurement.cs",
                        "ThermostatMeasurementQueryBuilder.cs",
                        "ThermostatMeasurements.cs",
                        "ThermostatMeasurementsQueryBuilder.cs",
                        "ThermostatMode.cs",
                        "ThermostatModeQueryBuilder.cs",
                        "ThermostatMutation.cs",
                        "ThermostatMutationQueryBuilder.cs",
                        "ThermostatQueryBuilder.cs",
                        "ThermostatState.cs",
                        "ThermostatStateQueryBuilder.cs",
                        "Wallet.cs",
                        "WalletQueryBuilder.cs",
                        "Weather.cs",
                        "WeatherEntry.cs",
                        "WeatherEntryQueryBuilder.cs",
                        "WeatherQueryBuilder.cs"
                    });

                var fileSizes = files.Where(f => f.Name != "BaseClasses.cs").Select(f => f.Length);
                fileSizes.ShouldBe(
                    new long[]
                    {
                        370, 399, 1295, 1082, 902, 4131, 443, 493, 2056, 1831, 379, 1038, 1076, 1609, 1675, 680, 418, 1507, 422, 1334, 716, 415, 1365, 4006, 887, 686, 3557, 4906, 402, 1312, 490, 2119, 537, 2296, 1149, 6812, 371, 1153, 495, 600, 2706, 2476, 413, 1389, 385, 1215, 368, 5985, 518, 2132, 1881, 845, 7805, 803, 1454, 417, 1433, 4626, 17076, 731, 1550, 551, 2587, 9743, 896, 5100, 1025, 476, 3221, 7042, 358, 1339, 467, 425, 1445, 1890, 499, 2175, 455, 1733, 545, 2501, 691, 495, 1921, 505, 2085, 3516, 679, 3759, 512, 2080, 600, 461, 1749, 2837, 996, 719, 3864, 5591, 822, 4380, 444, 1682, 347, 1283, 588, 675, 3451, 2781, 374, 1105, 404, 513, 483, 1902, 472, 1765, 2209, 483, 2143, 706, 777, 4324, 862, 454, 1756, 796, 4433, 508, 2005, 425, 1388, 413, 2590, 4949, 510, 2124, 482, 1874, 485, 1154, 3506, 1874
                    });

                var expectedOutput = GetTestResource("ExpectedMultipleFilesContext.Avatar");
                File.ReadAllText(Path.Combine(directoryInfo.FullName, "Avatar.cs")).ShouldBe(expectedOutput);
                expectedOutput = GetTestResource("ExpectedMultipleFilesContext.Home");
                File.ReadAllText(Path.Combine(directoryInfo.FullName, "Home.cs")).ShouldBe(expectedOutput);
                expectedOutput = GetTestResource("ExpectedMultipleFilesContext.IncludeDirective");
                File.ReadAllText(Path.Combine(directoryInfo.FullName, "IncludeDirective.cs")).ShouldBe(expectedOutput);
                expectedOutput = GetTestResource("ExpectedMultipleFilesContext.MutationQueryBuilder");
                File.ReadAllText(Path.Combine(directoryInfo.FullName, "MutationQueryBuilder.cs")).ShouldBe(expectedOutput);
            }
            finally
            {
                Directory.Delete(directoryInfo.FullName, true);
            }
        }

        [Fact]
        public void GenerateFullClientCSharpFile()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    CommentGeneration = CommentGenerationOption.CodeSummary | CommentGenerationOption.DescriptionAttribute
                };

            var generator = new GraphQlGenerator(configuration);
            var generatedSourceCode = generator.GenerateFullClientCSharpFile(TestSchema, "GraphQlGenerator.Test");
            var expectedOutput = GetTestResource("ExpectedFullClientCSharpFile");
            generatedSourceCode.ShouldBe(expectedOutput);
        }

        [Fact]
        public void GenerateQueryBuilder()
        {
            var configuration = new GraphQlGeneratorConfiguration();
            configuration.CustomClassNameMapping.Add("AwayMode", "VacationMode");

            var stringBuilder = new StringBuilder();
            new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, TestSchema, GeneratedObjectType.QueryBuilders));

            var expectedQueryBuilders = GetTestResource("ExpectedQueryBuilders");
            var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
            generatedSourceCode.ShouldBe(expectedQueryBuilders);
        }

        [Fact]
        public void GenerateDataClasses()
        {
            var configuration = new GraphQlGeneratorConfiguration();
            configuration.CustomClassNameMapping.Add("AwayMode", "VacationMode");

            var stringBuilder = new StringBuilder();
            new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, TestSchema, GeneratedObjectType.DataClasses));
            var expectedDataClasses = GetTestResource("ExpectedDataClasses");
            stringBuilder.ToString().ShouldBe(expectedDataClasses);
        }

        [Fact]
        public void GenerateDataClassesWithTypeConfiguration()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    IntegerTypeMapping = IntegerTypeMapping.Int64,
                    FloatTypeMapping = FloatTypeMapping.Double,
                    BooleanTypeMapping = BooleanTypeMapping.Custom,
                    IdTypeMapping = IdTypeMapping.String,
                    GeneratePartialClasses = false,
                    PropertyGeneration = PropertyGenerationOption.BackingField
                };

            configuration.ScalarFieldTypeMappingProvider = new TestCustomBooleanTypeMappingProvider();

            var stringBuilder = new StringBuilder();
            new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, TestSchema, GeneratedObjectType.DataClasses));
            var expectedDataClasses = GetTestResource("ExpectedDataClassesWithTypeConfiguration");
            stringBuilder.ToString().ShouldBe(expectedDataClasses);
        }

        private class TestCustomBooleanTypeMappingProvider : IScalarFieldTypeMappingProvider
        {
            public ScalarFieldTypeDescription GetCustomScalarFieldType(GraphQlGeneratorConfiguration configuration, GraphQlType baseType, GraphQlTypeBase valueType, string valueName) =>
                valueType.Name == "Boolean"
                    ? new ScalarFieldTypeDescription { NetTypeName = "bool" }
                    : DefaultScalarFieldTypeMappingProvider.Instance.GetCustomScalarFieldType(configuration, baseType, valueType, valueName);
        }

        [Fact]
        public void GenerateFormatMasks()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    IdTypeMapping = IdTypeMapping.Custom,
                    ScalarFieldTypeMappingProvider = TestFormatMaskScalarFieldTypeMappingProvider.Instance
                };

            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator(configuration);
            var schema = DeserializeTestSchema("TestSchema3");
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            var expectedDataClasses = GetTestResource("ExpectedFormatMasks");
            var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
            generatedSourceCode.ShouldBe(expectedDataClasses);
        }

        private class TestFormatMaskScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
        {
            public static readonly TestFormatMaskScalarFieldTypeMappingProvider Instance = new();

            public ScalarFieldTypeDescription GetCustomScalarFieldType(GraphQlGeneratorConfiguration configuration, GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
            {
                var isNotNull = valueType.Kind == GraphQlTypeKind.NonNull;
                var unwrappedType = valueType is GraphQlFieldType fieldType ? fieldType.UnwrapIfNonNull() : valueType;
                var nullablePostfix = isNotNull ? null : "?";

                if (unwrappedType.Name == "ID")
                    return new ScalarFieldTypeDescription { NetTypeName = "Guid" + nullablePostfix, FormatMask = "N" };

                if (valueName == "before" || valueName == "after" || unwrappedType.Name == "DateTimeOffset")
                    return new ScalarFieldTypeDescription { NetTypeName = "DateTimeOffset" + nullablePostfix, FormatMask = "yyyy-MM-dd\"T\"HH:mm" };

                return DefaultScalarFieldTypeMappingProvider.Instance.GetCustomScalarFieldType(configuration, baseType, valueType, valueName);
            }
        }

        [Fact]
        public void NewCSharpSyntaxWithClassPrefixAndSuffix()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    CSharpVersion = CSharpVersion.Newest,
                    ClassPrefix = "Test",
                    ClassSuffix = "V1",
                    MemberAccessibility = MemberAccessibility.Internal
                };

            var schema = DeserializeTestSchema("TestSchema2");

            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator(configuration);
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
            var expectedOutput = GetTestResource("ExpectedNewCSharpSyntaxWithClassPrefixAndSuffix");
            generatedSourceCode.ShouldBe(expectedOutput);

            CompileIntoAssembly(stringBuilder.ToString(), "GraphQLTestAssembly");

            Type.GetType("GraphQLTestAssembly.GraphQlQueryBuilder, GraphQLTestAssembly").ShouldNotBeNull();
        }

        [Fact]
        public void WithNullableReferences()
        {
            var configuration = new GraphQlGeneratorConfiguration { CSharpVersion = CSharpVersion.NewestWithNullableReferences };
            var schema = DeserializeTestSchema("TestSchema2");

            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator(configuration);
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            var expectedOutput = GetTestResource("ExpectedWithNullableReferences");
            var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
            generatedSourceCode.ShouldBe(expectedOutput);
        }

        [Fact]
        public void WithUnions()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    CSharpVersion = CSharpVersion.NewestWithNullableReferences,
                    JsonPropertyGeneration = JsonPropertyGenerationOption.UseDefaultAlias,
                    ScalarFieldTypeMappingProvider = TestFormatMaskScalarFieldTypeMappingProvider.Instance
                };

            var schema = DeserializeTestSchema("TestSchemaWithUnions");

            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator(configuration);
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            var expectedOutput = GetTestResource("ExpectedWithUnions");
            var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
            generatedSourceCode.ShouldBe(expectedOutput);
        }

        [Fact]
        public void WithNoFields()
        {
            var configuration = new GraphQlGeneratorConfiguration();

            var schema = DeserializeTestSchema("NoFieldsSchema");

            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator(configuration);
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
            var expectedOutput = GetTestResource("ExpectedNoFields");
            generatedSourceCode.ShouldBe(expectedOutput);
        }

        [Fact]
        public void GeneratedQuery()
        {
            var configuration = new GraphQlGeneratorConfiguration { JsonPropertyGeneration = JsonPropertyGenerationOption.Always };

            var schema = DeserializeTestSchema("TestSchema2");
            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator(configuration);
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(
                @"public class TestQueryBuilder : GraphQlQueryBuilder<TestQueryBuilder>
{
    private static readonly FieldMetadata[] AllFieldMetadata =
        new []
        {
            new FieldMetadata { Name = ""testField"" },
            new FieldMetadata { Name = ""objectParameter"" }
        };

    protected override string TypeName { get; } = ""Test"";

    public override IReadOnlyList<FieldMetadata> AllFields { get; } = AllFieldMetadata;

	public TestQueryBuilder WithTestField(
        QueryBuilderParameter<short?> valueInt16 = null,
        QueryBuilderParameter<ushort?> valueUInt16 = null,
        QueryBuilderParameter<byte?> valueByte = null,
        QueryBuilderParameter<int?> valueInt32 = null,
        QueryBuilderParameter<uint?> valueUInt32 = null,
        QueryBuilderParameter<long?> valueInt64 = null,
        QueryBuilderParameter<ulong?> valueUInt64 = null,
        QueryBuilderParameter<float?> valueSingle = null,
        QueryBuilderParameter<double?> valueDouble = null,
        QueryBuilderParameter<decimal?> valueDecimal = null,
        QueryBuilderParameter<DateTime?> valueDateTime = null,
        QueryBuilderParameter<DateTimeOffset?> valueDateTimeOffset = null,
        QueryBuilderParameter<Guid?> valueGuid = null,
        QueryBuilderParameter<string> valueString = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		if (valueInt16 != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueInt16"", ArgumentValue = valueInt16 });

		if (valueUInt16 != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueUInt16"", ArgumentValue = valueUInt16 });

		if (valueByte != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueByte"", ArgumentValue = valueByte });

		if (valueInt32 != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueInt32"", ArgumentValue = valueInt32 });

		if (valueUInt32 != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueUInt32"", ArgumentValue = valueUInt32 });

		if (valueInt64 != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueInt64"", ArgumentValue = valueInt64 });

		if (valueUInt64 != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueUInt64"", ArgumentValue = valueUInt64 });

		if (valueSingle != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueSingle"", ArgumentValue = valueSingle });

		if (valueDouble != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueDouble"", ArgumentValue = valueDouble });

		if (valueDecimal != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueDecimal"", ArgumentValue = valueDecimal });

		if (valueDateTime != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueDateTime"", ArgumentValue = valueDateTime, FormatMask = ""yy-MM-dd HH:mmZ"" });

		if (valueDateTimeOffset != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueDateTimeOffset"", ArgumentValue = valueDateTimeOffset });

		if (valueGuid != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueGuid"", ArgumentValue = valueGuid });

		if (valueString != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""valueString"", ArgumentValue = valueString });

		return WithScalarField(""testField"", null, null, args);
	}

    public TestQueryBuilder WithObjectParameterField(QueryBuilderParameter<object> objectParameter = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		if (objectParameter != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""objectParameter"", ArgumentValue = objectParameter });

        return WithScalarField(""objectParameter"", ""fieldAlias"", new GraphQlDirective[] { new IncludeDirective(new GraphQlQueryParameter<bool>(""direct"", ""Boolean"", true)), new SkipDirective((QueryBuilderParameter<bool>)false) }, args);
    }

    public TestQueryBuilder WithTestFragment(MeQueryBuilder queryBuilder) => WithFragment(queryBuilder, null);
}");

            const string assemblyName = "GeneratedQueryTestAssembly";
            CompileIntoAssembly(stringBuilder.ToString(), assemblyName);

            var builderType = Type.GetType($"{assemblyName}.TestQueryBuilder, {assemblyName}");
            builderType.ShouldNotBeNull();
            var formattingType = Type.GetType($"{assemblyName}.Formatting, {assemblyName}");
            formattingType.ShouldNotBeNull();

            var builderInstance = Activator.CreateInstance(builderType);
            builderType
                .GetMethod("WithTestField", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(
                    builderInstance,
                    new object[]
                    {
                        (short)1,
                        (ushort)2,
                        (byte)3,
                        4,
                        (uint)5,
                        6L,
                        (ulong)7,
                        8.123f,
                        9.456d,
                        10.789m,
                        new DateTime(2019, 6, 30, 0, 27, 47, DateTimeKind.Utc).AddTicks(1234567),
                        new DateTimeOffset(2019, 6, 30, 2, 27, 47, TimeSpan.FromHours(2)).AddTicks(1234567),
                        Guid.Empty,
                        "string value"
                    }.Select(p => CreateParameter(assemblyName, p)).ToArray());

            builderType
                .GetMethod("WithObjectParameterField", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(
                    builderInstance,
                    new object[]
                    {
                        new []
                        {
                            JsonConvert.DeserializeObject("{ \"rootProperty1\": \"root value 1\", \"rootProperty2\": 123.456, \"rootProperty3\": true, \"rootProperty4\": null, \"rootProperty5\": { \"nestedProperty\": 987 } }"),
                            JsonConvert.DeserializeObject("[{ \"rootProperty1\": \"root value 2\" }, { \"rootProperty1\": false }]")
                        }
                    }.Select(p => CreateParameter(assemblyName, p)).ToArray());

            var query =
                builderType
                    .GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(builderInstance, new [] { Enum.Parse(formattingType, "None"), (byte)2 });

            query.ShouldBe("{testField(valueInt16:1,valueUInt16:2,valueByte:3,valueInt32:4,valueUInt32:5,valueInt64:6,valueUInt64:7,valueSingle:8.123,valueDouble:9.456,valueDecimal:10.789,valueDateTime:\"19-06-30 00:27Z\",valueDateTimeOffset:\"2019-06-30T02:27:47.1234567+02:00\",valueGuid:\"00000000-0000-0000-0000-000000000000\",valueString:\"string value\"),fieldAlias:objectParameter(objectParameter:[{rootProperty1:\"root value 1\",rootProperty2:123.456,rootProperty3:true,rootProperty4:null,rootProperty5:{nestedProperty:987}},[{rootProperty1:\"root value 2\"},{rootProperty1:false}]])@include(if:$direct)@skip(if:false)}");
            query =
                builderType
                    .GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(builderInstance, new[] { Enum.Parse(formattingType, "Indented"), (byte)2 });

            query.ShouldBe($" {{{Environment.NewLine}  testField(valueInt16: 1, valueUInt16: 2, valueByte: 3, valueInt32: 4, valueUInt32: 5, valueInt64: 6, valueUInt64: 7, valueSingle: 8.123, valueDouble: 9.456, valueDecimal: 10.789, valueDateTime: \"19-06-30 00:27Z\", valueDateTimeOffset: \"2019-06-30T02:27:47.1234567+02:00\", valueGuid: \"00000000-0000-0000-0000-000000000000\", valueString: \"string value\"){Environment.NewLine}  fieldAlias: objectParameter(objectParameter: [{Environment.NewLine}    {{{Environment.NewLine}      rootProperty1: \"root value 1\",{Environment.NewLine}      rootProperty2: 123.456,{Environment.NewLine}      rootProperty3: true,{Environment.NewLine}      rootProperty4: null,{Environment.NewLine}      rootProperty5: {{{Environment.NewLine}        nestedProperty: 987}}}},{Environment.NewLine}    [{Environment.NewLine}    {{{Environment.NewLine}      rootProperty1: \"root value 2\"}},{Environment.NewLine}    {{{Environment.NewLine}      rootProperty1: false}}]]) @include(if: $direct) @skip(if: false){Environment.NewLine}}}");

            var rootQueryBuilderType = Type.GetType($"{assemblyName}.QueryQueryBuilder, {assemblyName}");
            rootQueryBuilderType.ShouldNotBeNull();
            var rootQueryBuilderInstance = rootQueryBuilderType.GetConstructor(new [] { typeof(string) }).Invoke(new object[1]);
            rootQueryBuilderType.GetMethod("WithAllFields", BindingFlags.Instance | BindingFlags.Public).Invoke(rootQueryBuilderInstance, null);
            rootQueryBuilderType
                .GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(rootQueryBuilderInstance, new[] { Enum.Parse(formattingType, "None"), (byte)2 });

            builderType
                .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(builderInstance, null);

            var meBuilderType = Type.GetType($"{assemblyName}.MeQueryBuilder, {assemblyName}");
            var childFragmentBuilderInstance = Activator.CreateInstance(meBuilderType);
            meBuilderType.GetMethod("WithAllScalarFields", BindingFlags.Instance | BindingFlags.Public).Invoke(childFragmentBuilderInstance, null);

            builderType
                .GetMethod("WithTestFragment", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(builderInstance, new [] { childFragmentBuilderInstance });

            query =
                builderType
                    .GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(builderInstance, new[] { Enum.Parse(formattingType, "None"), (byte)2 });

            query.ShouldBe("{...on Me{id,firstName,lastName,fullName,ssn,email,language,tone,mobile}}");
        }

        [Fact]
        public void DeprecatedAttributes()
        {
            var configuration =
                new GraphQlGeneratorConfiguration
                {
                    CSharpVersion = CSharpVersion.Newest,
                    CommentGeneration = CommentGenerationOption.CodeSummary | CommentGenerationOption.DescriptionAttribute,
                    IncludeDeprecatedFields = true,
                    GeneratePartialClasses = false
                };

            var schema = DeserializeTestSchema("TestSchemaWithDeprecatedFields");

            var stringBuilder = new StringBuilder();
            new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, schema, GeneratedObjectType.DataClasses));
            var expectedOutput = GetTestResource("ExpectedDeprecatedAttributes").Replace("\r", String.Empty);
            stringBuilder.ToString().Replace("\r", String.Empty).ShouldBe(expectedOutput);
        }

        private static object CreateParameter(string sourceAssembly, object value, string name = null, string graphQlType = null)
        {
            var genericType = value.GetType();
            if (genericType.IsValueType)
                genericType = typeof(Nullable<>).MakeGenericType(value.GetType());

            if (value is object[])
                genericType = typeof(object);

            object parameter;
            if (name == null)
            {
                var makeGenericType = Type.GetType($"{sourceAssembly}.QueryBuilderParameter`1, {sourceAssembly}").MakeGenericType(genericType);
                parameter = Activator.CreateInstance(makeGenericType, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { value }, CultureInfo.InvariantCulture);
            }
            else
            {
                var makeGenericType = Type.GetType($"{sourceAssembly}.GraphQlQueryParameter`1, {sourceAssembly}").MakeGenericType(genericType);
                parameter = Activator.CreateInstance(makeGenericType, BindingFlags.Instance | BindingFlags.Public, null, new[] { name, graphQlType, value }, CultureInfo.InvariantCulture);
            }

            return parameter;
        }

        private static string GetTestResource(string name)
        {
            using var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.{name}"));
            return reader.ReadToEnd();
        }

        private void CompileIntoAssembly(string sourceCode, string assemblyName)
        {
            var compilation = CompilationHelper.CreateCompilation(sourceCode, assemblyName);
            var assemblyFileName = Path.GetTempFileName();
            var result = compilation.Emit(assemblyFileName);
            var compilationReport = String.Join(Environment.NewLine, result.Diagnostics.Where(l => l.Severity != DiagnosticSeverity.Hidden).Select(l => $"[{l.Severity}] {l}"));
            if (!String.IsNullOrEmpty(compilationReport))
                _outputHelper.WriteLine(compilationReport);

            var errorReport = String.Join(Environment.NewLine, result.Diagnostics.Where(l => l.Severity == DiagnosticSeverity.Error).Select(l => $"[{l.Severity}] {l}"));
            errorReport.ShouldBeNullOrEmpty();

            Assembly.LoadFrom(assemblyFileName);
        }

        [Fact]
        public void GeneratedMutation()
        {
            var schema = DeserializeTestSchema("TestSchema2");
            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator();
            generator.Generate(CreateGenerationContext(stringBuilder, schema));

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(
                @"public class TestMutationBuilder : GraphQlQueryBuilder<TestMutationBuilder>
{
    private static readonly FieldMetadata[] AllFieldMetadata =
        new []
        {
            new FieldMetadata { Name = ""testAction"" },
        };

    protected override string TypeName { get; } = ""TestMutation"";

    public override IReadOnlyList<FieldMetadata> AllFields { get; } = AllFieldMetadata;

    public TestMutationBuilder(string operationName = null) : base(""mutation"", operationName)
    {
    }

    public TestMutationBuilder WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);

	public TestMutationBuilder WithTestAction(QueryBuilderParameter<TestInput> input = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		if (input != null)
			args.Add(new QueryBuilderArgumentInfo { ArgumentName = ""objectParameter"", ArgumentValue = input });

        return WithScalarField(""testAction"", null, null, args);
    }
}

    public partial class TestInput : IGraphQlInputObject
    {
	    private InputPropertyInfo _inputObject1;
	    private InputPropertyInfo _inputObject2;
        private InputPropertyInfo _testProperty;
        private InputPropertyInfo _timestampProperty;

	    [JsonConverter(typeof(QueryBuilderParameterConverter<TestInput>))]
	    public QueryBuilderParameter<TestInput> InputObject1
	    {
		    get => (QueryBuilderParameter<TestInput>)_inputObject1.Value;
		    set => _inputObject1 = new InputPropertyInfo { Name = ""inputObject1"", Value = value };
	    }

	    [JsonConverter(typeof(QueryBuilderParameterConverter<TestInput>))]
	    public QueryBuilderParameter<TestInput> InputObject2
	    {
		    get => (QueryBuilderParameter<TestInput>)_inputObject2.Value;
		    set => _inputObject2 = new InputPropertyInfo { Name = ""inputObject2"", Value = value };
	    }

        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
	    public QueryBuilderParameter<string> TestProperty
	    {
		    get => (QueryBuilderParameter<string>)_testProperty.Value;
		    set => _testProperty = new InputPropertyInfo { Name = ""testProperty"", Value = value };
	    }

        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
	    public QueryBuilderParameter<DateTimeOffset?> Timestamp
	    {
		    get => (QueryBuilderParameter<DateTimeOffset?>)_timestampProperty.Value;
		    set => _timestampProperty = new InputPropertyInfo { Name = ""timestamp"", Value = value, FormatMask = ""yy-MM-dd HH:mmzzz"" };
	    }

	    IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
	    {
		    if (_inputObject1.Name != null) yield return _inputObject1;
		    if (_inputObject2.Name != null) yield return _inputObject2;
            if (_testProperty.Name != null) yield return _testProperty;
            if (_timestampProperty.Name != null) yield return _timestampProperty;
	    }
    }");

            const string assemblyName = "GeneratedMutationTestAssembly";
            CompileIntoAssembly(stringBuilder.ToString(), assemblyName);

            var builderType = Type.GetType($"{assemblyName}.TestMutationBuilder, {assemblyName}");
            builderType.ShouldNotBeNull();
            var formattingType = Type.GetType($"{assemblyName}.Formatting, {assemblyName}");
            formattingType.ShouldNotBeNull();

            var builderInstance = Activator.CreateInstance(builderType, new object[] { null });

            var inputObjectType = Type.GetType($"{assemblyName}.TestInput, {assemblyName}");
            inputObjectType.ShouldNotBeNull();

            var queryParameter2Value = Activator.CreateInstance(inputObjectType);
            var queryParameter1 = CreateParameter(assemblyName, "Test Value", "stringParameter", "String");
            var queryParameter2 = CreateParameter(assemblyName, queryParameter2Value, "objectParameter", "[TestInput!]");
            var testPropertyInfo = inputObjectType.GetProperty("TestProperty");
            testPropertyInfo.SetValue(queryParameter2Value, CreateParameter(assemblyName, "Input Object Parameter Value"));
            var timestampPropertyInfo = inputObjectType.GetProperty("Timestamp");
            timestampPropertyInfo.SetValue(queryParameter2Value, CreateParameter(assemblyName, new DateTimeOffset(2019, 6, 30, 2, 27, 47, TimeSpan.FromHours(2)).AddTicks(1234567)));

            var inputObject = Activator.CreateInstance(inputObjectType);
            testPropertyInfo.SetValue(inputObject, queryParameter1);
            var nestedObject = Activator.CreateInstance(inputObjectType);
            testPropertyInfo.SetValue(nestedObject, CreateParameter(assemblyName, "Nested Value"));
            inputObjectType.GetProperty("InputObject1").SetValue(inputObject, CreateParameter(assemblyName, nestedObject));
            inputObjectType.GetProperty("InputObject2").SetValue(inputObject, queryParameter2);

            builderType
                .GetMethod("WithTestAction", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(
                    builderInstance,
                    new []
                    {
                        inputObject
                    }.Select(p => CreateParameter(assemblyName, p)).ToArray());

            var withParameterMethod = builderType.GetMethod("WithParameter", BindingFlags.Instance | BindingFlags.Public);
            withParameterMethod.MakeGenericMethod(typeof(String)).Invoke(builderInstance, new[] { queryParameter1 });
            withParameterMethod.MakeGenericMethod(queryParameter2Value.GetType()).Invoke(builderInstance, new[] { queryParameter2 });

            var mutation =
                builderType
                    .GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(builderInstance, new[] { Enum.Parse(formattingType, "None"), (byte)2 });

            mutation.ShouldBe("mutation($stringParameter:String=\"Test Value\",$objectParameter:[TestInput!]={testProperty:\"Input Object Parameter Value\",timestamp:\"19-06-30 02:27+02:00\"}){testAction(objectParameter:{inputObject1:{testProperty:\"Nested Value\"},inputObject2:$objectParameter,testProperty:$stringParameter})}");

            var inputObjectJson = JsonConvert.SerializeObject(inputObject);
            inputObjectJson.ShouldBe("{\"TestProperty\":\"Test Value\",\"Timestamp\":null,\"InputObject1\":{\"TestProperty\":\"Nested Value\",\"Timestamp\":null,\"InputObject1\":null,\"InputObject2\":null},\"InputObject2\":{\"TestProperty\":\"Input Object Parameter Value\",\"Timestamp\":\"2019-06-30T02:27:47.1234567+02:00\",\"InputObject1\":null,\"InputObject2\":null}}");

            var deserializedInputObject = JsonConvert.DeserializeObject(inputObjectJson, inputObjectType);
            var testPropertyValue = testPropertyInfo.GetValue(deserializedInputObject);
            var converter = testPropertyValue.GetType().GetMethod("op_Implicit", new[] { testPropertyValue.GetType() });
            var testPropertyPlainValue = converter.Invoke(null, new[] { testPropertyValue });
            testPropertyPlainValue.ShouldBe("Test Value");
        }

        [Fact]
        public void QueryParameterReverseMapping()
        {
            var schema = DeserializeTestSchema("TestSchemaWithUnions");
            var stringBuilder = new StringBuilder();
            var generator = new GraphQlGenerator();
            generator.Generate(CreateGenerationContext(stringBuilder, schema));
            const string assemblyName = "QueryParameterReverseMappingTestAssembly";
            CompileIntoAssembly(stringBuilder.ToString(), assemblyName);

            GetQueryParameterGraphQlType(GetGeneratedType("UnderscoreNamedInput"), true).ShouldBe("underscore_named_input");
            GetQueryParameterGraphQlType(GetGeneratedType("UnderscoreNamedInput").MakeArrayType(), false).ShouldBe("[underscore_named_input]!");
            GetQueryParameterGraphQlType(typeof(ICollection<>).MakeGenericType(typeof(Int32)), true).ShouldBe("[Int]");
            GetQueryParameterGraphQlType(typeof(Double), true).ShouldBe("Float");
            GetQueryParameterGraphQlType(typeof(Decimal), false).ShouldBe("Float!");
            GetQueryParameterGraphQlType(typeof(Guid), true).ShouldBe("ID");
            GetQueryParameterGraphQlType(typeof(String), false).ShouldBe("String!");

            string GetQueryParameterGraphQlType(Type valueType, bool nullable)
            {
                var queryParameterType = GetGeneratedType("GraphQlQueryParameter`1");
                var queryParameter = Activator.CreateInstance(queryParameterType.MakeGenericType(valueType), "parameter_name", null, nullable);
                return (string)queryParameterType.GetProperty("GraphQlTypeName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(queryParameter);
            }

            Type GetGeneratedType(string typeName)
            {
                typeName = $"{assemblyName}.{typeName}, {assemblyName}";
                return Type.GetType(typeName) ?? throw new InvalidOperationException($"value type \"{typeName}\" not found");
            }
        }

        private static string StripBaseClasses(string sourceCode)
        {
            using var reader = new StreamReader(typeof(GraphQlGenerator).Assembly.GetManifestResourceStream("GraphQlClientGenerator.BaseClasses.cs"));
            return sourceCode.Replace("#region base classes" + Environment.NewLine + reader.ReadToEnd() + Environment.NewLine + "#endregion", null).Trim();
        }
    }
}
