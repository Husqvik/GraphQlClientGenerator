namespace GraphQlClientGenerator;

public static class IntrospectionQuery
{
    public const string OperationName = "IntrospectionQuery";

    public static string Get(bool includeAppliedDirectives) =>
        includeAppliedDirectives ? TextWithAppliedDirectives : Text;

    private const string Text =
        @"query IntrospectionQuery {
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
    }
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
  }";

    private const string TextWithAppliedDirectives =
        @"query IntrospectionQuery {
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
      appliedDirectives{
        ...AppliedDirective
      }
    }
  }

  fragment FullType on __Type {
    kind
    name
    appliedDirectives{
      ...AppliedDirective
    }
    description
    fields(includeDeprecated: true) {
      name
      description 
      appliedDirectives{
        ...AppliedDirective
      }
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
    }
  }
  
  fragment InputValue on __InputValue {
    name
    description
    type { ...TypeRef }
    defaultValue
    appliedDirectives{
      ...AppliedDirective
    }
  }

  fragment TypeRef on __Type {
    kind
    name
    appliedDirectives{
      ...AppliedDirective
    }
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

  fragment AppliedDirective on __AppliedDirective {
    name
    args {
      name
      value
    }
  }";
}