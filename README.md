GraphDiff
=========

DbContext extension methods for Entity Framework Code First, that allow you to save an entire detached Model/Entity, with child Entities and Lists, to the database without writing the code to do it.

**This version is for EF6+.** If you would like to use the project on EF(4.3,5.0) see this branch https://github.com/refactorthis/GraphDiff/tree/EF4-5.

Please see the initial post @ https://refactorthis.wordpress.com/2012/12/11/introducing-graphdiff-for-entity-framework-code-first-allowing-automated-updates-of-a-graph-of-detached-entities/ for more information.

Nuget package is at http://nuget.org/packages/RefactorThis.GraphDiff/

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

## Collaborators

Brent McKendrick, Andreas Pelzer

## License

See the LICENSE file for license rights and limitations (MIT).
