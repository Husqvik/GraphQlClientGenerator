GraphQL C# client generator
=======================

This simple console app generates C# GraphQL query builder and data classes for simple, compiler checked, usage of GraphQL API.

----------

Generator app usage
-------------

`GraphQlClientGenerator <GraphQlServiceUrl> <AccessToken> <TargetNamespace> <TargetFileName> <Namespace>`

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
