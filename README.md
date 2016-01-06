# Equality
The last .NET equality solution you'll ever need. Automatically produces equality comparison and hash-code generation for any type by emitting IL based on your type. Emitted code is cached and specialized for struct and class types. Specify fields and auto-properties to ignore, as well as properties you want to include by applying attributes.

###TODO

- ~~Handle enumerables~~
- 'Fluent'-style API for configuring fields to include/exclude
- Fody tool to IL-weave the overrides to call the respective 'Equality' methods