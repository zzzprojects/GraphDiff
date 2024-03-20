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

The best way to contribute is by **spreading the word** about the library:

 - Blog it
 - Comment it
 - Star it
 - Share it
 
A **HUGE THANKS** for your help.

## More Projects

- Projects:
   - [EntityFramework Extensions](https://entityframework-extensions.net/)
   - [Dapper Plus](https://dapper-plus.net/)
   - [C# Eval Expression](https://eval-expression.net/)
- Learn Websites
   - [Learn EF Core](https://www.learnentityframeworkcore.com/)
   - [Learn Dapper](https://www.learndapper.com/)
- Online Tools:
   - [.NET Fiddle](https://dotnetfiddle.net/)
   - [SQL Fiddle](https://sqlfiddle.com/)
   - [ZZZ Code AI](https://zzzcode.ai/)
- and much more!

To view all our free and paid projects, visit our website [ZZZ Projects](https://zzzprojects.com/).
