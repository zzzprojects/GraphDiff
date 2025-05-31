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

Read more on our [Website](https://entityframework-graphdiff.net/overview).

## Downloads

### EF Core

[![nuget](https://img.shields.io/nuget/v/RefactorThis.GraphDiff?logo=nuget&style=flat-square)](https://www.nuget.org/packages/RefactorThis.GraphDiff)
[![nuget](https://img.shields.io/nuget/dt/RefactorThis.GraphDiff?logo=nuget&style=flat-square)](https://www.nuget.org/packages/RefactorThis.GraphDiff)

```
PM> NuGet\Install-Package RefactorThis.GraphDiff
```

```
> dotnet add package RefactorThis.GraphDiff
```

## Sponsors

ZZZ Projects owns and maintains **GraphDiff** as part of our [mission](https://zzzprojects.com/mission) to add value to the .NET community

Through [Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=zzzprojects&utm_medium=graphdiff) and [Dapper Plus](https://dapper-plus.net/?utm_source=zzzprojects&utm_medium=graphdiff), we actively sponsor and help key open-source libraries grow.

[![Entity Framework Extensions](https://raw.githubusercontent.com/zzzprojects/GraphDiff/master/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert?utm_source=zzzprojects&utm_medium=graphdiff)

[![Dapper Plus](https://raw.githubusercontent.com/zzzprojects/GraphDiff/master/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert?utm_source=zzzprojects&utm_medium=graphdiff)

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
