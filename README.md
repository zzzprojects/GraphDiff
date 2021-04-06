## Library Powered By

This library is powered by [Entity Framework Extensions](https://entityframework-extensions.net/?z=github&y=graphdiff)

<a href="https://entityframework-extensions.net/?z=github&y=graphdiff">
<kbd>
<img src="https://zzzprojects.github.io/images/logo/entityframework-extensions-pub.jpg" alt="Entity Framework Extensions" />
</kbd>
</a>

# What's GraphDiff?
GraphDiff is a DbContext extension methods for Entity Framework Code First, that allow you to save an entire detached Model/Entity, with child Entities and Lists, to the database without writing the code to do it.

**This version is for EF6+.** If you would like to use the project on EF(4.3,5.0) see this branch https://github.com/refactorthis/GraphDiff/tree/EF4-5.

Please see the initial post @ https://refactorthis.wordpress.com/2012/12/11/introducing-graphdiff-for-entity-framework-code-first-allowing-automated-updates-of-a-graph-of-detached-entities/ for more information.

## Features

 - Merge an entire graph of detached entities to the database using DbContext.UpdateGraph<T>();
 - Ensures concurrency is maintained for all child entities in the graph
 - Allows for different configuration mappings to ensure that only changes within the defined graph are persisted
 - Comprehensive testing suite to cover many (un/)common scenarios.
 
## Proposed Features

 - Fluent API style mapping of aggregates on bootstrapping
 - Retrieve an aggregate from the database without specifying include expressions
 - Define the aggregate using attributes on the models
 - Allow for the initial db query to be performed as multiple queries where needed (too many includes, etc)

## Release Notes

2.0.1
 - Rewrite of graph traversal code and rewrite of tests to cover more scenarios.
 - multiple bug fixes

## Useful links

- [Website](https://entityframework-graphdiff.net/overview)
- [KnowledgeBase](https://entityframework-graphdiff.net/knowledge-base)
- [Online Examples](https://entityframework-graphdiff.net/online-examples) 
- [NuGet](https://www.nuget.org/packages/RefactorThis.GraphDiff/)
- You can also consult GraphDiff questions on 
[Stack Overflow](https://stackoverflow.com/questions/tagged/graphdiff)

## Contribute

Want to help us? Your donation directly helps us maintain and grow ZZZ Free Projects. 

We can't thank you enough for your support 🙏.

👍 [One-time donation](https://zzzprojects.com/contribute)

❤️ [Become a sponsor](https://github.com/sponsors/zzzprojects) 

### Why should I contribute to this free & open-source library?
We all love free and open-source libraries! But there is a catch... nothing is free in this world.

We NEED your help. Last year alone, we spent over **3000 hours** maintaining all our open source libraries.

Contributions allow us to spend more of our time on: Bug Fix, Development, Documentation, and Support.

### How much should I contribute?
Any amount is much appreciated. All our free libraries together have more than **100 million** downloads.

If everyone could contribute a tiny amount, it would help us make the .NET community a better place to code!

Another great free way to contribute is  **spreading the word** about the library.

A **HUGE THANKS** for your help!

## More Projects

- [EntityFramework Extensions](https://entityframework-extensions.net/)
- [Dapper Plus](https://dapper-plus.net/)
- [C# Eval Expression](https://eval-expression.net/)
- and much more! 

To view all our free and paid projects, visit our [website](https://zzzprojects.com/).
