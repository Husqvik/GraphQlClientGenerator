﻿namespace GraphQlClientGenerator;

public static class GraphQlIntrospection
{
    public const string QuerySupportedDirectives =
        """
        query DirectiveIntrospection {
          __schema {
            directives {
              name
            }
          }
        }
        """; 

        public static string QuerySchemaMetadata(GraphQlWellKnownDirective directive) =>
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
            args {
              ...InputValue
            }
            type {
              ...TypeRef
            }
            isDeprecated
            deprecationReason
          }
          inputFields {
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
          }{{(directive.HasFlag(GraphQlWellKnownDirective.OneOf) ? $"{Environment.NewLine}isOneOf" : null)}}
        }
        
        fragment InputValue on __InputValue {
          name
          description
          type { ...TypeRef }
          defaultValue
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
}

[Flags]
public enum GraphQlWellKnownDirective
{
    None = 0,
    OneOf = 1
}