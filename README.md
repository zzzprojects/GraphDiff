GraphDiff
=========

DbContext extension methods for Entity Framework Code First, that allow you to save an entire detached Model/Entity, with child Entities and Lists, to the database without writing (much) extra code.

Please see http://refactorthis.wordpress.com/2012/12/11/introducing-graphdiff-for-entity-framework-code-first-allowing-automated-updates-of-a-graph-of-detached-entities/ for more information.

Nuget package is at http://nuget.org/packages/RefactorThis.GraphDiff/

# Developing

The Models used for testing will be emitted each time you run any tests, by dropping (if it exists) and recreating a database named GraphDiff in SQL Express, in the default directory for your SQL Express install (hard to find by default - see [here to set it to something simple](http://technet.microsoft.com/en-us/library/dd206993.aspx)).

## License

See the LICENSE file for license rights and limitations (MIT).
