## v2.4.0 (minor)

Changes since v2.3.0:

- Move the IEnumerable clone tests into their own file ([@Claude](https://github.com/Claude))
- Clone the items when IEnumerable.DeepClone() is called, not on each enumeration ([@Claude](https://github.com/Claude))
- Let concurrent and read-only dictionaries call DeepClone() without a cast ([@Claude](https://github.com/Claude))

