namespace GraphQlClientGenerator;

public static class GraphQlIntrospection
{
    public const string QuerySupportedFieldArgs =
        """
        {
          typeMetadata: __type(name: "__Type") {
            fields {
              name
              args {
                name
                type { kind name }
              }
            }
          }
        }
        """;

    public static string QuerySchemaMetadata(bool hasOneOfSupport, bool hasDeprecatedInputFieldSupport) =>
        $$"""
        query FullIntrospection {
          __schema {
            queryType { name }
            mutationType { name }
            subscriptionType { name }
            types {
              ...FullType
            }
            directives {
              name
              description
              locations
              args {
                ...InputValue
              }
            }
          }
        }

        fragment FullType on __Type {
          kind
          name
          description
          fields(includeDeprecated: true) {
            name
            description
            args{{InsertIfTrue(hasDeprecatedInputFieldSupport, "(includeDeprecated: true)")}} {
              ...InputValue
            }
            type {
              ...TypeRef
            }
            isDeprecated
            deprecationReason
          }
          inputFields{{InsertIfTrue(hasDeprecatedInputFieldSupport, "(includeDeprecated: true)")}} {
            ...InputValue
          }
          interfaces {
            ...TypeRef
          }
          enumValues(includeDeprecated: true) {
            name
            description
            isDeprecated
            deprecationReason
          }
          possibleTypes {
            ...TypeRef
          }{{InsertIfTrue(hasOneOfSupport, $"{Environment.NewLine}isOneOf")}}
        }

        fragment InputValue on __InputValue {
          name
          description
          type { ...TypeRef }
          defaultValue{{InsertIfTrue(hasDeprecatedInputFieldSupport, $"{Environment.NewLine}isDeprecated{Environment.NewLine}deprecationReason")}}
        }

        fragment TypeRef on __Type {
          kind
          name
          ofType {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                  ofType {
                  kind
                  name
                  ofType {
                    kind
                    name
                    ofType {
                      kind
                      name
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private static string InsertIfTrue(bool use, string text) => use ? text : null;
}