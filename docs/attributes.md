## Attributes

VinylCutter supports a number of C# constructs and custom attributes to allow customization of the generated code.

By default, the following parts of your record definition in C# will be transfered over to the generated code:

- struct vs class
- public vs internal
- Both properties and fields will be made record fields
- List<T> will be convereted to IEnumerable<T> and stored as ImmutableArray<T>
- The namespace of the first record type, if any, will be used in the generated output
- Interfaces and base types will be transfered to the generated code.
   - This may require "subbing" in the interface in your definition: `interface IFoo {}`

There are currently five attributes that you attach to your definitions:

- **[Default]** will generate constructors with default values for those fields.
   - **Note:** The text in default will be directly inserted into the generated code. `[Default ("")]` is special cased to be empty string, but other strings will need to be quoted.
   - **Note:** Fields in records are generated in order defined, so all **[Default]** items must be the last fields defined or the generated code will fail to compile.
- **[Inject]** will insert the attached item directly in the generated code. This can be done both inside record classes at at the top level (useful for Enums for example).
- **[Skip]** on type will prevent it from being added to the generated output.
- **[With]** will add associated WithFoo methods to construct new instances with modifications.
	- If placed on the class or struct, it will apply to all fields of the record
	- If placed on specific fields\properties it will apply only to those specific items.
- **[Mutable]** on field will create a private field that is not set by the constructor, and copied directly to new instances
