GraphQL C# client generator
=======================

[![Build](https://ci.appveyor.com/api/projects/status/t4iti5bn5ubs2k3k?svg=true&pendingText=pending&passingText=passed&failingText=failed)](https://ci.appveyor.com/project/Husqvik/graphql-client-generator)
[![NuGet Badge](https://badge.fury.io/nu/GraphQlClientGenerator.svg)](https://www.nuget.org/packages/GraphQlClientGenerator)

This simple console app generates C# GraphQL query builder and data classes for simple, compiler checked, usage of a GraphQL API.

----------

Generator app usage
-------------

```console
GraphQlClientGenerator.Console --serviceUrl <GraphQlServiceUrl> --outputPath <TargetPath> --namespace <TargetNamespace> [--header <header value>]
```

Nuget package
-------------
Installation:
```console
Install-Package GraphQlClientGenerator
```

dotnet tool
-------------
```console
dotnet tool install GraphQlClientGenerator.Tool --global
graphql-client-generator --serviceUrl <GraphQlServiceUrl> --outputPath <TargetPath> --namespace <TargetNamespace> [--header <header value>]
```

Code
-------------
Code example for class generation:
```csharp
var schema = await GraphQlGenerator.RetrieveSchema("https://my-graphql-api/gql");
var generator = new GraphQlGenerator();
var generatedClasses = generator.GenerateFullClientCSharpFile(schema);
```

or using full blown setup:

```csharp
var schema = await GraphQlGenerator.RetrieveSchema("https://my-graphql-api/gql");
var configuration = new GraphQlGeneratorConfiguration { TargetNamespace = "MyGqlApiClient", ... };
var generator = new GraphQlGenerator(configuration);
var builder = new StringBuilder();
using var writer = new StringWriter(builder);
var generationContext = new SingleFileGenerationContext(schema, writer) { LogMessage = Console.WriteLine };
generator.Generate(generationContext);
var csharpCode = builder.ToString();
```

C# 9 source generator
-------------
C# 9 introduced source generators that can be attached to compilation process. Generated classes will be automatically included in project.

Project file example:
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net9.0</TargetFramework>

  <!-- GraphQL generator properties -->
  <GraphQlClientGenerator_ServiceUrl>https://api.tibber.com/v1-beta/gql</GraphQlClientGenerator_ServiceUrl>
  <!-- GraphQlClientGenerator_Namespace is optional; if omitted the first compilation unit namespace will be used -->
  <GraphQlClientGenerator_Namespace>$(RootNamespace)</GraphQlClientGenerator_Namespace>
  <GraphQlClientGenerator_CustomClassMapping>Consumption:ConsumptionEntry|Production:ProductionEntry|RootMutation:TibberMutation|Query:Tibber</GraphQlClientGenerator_CustomClassMapping>
  
  <!-- other GraphQL generator property values -->
  
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="GraphQlClientGenerator" Version="0.9.*" IncludeAssets="analyzers" />
  
  <!-- AdditionalFiles and GraphQlClientGenerator_ServiceUrl are mutually exclusive -->
  <!-- <AdditionalFiles Include="GqlSchemaTibberApi.gql.schema.json" CacheObjects="true" /> --> <!-- schema file name must end with ".gql.schema.json" -->
  
  <CompilerVisibleProperty Include="GraphQlClientGenerator_ServiceUrl" />
  <CompilerVisibleProperty Include="GraphQlClientGenerator_Namespace" />
  <!-- other configuration properties -->
  <!--<CompilerVisibleProperty Include="GraphQlClientGenerator_{ConfigurationProperty}" />-->
</ItemGroup>
```

Query builder usage
-------------
```csharp
var builder =
  new QueryQueryBuilder()
    .WithMe(
      new MeQueryBuilder()
        .WithAllScalarFields()
        .WithHome(
          new HomeQueryBuilder()
            .WithAllScalarFields()
            .WithSubscription(
              new SubscriptionQueryBuilder()
                .WithStatus()
                .WithValidFrom())
            .WithSignupStatus(
              new SignupStatusQueryBuilder().WithAllFields())
            .WithDisaggregation(
              new DisaggregationQueryBuilder().WithAllFields()),
          "b420001d-189b-44c0-a3d5-d62452bfdd42")
        .WithEnergyStatements ("2016-06", "2016-10"));

var query = builder.Build(Formatting.Indented);
```
results into
```graphql
query {
  me {
    id
    firstName
    lastName
    fullName
    ssn
    email
    language
    tone
    home (id: "b420001d-189b-44c0-a3d5-d62452bfdd42") {
      id
      avatar
      timeZone
      subscription {
        status
        validFrom
      }
      signupStatus {
        registrationStartedTimestamp
        registrationCompleted
        registrationCompletedTimestamp
        checkCurrentSupplierPassed
        supplierSwitchConfirmationPassed
        startDatePassed
        firstReadingReceived
        firstBillingDone
        firstBillingTimestamp
      }
      disaggregation {
        year
        month
        fixedConsumptionKwh
        fixedConsumptionKwhPercent
        heatingConsumptionKwh
        heatingConsumptionKwhPercent
        behaviorConsumptionKwh
        behaviorConsumptionKwhPercent
      }
    }
    energyStatements(from: "2016-06", to: "2016-10") 
  }
}
```

Mutation
-------------
```csharp
var mutation =
  new MutationQueryBuilder()
    .WithUpdateHome(
      new HomeQueryBuilder().WithAllScalarFields(),
      new UpdateHomeInput { HomeId = Guid.Empty, AppNickname = "My nickname", Type = HomeType.House, NumberOfResidents = 4, Size = 160, AppAvatar = HomeAvatar.Floorhouse1, PrimaryHeatingSource = HeatingSource.Electricity }
    )
    .Build(Formatting.Indented, 2);
```
result:
```graphql
mutation {
  updateHome (input: {
      homeId: "00000000-0000-0000-0000-000000000000"
      appNickname: "My nickname"
      appAvatar: FLOORHOUSE1
      size: 160
      type: HOUSE
      numberOfResidents: 4
      primaryHeatingSource: ELECTRICITY
    }) {
    id
    timeZone
    appNickname
    appAvatar
    size
    type
    numberOfResidents
    primaryHeatingSource
    hasVentilationSystem
  }
}
```

Field exclusion
-------------
Sometimes there is a need to select almost all fields of a queried object except few. In that case `Except` methods can be used often in conjunction with `WithAllFields` or `WithAllScalarFields`.
```csharp
new ViewerQueryBuilder()
  .WithHomes(
    new HomeQueryBuilder()
      .WithAllScalarFields()
      .ExceptPrimaryHeatingSource()
      .ExceptMainFuseSize()
  )
  .Build(Formatting.Indented);
```
result:
```graphql
query {
  homes {
    id
    timeZone
    appNickname
    appAvatar
    size
    type
    numberOfResidents
    hasVentilationSystem
  }
}

```

Aliases
-------------
Queried fields can be freely renamed to match target data classes using GraphQL aliases.
```csharp
new ViewerQueryBuilder("MyQuery")
  .WithHome(
    new HomeQueryBuilder()
      .WithType()
      .WithSize()
      .WithAddress(new AddressQueryBuilder().WithAddress1("primaryAddressText").WithCountry(), "primaryAddress"),
    Guid.NewGuid(),
    "primaryHome")
  .WithHome(
    new HomeQueryBuilder()
      .WithType()
      .WithSize()
      .WithAddress(new AddressQueryBuilder().WithAddress1("secondaryAddressText").WithCountry(), "secondaryAddress"),
    Guid.NewGuid(),
    "secondaryHome")
  .Build(Formatting.Indented);
```
result:
```graphql
query MyQuery {
  primaryHome: home (id: "120efe4a-6839-45fc-beed-27455d29212f") {
    type
    size
    primaryAddress: address {
      primaryAddressText: address1
      country
    }
  }
  secondaryHome: home (id: "0c735830-be56-4a3d-a8cb-d0189037f221") {
    type
    size
    secondaryAddress: address {
      secondaryAddressText: address1
      country
    }
  }
}
```

Query parameters
-------------
```csharp
var homeIdParameter = new GraphQlQueryParameter<Guid>("homeId", "ID", homeId);

var builder =
  new TibberQueryBuilder()
    .WithViewer(
      new ViewerQueryBuilder()
        .WithHome(new HomeQueryBuilder().WithAllScalarFields(), homeIdParameter)
    )
    .WithParameter(homeIdParameter);
```
result:
```graphql
query ($homeId: ID = "c70dcbe5-4485-4821-933d-a8a86452737b") {
  viewer{
    home(id: $homeId) {
      id
      timeZone
      appNickname
      appAvatar
      size
      type
      numberOfResidents
      primaryHeatingSource
      hasVentilationSystem
      mainFuseSize
    }
  }
}
```

Directives
-------------
```csharp
var includeDirectParameter = new GraphQlQueryParameter<bool>("direct", "Boolean", true);
var includeDirective = new IncludeDirective(includeDirectParameter);
var skipDirective = new SkipDirective(true);

var builder =
  new TibberQueryBuilder()
    .WithViewer(
       new ViewerQueryBuilder()
         .WithName(include: includeDirective)
         .WithAccountType(skip: skipDirective)
         .WithHomes(new HomeQueryBuilder().WithId(), skip: skipDirective)
    )
    .WithParameter(includeDirectParameter);
```
result:
```graphql
query (
  $direct: Boolean = true) {
  viewer {
    name @include(if: $direct)
    accountType @skip(if: true)
    homes @skip(if: true) {
      id
    }
  }
}
```

Inline fragments
-------------
```csharp
var builder =
  new RootQueryBuilder("InlineFragments")
    .WithUnion(
      new UnionTypeQueryBuilder()
        .WithConcreteType1Fragment(new ConcreteType1QueryBuilder().WithAllFields())
        .WithConcreteType2Fragment(new ConcreteType2QueryBuilder().WithAllFields())
        .WithConcreteType3Fragment(
          new ConcreteType3QueryBuilder()
            .WithName()
            .WithConcreteType3Field("alias")
            .WithFunction("my value", "myResult1")
        )
    )
    .WithInterface(
      new NamedTypeQueryBuilder()
        .WithName()
        .WithConcreteType3Fragment(
          new ConcreteType3QueryBuilder()
            .WithName()
            .WithConcreteType3Field()
            .WithFunction("my value")
        ),
      Guid.Empty
    );
```
result:
```graphql
query InlineFragments {
  union {
    __typename
    ... on ConcreteType1 {
      name
      concreteType1Field
    }
    ... on ConcreteType2 {
      name
      concreteType2Field
    }
    ... on ConcreteType3 {
      __typename
      name
      alias: concreteType3Field
      myResult1: function(value: "my value")
    }
  }
  interface(parameter: "00000000-0000-0000-0000-000000000000") {
    name
    ... on ConcreteType3 {
      __typename
      name
      concreteType3Field
      function(value: "my value")
    }
  }
}
```

Custom scalar types
-------------
GraphQL supports custom scalar types. By default these are mapped to `object` type. To ensure appropriate .NET types are generated for data class properties custom mapping interface can be used:

```csharp
var configuration = new GraphQlGeneratorConfiguration();
configuration.ScalarFieldTypeMappingProvider = new MyCustomScalarFieldTypeMappingProvider();

public class MyCustomScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
{
    public ScalarFieldTypeDescription GetCustomScalarFieldType(ScalarFieldTypeProviderContext context)
    {
        var unwrappedType = context.FieldType.UnwrapIfNonNull();

        return
            unwrappedType.Name switch
            {
                "Byte" => new ScalarFieldTypeDescription { NetTypeName = GenerationContext.GetNullableNetTypeName(context, "byte", false), FormatMask = null },
                "DateTime" => new ScalarFieldTypeDescription { NetTypeName = GenerationContext.GetNullableNetTypeName(context, "DateTime", false), FormatMask = null },
                _ => DefaultScalarFieldTypeMappingProvider.GetFallbackFieldType(context)
            };
    }
}
```

Generated class example:
```csharp
public class OrderType
{
    public DateTime? CreatedDateTimeUtc { get; set; }
    public byte? SomeSmallNumber { get; set; }
}
```

vs.

```csharp
public class OrderType
{
    public object CreatedDateTimeUtc { get; set; }
    public object SomeSmallNumber { get; set; }
}
```
### C# 9 source generator custom types

Source generator supports `RegexScalarFieldTypeMappingProvider` rules using JSON configuration file. Example:
```json
[
  {
    "patternBaseType": ".+",
    "patternValueType": ".+",
    "patternValueName": "^((timestamp)|(.*(f|F)rom)|(.*(t|T)o))$",
    "netTypeName": "DateTimeOffset",
    "isReferenceType": false,
    "formatMask": "O"
  }
]
```
All pattern values must be specified. `Null` values are not accepted.

The file must be named `RegexScalarFieldTypeMappingProvider.gql.config.json` and included as additional file.

```xml
<ItemGroup>
  <AdditionalFiles Include="RegexScalarFieldTypeMappingProvider.gql.config.json" CacheObjects="true" />
</ItemGroup>
```
