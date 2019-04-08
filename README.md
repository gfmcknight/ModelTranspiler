# Model Transpiler
This project is meant to a transpiler which converts C#
classes to TypeScript classes in order to make maintaining
back/front models consistent.

## Roadmap
- [ ] Conversions for accessors when they have the public keyword
- [ ] Constructors that take a JSON object that would come from a .NET Core app
- [ ] Sensitivity to JsonProperty and JsonIgnore
- [ ] Take a project and transpile all classes with a given annotation
- [ ] RPC procedure for methods, getters/setters with code
- [ ] Some sort of library for RPC binding on the server side
- [ ] Transpile simple methods and 
