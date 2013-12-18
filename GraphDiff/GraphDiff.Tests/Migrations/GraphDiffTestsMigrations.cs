namespace RefactorThis.GraphDiff.Tests.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class GraphDiffTestsMigrations : DbMigrationsConfiguration<RefactorThis.GraphDiff.Tests.TestDbContext>
    {
        public GraphDiffTestsMigrations()
        {
            AutomaticMigrationsEnabled = true;
        }
    }
}
