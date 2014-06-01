GraphDiff
=========

DbContext extension methods for Entity Framework Code First, that allow you to save an entire detached Model/Entity, with child Entities and Lists, to the database without writing the code to do it.

**This version is for EF6+.** If you would like to use the project on EF(4.3,5.0) see this branch https://github.com/refactorthis/GraphDiff/tree/EF4-5.

Please see the initial post @ http://blog.brentmckendrick.com/introducing-graphdiff-for-entity-framework-code-first-allowing-automated-updates-of-a-graph-of-detached-entities/ for more information.

Nuget package is at http://nuget.org/packages/RefactorThis.GraphDiff/

## Realease Notes

2.0.0
 - Rewrite of graph traversal code and rewrite of tests to cover more scenarios.
 - multiple bug fixes

## Collaborators

Brent McKendrick, Andreas Pelzer

## License

See the LICENSE file for license rights and limitations (MIT).
