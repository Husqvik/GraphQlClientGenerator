GraphQL C# client generator
=======================

This simple console app generates C# GraphQL query builder and data classes for simple, compiler checked, usage of GraphQL API.

----------

Generator app usage
-------------

`GraphQlClientGenerator <GraphQlServiceUrl> <TargetFileName> <TargetNamespace>`

Nuget package
-------------
Installation:
```
Install-Package GraphQlClientGenerator
```

Code example for class generation:
```
var schema = await GraphQlGenerator.RetrieveSchema(Url, token);

var builder = new StringBuilder();
GraphQlGenerator.GenerateQueryBuilder(schema, builder);
GraphQlGenerator.GenerateDataClasses(schema, builder);
	
var generatedClasses = builder.ToString();
```

Query builder usage
-------------
```
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
```
{
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
```
var mutation =
	new RootMutationQueryBuilder()
		.WithUpdateHome(
			new HomeQueryBuilder().WithAllScalarFields(),
			new UpdateHomeInput { HomeId = Guid.Empty, AppNickname = "My nickname", Type = HomeType.House, NumberOfResidents = 4, Size = 160, AppAvatar = HomeAvatar.Floorhouse1, PrimaryHeatingSource = HeatingSource.Electricity }
		)
	.Build(Formatting.Indented, 2);
```
result:
```
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
