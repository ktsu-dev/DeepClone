## v2.8.0 (minor)

Changes since v2.7.0:

- [patch] Refuse a DeepCloneFrom between OrderedDictionaries or ConcurrentDictionaries with different comparers (#145) (matt-edmondson)
- Clone value-type key-value pairs under NativeAOT without a type initializer crash [minor] (#147) (matt-edmondson)
- Clone a collection subclass that implements IDeepCloneable by its own implementation in every overload [patch] (#150) (matt-edmondson)

