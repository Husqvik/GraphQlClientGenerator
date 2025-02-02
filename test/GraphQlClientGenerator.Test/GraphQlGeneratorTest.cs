using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace GraphQlClientGenerator.Test;

public class GraphQlGeneratorTest(ITestOutputHelper outputHelper)
{
    private static readonly GraphQlSchema TestSchema = DeserializeTestSchema("TestSchema");

    private static GraphQlSchema DeserializeTestSchema(string resourceName) =>
        GraphQlGenerator.DeserializeGraphQlSchema(GetTestResource($"TestSchemas.{resourceName}"));

    private static TestSingleFileGenerationContext CreateGenerationContext(
        StringBuilder builder,
        GraphQlSchema schema,
        GeneratedObjectType objectTypes = GeneratedObjectType.All,
        Action<string> logMessage = null) =>
        new(schema, new StringWriter(builder), objectTypes) { LogMessage = logMessage };

    private static readonly IReadOnlyList<int> ExpectedFileSizes = [
        447, 476, 1400, 1180, 979, 4292, 520, 570, 2133, 1943, 456, 1115, 1174, 1686, 1780, 757, 495, 1612, 499, 1439, 793, 492, 1470, 4153, 964, 763, 3704, 4984, 479, 1417, 567, 2231, 615, 2415, 1226, 7008, 448,
        1251, 572, 677, 2839, 2588, 490, 1494, 462, 1313, 368, 6096, 595, 2251, 1958, 922, 7945, 880, 1531, 494, 1520, 4769, 17356, 808, 1627, 628, 2713, 10002, 973, 5275, 1102, 553, 3333, 7245, 435, 1437, 544, 502,
        1550, 2002, 576, 2287, 532, 1827, 622, 2620, 768, 572, 2033, 582, 2204, 3656, 756, 3899, 589, 2199, 677, 538, 1861, 2970, 1073, 796, 4018, 5759, 899, 4541, 521, 1794, 424, 1381, 665, 752, 3580, 2914, 451,
        1203, 481, 590, 560, 2014, 549, 1877, 2328, 560, 2255, 783, 854, 4478, 939, 531, 1850, 873, 4594, 585, 2117, 502, 1493, 490, 2695, 5110, 587, 2243, 559, 1986, 562, 1231, 3653, 1986];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleFileGeneration(bool fileScopedNamespaces)
    {
        var configuration =
            new GraphQlGeneratorConfiguration
            {
                TargetNamespace = "GraphQlGeneratorTest",
                CodeDocumentationType = CodeDocumentationType.XmlSummary | CodeDocumentationType.DescriptionAttribute,
                CSharpVersion = CSharpVersion.CSharp6,
                FileScopedNamespaces = fileScopedNamespaces
            };

        var directoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "GraphQlGeneratorTest"));
        outputHelper.WriteLine($"Temp path: {directoryInfo.FullName}");

        try
        {
            var context =
                new MultipleFileGenerationContext(
                    DeserializeTestSchema("TestSchema2"),
                    new FileSystemEmitter(directoryInfo.FullName),
                    "GraphQlGeneratorTest.csproj")
                {
                    LogMessage = outputHelper.WriteLine
                };

            var generator = new GraphQlGenerator(configuration);
            generator.Generate(context);

            var files = directoryInfo.GetFiles().OrderBy(f => f.Name).ToArray();
            var fileNames = files.Select(f => f.Name);
            fileNames.ShouldBe(
            [
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
            ]);

            var fileSizes =
                files
                    .Where(f => f.Name != "BaseClasses.cs")
                    .Select(f => File.ReadAllText(f.FullName).ReplaceLineEndings(Environment.NewLine).Length);

            var resourceSuffix = String.Empty;
            if (fileScopedNamespaces)
                resourceSuffix = ".FileScoped";
            else
                fileSizes.ShouldBe(ExpectedFileSizes);

            var expectedOutput = GetTestResource($"ExpectedMultipleFilesContext.Avatar{resourceSuffix}");
            File.ReadAllText(Path.Combine(directoryInfo.FullName, "Avatar.cs")).ShouldBe(expectedOutput);
            expectedOutput = GetTestResource($"ExpectedMultipleFilesContext.Home{resourceSuffix}");
            File.ReadAllText(Path.Combine(directoryInfo.FullName, "Home.cs")).ShouldBe(expectedOutput);
            expectedOutput = GetTestResource($"ExpectedMultipleFilesContext.IncludeDirective{resourceSuffix}");
            File.ReadAllText(Path.Combine(directoryInfo.FullName, "IncludeDirective.cs")).ShouldBe(expectedOutput);
            expectedOutput = GetTestResource($"ExpectedMultipleFilesContext.MutationQueryBuilder{resourceSuffix}");
            File.ReadAllText(Path.Combine(directoryInfo.FullName, "MutationQueryBuilder.cs")).ShouldBe(expectedOutput);
        }
        finally
        {
            Directory.Delete(directoryInfo.FullName, true);
        }
    }

    [Fact]
    public Task GenerateFullClientCSharpFile()
    {
        var configuration =
            new GraphQlGeneratorConfiguration
            {
                TargetNamespace = "GraphQlGenerator.Test",
                CodeDocumentationType = CodeDocumentationType.XmlSummary | CodeDocumentationType.DescriptionAttribute
            };
            
        var generator = new GraphQlGenerator(configuration);
        var generatedSourceCode = generator.GenerateFullClientCSharpFile(TestSchema);
        return Verify(generatedSourceCode);
    }

    [Fact]
    public void GenerateQueryBuilder()
    {
        var configuration = new GraphQlGeneratorConfiguration();
        configuration.CustomClassNameMapping.Add("AwayMode", "VacationMode");

        var stringBuilder = new StringBuilder();
        new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, TestSchema, GeneratedObjectType.BaseClasses | GeneratedObjectType.QueryBuilders));

        var expectedQueryBuilders = GetTestResource("ExpectedSingleFileGenerationContext.QueryBuilders");
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
        var expectedDataClasses = GetTestResource("ExpectedSingleFileGenerationContext.DataClasses");
        var generatedSourceCode = stringBuilder.ToString();
        generatedSourceCode.ShouldBe(expectedDataClasses);
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
                PropertyGeneration = PropertyGenerationOption.BackingField,
                ScalarFieldTypeMappingProvider = new TestCustomBooleanTypeMappingProvider()
            };

        var stringBuilder = new StringBuilder();
        new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, TestSchema, GeneratedObjectType.DataClasses));
        var expectedDataClasses = GetTestResource("ExpectedSingleFileGenerationContext.DataClassesWithTypeConfiguration");
        var generatedSourceCode = stringBuilder.ToString();
        generatedSourceCode.ShouldBe(expectedDataClasses);
    }

    private class TestCustomBooleanTypeMappingProvider : IScalarFieldTypeMappingProvider
    {
        public ScalarFieldTypeDescription GetCustomScalarFieldType(ScalarFieldTypeProviderContext context) =>
            context.FieldType.Name == "Boolean"
                ? new ScalarFieldTypeDescription { NetTypeName = "bool" }
                : DefaultScalarFieldTypeMappingProvider.Instance.GetCustomScalarFieldType(context);
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

        var expectedDataClasses = GetTestResource("ExpectedSingleFileGenerationContext.FormatMasks");
        var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
        generatedSourceCode.ShouldBe(expectedDataClasses);
    }

    private class TestFormatMaskScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
    {
        public static readonly TestFormatMaskScalarFieldTypeMappingProvider Instance = new();

        public ScalarFieldTypeDescription GetCustomScalarFieldType(ScalarFieldTypeProviderContext context)
        {
            var isNotNull = context.FieldType.Kind == GraphQlTypeKind.NonNull;
            var unwrappedType = context.FieldType.UnwrapIfNonNull();
            var nullablePostfix = isNotNull ? null : "?";

            if (unwrappedType.Name == "ID")
                return new ScalarFieldTypeDescription { NetTypeName = $"Guid{nullablePostfix}", FormatMask = "N" };

            if (context.FieldName is "before" or "after" || unwrappedType.Name == "DateTimeOffset")
                return new ScalarFieldTypeDescription { NetTypeName = $"DateTimeOffset{nullablePostfix}", FormatMask = "yyyy-MM-dd\"T\"HH:mm" };

            return DefaultScalarFieldTypeMappingProvider.Instance.GetCustomScalarFieldType(context);
        }
    }

    [Fact]
    public void NewCSharpSyntaxWithClassPrefixAndSuffix()
    {
        var configuration =
            new GraphQlGeneratorConfiguration
            {
                CSharpVersion = CSharpVersion.CSharp6,
                ClassPrefix = "Test",
                ClassSuffix = "V1",
                MemberAccessibility = MemberAccessibility.Internal
            };
            
        var schema = DeserializeTestSchema("TestSchema2");

        var stringBuilder = new StringBuilder();
        var generator = new GraphQlGenerator(configuration);
        generator.Generate(CreateGenerationContext(stringBuilder, schema));

        var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
        var expectedOutput = GetTestResource("ExpectedSingleFileGenerationContext.NewCSharpSyntaxWithClassPrefixAndSuffix");
        generatedSourceCode.ShouldBe(expectedOutput);

        CompileIntoAssembly(stringBuilder.ToString(), "GraphQLTestAssembly").ShouldNotBeNull();

        Type.GetType("GraphQLTestAssembly.GraphQlQueryBuilder, GraphQLTestAssembly").ShouldNotBeNull();
    }

    [Fact]
    public void WithNullableReferencesAndPropertyNullabilityBySchema()
    {
        var configuration =
            new GraphQlGeneratorConfiguration
            {
                CSharpVersion = CSharpVersion.CSharp6,
                EnableNullableReferences = true,
                DataClassMemberNullability = DataClassMemberNullability.DefinedBySchema
            };

        var schema = DeserializeTestSchema("TestSchema2");

        var stringBuilder = new StringBuilder();
        var generator = new GraphQlGenerator(configuration);
        generator.Generate(CreateGenerationContext(stringBuilder, schema));

        var expectedOutput = GetTestResource("ExpectedSingleFileGenerationContext.NullableReferences");
        var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
        generatedSourceCode.ShouldBe(expectedOutput);
    }

    [Fact]
    public void WithUnions()
    {
        var configuration =
            new GraphQlGeneratorConfiguration
            {
                CSharpVersion = CSharpVersion.CSharp6,
                EnableNullableReferences = true,
                JsonPropertyGeneration = JsonPropertyGenerationOption.UseDefaultAlias,
                EnumValueNaming = EnumValueNamingOption.Original,
                ScalarFieldTypeMappingProvider = TestFormatMaskScalarFieldTypeMappingProvider.Instance
            };
            
        var schema = DeserializeTestSchema("TestSchemaWithUnions");

        var stringBuilder = new StringBuilder();
        var generator = new GraphQlGenerator(configuration);
        var logMessages = new List<string>();
        var generationContext = CreateGenerationContext(stringBuilder, schema, logMessage: logMessages.Add);

        generator.Generate(generationContext);

        var expectedOutput = GetTestResource("ExpectedSingleFileGenerationContext.Unions");
        var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
        generatedSourceCode.ShouldBe(expectedOutput);

        logMessages.Count.ShouldBe(2);
        logMessages[0].ShouldBe("WARNING: duplicate \"skip\" directive definition");
        logMessages[1].ShouldBe("WARNING: duplicate union \"UnionType\" possible type \"ConcreteType3\"");
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
            """
            public class TestQueryBuilder : GraphQlQueryBuilder<TestQueryBuilder>
            {
                private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
                    new []
                    {
                        new GraphQlFieldMetadata { Name = "testField" },
                        new GraphQlFieldMetadata { Name = "objectParameter" }
                    };
            
                protected override string TypeName { get; } = "Test";
            
                public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;
            
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
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueInt16", ArgumentValue = valueInt16 });
            
            		if (valueUInt16 != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueUInt16", ArgumentValue = valueUInt16 });
            
            		if (valueByte != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueByte", ArgumentValue = valueByte });
            
            		if (valueInt32 != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueInt32", ArgumentValue = valueInt32 });
            
            		if (valueUInt32 != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueUInt32", ArgumentValue = valueUInt32 });
            
            		if (valueInt64 != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueInt64", ArgumentValue = valueInt64 });
            
            		if (valueUInt64 != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueUInt64", ArgumentValue = valueUInt64 });
            
            		if (valueSingle != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueSingle", ArgumentValue = valueSingle });
            
            		if (valueDouble != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueDouble", ArgumentValue = valueDouble });
            
            		if (valueDecimal != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueDecimal", ArgumentValue = valueDecimal });
            
            		if (valueDateTime != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueDateTime", ArgumentValue = valueDateTime, FormatMask = "yy-MM-dd HH:mmZ" });
            
            		if (valueDateTimeOffset != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueDateTimeOffset", ArgumentValue = valueDateTimeOffset });
            
            		if (valueGuid != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueGuid", ArgumentValue = valueGuid });
            
            		if (valueString != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "valueString", ArgumentValue = valueString });
            
            		return WithScalarField("testField", null, null, args);
            	}
            
                public TestQueryBuilder WithObjectParameterField(QueryBuilderParameter<object> objectParameter = null)
            	{
            		var args = new List<QueryBuilderArgumentInfo>();
            		if (objectParameter != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "objectParameter", ArgumentValue = objectParameter });
            
                    return WithScalarField("objectParameter", "fieldAlias", new GraphQlDirective[] { new IncludeDirective(new GraphQlQueryParameter<bool>("direct", "Boolean", true)), new SkipDirective((QueryBuilderParameter<bool>)false) }, args);
                }
            
                public TestQueryBuilder WithTestFragment(MeQueryBuilder queryBuilder) => WithFragment(queryBuilder, null);
            }
            """);

        const string assemblyName = "GeneratedQueryTestAssembly";
        CompileIntoAssembly(stringBuilder.ToString(), assemblyName);

        var builderType = Type.GetType($"{assemblyName}.TestQueryBuilder, {assemblyName}").ShouldNotBeNull();

        var builderInstance = Activator.CreateInstance(builderType);
        builderType
            .GetMethod("WithTestField", BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotBeNull()
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
                    "\"string\" value"
                }.Select(p => CreateParameter(assemblyName, p)).ToArray());

        builderType
            .GetMethod("WithObjectParameterField", BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotBeNull()
            .Invoke(
                builderInstance,
                new object[]
                {
                    new []
                    {
                        JsonConvert.DeserializeObject("""{ "rootProperty1": "root value 1", "rootProperty2": 123.456, "rootProperty3": true, "rootProperty4": null, "rootProperty5": { "nestedProperty1": 987, "nestedProperty2": "a \"quoted\" value\\t\\r\\n" } }"""),
                        JsonConvert.DeserializeObject("""[{ "rootProperty1": "root value 2" }, { "rootProperty1": false }]""")
                    }
                }.Select(p => CreateParameter(assemblyName, p)).ToArray());

        var query = BuildQuery(builderInstance);
        query.ShouldBe("{testField(valueInt16:1,valueUInt16:2,valueByte:3,valueInt32:4,valueUInt32:5,valueInt64:6,valueUInt64:7,valueSingle:8.123,valueDouble:9.456,valueDecimal:10.789,valueDateTime:\"19-06-30 00:27Z\",valueDateTimeOffset:\"2019-06-30T02:27:47.1234567+02:00\",valueGuid:\"00000000-0000-0000-0000-000000000000\",valueString:\"\\\"string\\\" value\"),fieldAlias:objectParameter(objectParameter:[{rootProperty1:\"root value 1\",rootProperty2:123.456,rootProperty3:true,rootProperty4:null,rootProperty5:{nestedProperty1:987,nestedProperty2:\"a \\\"quoted\\\" value\\\\t\\\\r\\\\n\"}},[{rootProperty1:\"root value 2\"},{rootProperty1:false}]])@include(if:$direct)@skip(if:false)}");

        query = BuildQuery(builderInstance, "Indented");
        query.ShouldBe($"{{{Environment.NewLine}  testField(valueInt16: 1, valueUInt16: 2, valueByte: 3, valueInt32: 4, valueUInt32: 5, valueInt64: 6, valueUInt64: 7, valueSingle: 8.123, valueDouble: 9.456, valueDecimal: 10.789, valueDateTime: \"19-06-30 00:27Z\", valueDateTimeOffset: \"2019-06-30T02:27:47.1234567+02:00\", valueGuid: \"00000000-0000-0000-0000-000000000000\", valueString: \"\\\"string\\\" value\"){Environment.NewLine}  fieldAlias: objectParameter(objectParameter: [{Environment.NewLine}    {{{Environment.NewLine}      rootProperty1: \"root value 1\",{Environment.NewLine}      rootProperty2: 123.456,{Environment.NewLine}      rootProperty3: true,{Environment.NewLine}      rootProperty4: null,{Environment.NewLine}      rootProperty5: {{{Environment.NewLine}        nestedProperty1: 987,{Environment.NewLine}        nestedProperty2: \"a \\\"quoted\\\" value\\\\t\\\\r\\\\n\"}}}},{Environment.NewLine}    [{Environment.NewLine}    {{{Environment.NewLine}      rootProperty1: \"root value 2\"}},{Environment.NewLine}    {{{Environment.NewLine}      rootProperty1: false}}]]) @include(if: $direct) @skip(if: false){Environment.NewLine}}}");

        var rootQueryBuilderType = Type.GetType($"{assemblyName}.QueryQueryBuilder, {assemblyName}");
        rootQueryBuilderType.ShouldNotBeNull();
        var rootQueryBuilderInstance = rootQueryBuilderType.GetConstructor([typeof(string)]).ShouldNotBeNull().Invoke(new object[1]);
        WithAllFields(rootQueryBuilderInstance);
        BuildQuery(rootQueryBuilderInstance);

        builderType
            .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotBeNull()
            .Invoke(builderInstance, null);

        var meBuilderType = Type.GetType($"{assemblyName}.MeQueryBuilder, {assemblyName}").ShouldNotBeNull();
        var childFragmentBuilderInstance = Activator.CreateInstance(meBuilderType);
        WithAllFields(childFragmentBuilderInstance, true);

        builderType
            .GetMethod("WithTestFragment", BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotBeNull()
            .Invoke(builderInstance, [childFragmentBuilderInstance]);

        query = BuildQuery(builderInstance);
        query.ShouldBe("{...on Me{id,firstName,lastName,fullName,ssn,email,language,tone,mobile}}");
    }

    private static void WithAllFields(object queryBuilder, bool scalarOnly = false) =>
        queryBuilder
            .GetType()
            .GetMethod(scalarOnly ? "WithAllScalarFields" : "WithAllFields", BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotBeNull()
            .Invoke(queryBuilder, null);

    private static string BuildQuery(object queryBuilder, string formatting = "None")
    {
        var builderType = queryBuilder.GetType();
        var assemblyName = builderType.Assembly.GetName().Name;
        var formattingType = Type.GetType($"{assemblyName}.Formatting, {assemblyName}").ShouldNotBeNull();

        return
            (string)builderType
                .GetMethod("Build", [formattingType, typeof(byte)])
                .ShouldNotBeNull()
                .Invoke(queryBuilder, [Enum.Parse(formattingType, formatting), (byte)2]);
    }

    [Fact]
    public void GeneratedQueryWithAllFragments()
    {
        var configuration = new GraphQlGeneratorConfiguration { JsonPropertyGeneration = JsonPropertyGenerationOption.Always };

        var schema = DeserializeTestSchema("TestSchemaWithUnions");
        var stringBuilder = new StringBuilder();
        var generator = new GraphQlGenerator(configuration);
        generator.Generate(CreateGenerationContext(stringBuilder, schema));

        const string assemblyName = "GeneratedQueryWithFragmentsTestAssembly";
        CompileIntoAssembly(stringBuilder.ToString(), assemblyName);

        var rootQueryBuilderType = Type.GetType($"{assemblyName}.QueryQueryBuilder, {assemblyName}");
        rootQueryBuilderType.ShouldNotBeNull();
        var rootQueryBuilderInstance = rootQueryBuilderType.GetConstructor([typeof(string)]).ShouldNotBeNull().Invoke(new object[1]);
        WithAllFields(rootQueryBuilderInstance);

        var formattingType = Type.GetType($"{assemblyName}.Formatting, {assemblyName}");
        formattingType.ShouldNotBeNull();

        var query = BuildQuery(rootQueryBuilderInstance);
        query.ShouldBe("query{scalarValue,simpleObject{id,stringValueNullable,stringValue,stringArrayValue,nestedList{id,stringValueNullable,stringValue,stringArrayValue}},union{__typename,...on ConcreteType1{TypeName,name,concreteType1Field,value,deprecated_field,accessor{value},accessors{value},nestedAccessors{value}},...on ConcreteType2{name,concreteType2Field,value},...on ConcreteType3{name,concreteType3Field,VALUE},...on ConcreteType4{concreteType4Field}},underscore_named_field{underscore_named_field_enum,underscore_named_field_enum_collection},nestedLists,_,COLLISIONS,collisions}");
    }

    [Fact]
    public void DeprecatedAttributes()
    {
        var configuration =
            new GraphQlGeneratorConfiguration
            {
                CSharpVersion = CSharpVersion.CSharp6,
                CodeDocumentationType = CodeDocumentationType.XmlSummary | CodeDocumentationType.DescriptionAttribute,
                IncludeDeprecatedFields = true,
                GeneratePartialClasses = false
            };

        var schema = DeserializeTestSchema("TestSchemaWithDeprecatedFields");

        var stringBuilder = new StringBuilder();
        new GraphQlGenerator(configuration).Generate(CreateGenerationContext(stringBuilder, schema, GeneratedObjectType.DataClasses));
        var expectedOutput = GetTestResource("ExpectedSingleFileGenerationContext.DeprecatedAttributes").Replace("\r", String.Empty);
        stringBuilder.ToString().Replace("\r", String.Empty).ShouldBe(expectedOutput);
    }

    private static object CreateParameter(string sourceAssembly, object value, string name = null, string graphQlType = null, Type netParameterType = null)
    {
        var genericType = netParameterType ?? value.GetType();
        if (genericType.IsValueType)
            genericType = typeof(Nullable<>).MakeGenericType(value.GetType());

        if (value is object[])
            genericType = typeof(object);

        var bindingFlags = BindingFlags.Instance;
        string parameterTypeName;
        object[] constructorArguments;
        if (name is null)
        {
            parameterTypeName = "QueryBuilderParameter";
            bindingFlags |= BindingFlags.NonPublic;
            constructorArguments = [value];
        }
        else
        {
            parameterTypeName = "GraphQlQueryParameter";
            bindingFlags |= BindingFlags.Public;
            constructorArguments = [name, graphQlType, value];
        }


        var parameterType = Type.GetType($"{sourceAssembly}.{parameterTypeName}`1, {sourceAssembly}").ShouldNotBeNull().MakeGenericType(genericType);
        return Activator.CreateInstance(parameterType, bindingFlags, null, constructorArguments, CultureInfo.InvariantCulture);
    }

    private static string GetTestResource(string name)
    {
        using var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.{name}").ShouldNotBeNull());
        return reader.ReadToEnd().ReplaceLineEndings(Environment.NewLine);
    }

    private Assembly CompileIntoAssembly(string sourceCode, string assemblyName)
    {
        var compilation = CompilationHelper.CreateCompilation(sourceCode, assemblyName);
        var assemblyFileName = Path.GetTempFileName();
        var result = compilation.Emit(assemblyFileName);
        var compilationReport = String.Join(Environment.NewLine, result.Diagnostics.Where(l => l.Severity != DiagnosticSeverity.Hidden).Select(l => $"[{l.Severity}] {l}"));
        if (!String.IsNullOrEmpty(compilationReport))
            outputHelper.WriteLine(compilationReport);

        var errorReport = String.Join(Environment.NewLine, result.Diagnostics.Where(l => l.Severity == DiagnosticSeverity.Error).Select(l => $"[{l.Severity}] {l}"));
        errorReport.ShouldBeNullOrEmpty();

        return Assembly.LoadFrom(assemblyFileName);
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
            """
            public class TestMutationBuilder : GraphQlQueryBuilder<TestMutationBuilder>
            {
                private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
                    new []
                    {
                        new GraphQlFieldMetadata { Name = "testAction" }
                    };
            
                protected override string TypeName { get; } = "TestMutation";
            
                public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;
            
                public TestMutationBuilder(string operationName = null) : base("mutation", operationName)
                {
                }
            
                public TestMutationBuilder WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);
            
            	public TestMutationBuilder WithTestAction(QueryBuilderParameter<TestInput> input = null)
            	{
            		var args = new List<QueryBuilderArgumentInfo>();
            		if (input != null)
            			args.Add(new QueryBuilderArgumentInfo { ArgumentName = "objectParameter", ArgumentValue = input });
            
                    return WithScalarField("testAction", null, null, args);
                }
            }
            
            public partial class TestInput : IGraphQlInputObject
            {
            	private InputPropertyInfo _inputObject1;
            	private InputPropertyInfo _inputObject2;
                private InputPropertyInfo _testProperty;
                private InputPropertyInfo _testNullValueProperty;
                private InputPropertyInfo _timestampProperty;
        
            	[JsonConverter(typeof(QueryBuilderParameterConverter<TestInput>))]
            	public QueryBuilderParameter<TestInput> InputObject1
            	{
            		get => (QueryBuilderParameter<TestInput>)_inputObject1.Value;
            		set => _inputObject1 = new InputPropertyInfo { Name = "inputObject1", Value = value };
            	}
        
            	[JsonConverter(typeof(QueryBuilderParameterConverter<TestInput>))]
            	public QueryBuilderParameter<TestInput> InputObject2
            	{
            		get => (QueryBuilderParameter<TestInput>)_inputObject2.Value;
            		set => _inputObject2 = new InputPropertyInfo { Name = "inputObject2", Value = value };
            	}
        
                [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
            	public QueryBuilderParameter<string> TestProperty
            	{
            		get => (QueryBuilderParameter<string>)_testProperty.Value;
            		set => _testProperty = new InputPropertyInfo { Name = "testProperty", Value = value };
            	}
        
                [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
            	public QueryBuilderParameter<string> TestNullValueProperty
            	{
            		get => (QueryBuilderParameter<string>)_testNullValueProperty.Value;
            		set => _testNullValueProperty = new InputPropertyInfo { Name = "testNullValueProperty", Value = value };
            	}
        
                [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
            	public QueryBuilderParameter<DateTimeOffset?> Timestamp
            	{
            		get => (QueryBuilderParameter<DateTimeOffset?>)_timestampProperty.Value;
            		set => _timestampProperty = new InputPropertyInfo { Name = "timestamp", Value = value, FormatMask = "yy-MM-dd HH:mm zzz" };
            	}
        
            	IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
            	{
            		if (_inputObject1.Name != null) yield return _inputObject1;
            		if (_inputObject2.Name != null) yield return _inputObject2;
                    if (_testProperty.Name != null) yield return _testProperty;
                    if (_testNullValueProperty.Name != null) yield return _testNullValueProperty;
                    if (_timestampProperty.Name != null) yield return _timestampProperty;
            	}
            }
            """);

        const string assemblyName = "GeneratedMutationTestAssembly";
        CompileIntoAssembly(stringBuilder.ToString(), assemblyName);

        var builderType = Type.GetType($"{assemblyName}.TestMutationBuilder, {assemblyName}");
        builderType.ShouldNotBeNull();
        var formattingType = Type.GetType($"{assemblyName}.Formatting, {assemblyName}");
        formattingType.ShouldNotBeNull();

        var builderInstance = Activator.CreateInstance(builderType, [null]);

        var inputObjectType = Type.GetType($"{assemblyName}.TestInput, {assemblyName}");
        inputObjectType.ShouldNotBeNull();

        var queryParameter2Value = Activator.CreateInstance(inputObjectType).ShouldNotBeNull();
        var queryParameter1 = CreateParameter(assemblyName, "Test Value", "stringParameter", "String");
        var queryParameter2 = CreateParameter(assemblyName, queryParameter2Value, "objectParameter", "[TestInput!]");
        var testPropertyInfo = inputObjectType.GetProperty("TestProperty").ShouldNotBeNull();
        testPropertyInfo.SetValue(queryParameter2Value, CreateParameter(assemblyName, "Input Object Parameter Value"));
        var timestampPropertyInfo = inputObjectType.GetProperty("Timestamp").ShouldNotBeNull();
        timestampPropertyInfo.SetValue(queryParameter2Value, CreateParameter(assemblyName, new DateTimeOffset(2019, 6, 30, 2, 27, 47, TimeSpan.FromHours(2)).AddTicks(1234567)));

        var inputObject = Activator.CreateInstance(inputObjectType);
        testPropertyInfo.SetValue(inputObject, queryParameter1);
        var nestedObject = Activator.CreateInstance(inputObjectType);
        testPropertyInfo.SetValue(nestedObject, CreateParameter(assemblyName, "Nested Value"));
        inputObjectType.GetProperty("InputObject1").ShouldNotBeNull().SetValue(inputObject, CreateParameter(assemblyName, nestedObject));
        inputObjectType.GetProperty("InputObject2").ShouldNotBeNull().SetValue(inputObject, queryParameter2);
        inputObjectType.GetProperty("TestNullValueProperty").ShouldNotBeNull().SetValue(inputObject, CreateParameter(assemblyName, null, null, "String", typeof(String)));

        builderType
            .GetMethod("WithTestAction", BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotBeNull()
            .Invoke(builderInstance, [CreateParameter(assemblyName, inputObject)]);

        var withParameterMethod = builderType.GetMethod("WithParameter", BindingFlags.Instance | BindingFlags.Public).ShouldNotBeNull();
        withParameterMethod.MakeGenericMethod(typeof(String)).Invoke(builderInstance, [queryParameter1]);
        withParameterMethod.MakeGenericMethod(queryParameter2Value.GetType()).Invoke(builderInstance, [queryParameter2]);

        var mutation = BuildQuery(builderInstance);

        mutation.ShouldBe("""mutation($stringParameter:String="Test Value",$objectParameter:[TestInput!]={testProperty:"Input Object Parameter Value",timestamp:"19-06-30 02:27 +02:00"}){testAction(objectParameter:{inputObject1:{testProperty:"Nested Value"},inputObject2:$objectParameter,testProperty:$stringParameter,testNullValueProperty:null})}""");

        var inputObjectJson = JsonConvert.SerializeObject(inputObject);
        inputObjectJson.ShouldBe("""{"InputObject1":{"InputObject1":null,"InputObject2":null,"TestProperty":"Nested Value","TestNullValueProperty":null,"Timestamp":null},"InputObject2":{"InputObject1":null,"InputObject2":null,"TestProperty":"Input Object Parameter Value","TestNullValueProperty":null,"Timestamp":"2019-06-30T02:27:47.1234567+02:00"},"TestProperty":"Test Value","TestNullValueProperty":null,"Timestamp":null}""");

        var deserializedInputObject = JsonConvert.DeserializeObject(inputObjectJson, inputObjectType);
        var testPropertyValue = testPropertyInfo.GetValue(deserializedInputObject).ShouldNotBeNull();
        var converter = testPropertyValue.GetType().GetMethod("op_Implicit", [testPropertyValue.GetType()]).ShouldNotBeNull();
        var testPropertyPlainValue = converter.Invoke(null, [testPropertyValue]);
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
        return;

        static string GetQueryParameterGraphQlType(Type valueType, bool nullable)
        {
            var queryParameterType = GetGeneratedType("GraphQlQueryParameter`1");
            var queryParameter = Activator.CreateInstance(queryParameterType.MakeGenericType(valueType), "parameter_name", null, nullable);
            return (string)queryParameterType.GetProperty("GraphQlTypeName", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().GetValue(queryParameter);
        }

        static Type GetGeneratedType(string typeName)
        {
            typeName = $"{assemblyName}.{typeName}, {assemblyName}";
            return Type.GetType(typeName) ?? throw new InvalidOperationException($"value type \"{typeName}\" not found");
        }
    }

    [Fact]
    public void WithNestedListsOfComplexObjects()
    {
        var configuration = new GraphQlGeneratorConfiguration();
            
        var schema = DeserializeTestSchema("TestSchemaWithNestedListsOfComplexObjects");

        var stringBuilder = new StringBuilder();
        var generator = new GraphQlGenerator(configuration);
        generator.Generate(CreateGenerationContext(stringBuilder, schema));

        var expectedOutput = GetTestResource("ExpectedSingleFileGenerationContext.NestedListsOfComplexObjects");
        var generatedSourceCode = StripBaseClasses(stringBuilder.ToString());
        generatedSourceCode.ShouldBe(expectedOutput);
    }

    private static string StripBaseClasses(string sourceCode)
    {
        using var reader = new StreamReader(typeof(GraphQlGenerator).Assembly.GetManifestResourceStream("GraphQlClientGenerator.BaseClasses.cs").ShouldNotBeNull());
        return sourceCode.Replace($"#region base classes{Environment.NewLine}{reader.ReadToEnd()}{Environment.NewLine}#endregion", null).Trim();
    }

    private class TestSingleFileGenerationContext(GraphQlSchema schema, TextWriter writer, GeneratedObjectType objectTypes = GeneratedObjectType.All)
        : SingleFileGenerationContext(schema, writer, objectTypes)
    {
        public override byte IndentationSize => 0;

        public override void BeforeGeneration()
        {
        }

        public override void AfterGeneration() =>
            typeof(SingleFileGenerationContext)
                .GetMethod("ExitNullableReferenceScope", BindingFlags.NonPublic | BindingFlags.Instance)
                .ShouldNotBeNull()
                .Invoke(this, null);
    }
}