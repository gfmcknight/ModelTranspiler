# Model Transpiler
This project is meant to a transpiler which converts C#
classes to TypeScript classes in order to make maintaining
back/front models consistent.

## Roadmap
- [X] Conversions for accessors when they have the public keyword
- [X] Constructors that take a JSON object that would come from a .NET Core app
- [X] Sensitivity to JsonProperty and JsonIgnore
- [X] Take a project and transpile all classes with a given annotation
- [X] Directories that mirror namespaces
- [X] A toJson() function on models to serialize to the server
- [X] Support for C# types: double, int, string, bool, Guid, DateTime
- [X] Models that hold other models
- [ ] RPC procedure for methods, getters/setters with code
- [ ] Some sort of library for RPC binding on the server side
- [ ] Transpile simple methods and getters/setters
