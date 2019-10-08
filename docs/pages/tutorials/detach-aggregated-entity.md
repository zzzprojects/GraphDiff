---
permalink: detach-aggregated-entity
---

## Definition

GraphDiff is capable of mapping changes from an aggregate root to the database. The aggregate root is a bunch of models which are handled by one unit when updating/adding/deleting.

For example, to add different associated and owned entities simultaneously to the parent entity.

{% include template-example.html %} 
{% highlight csharp %}
var associated = new OneToOneAssociatedModel { Title = "Associated Entity" };
var manyAssociated = new OneToManyAssociatedModel { Title = "Associated Collection Item 1" };
var node = new TestNode
{
    Title = "Parent Node",
    OneToManyOwned = new List<OneToManyOwnedModel>
    {
        new OneToManyOwnedModel { Title = "Owned Collection Item 1" },
        new OneToManyOwnedModel { Title = "Owned Collection Item 2" },
        new OneToManyOwnedModel { Title = "Owned Collection Item 3" }
    },
    OneToManyAssociated = new List<OneToManyAssociatedModel>
    {
        manyAssociated
    },
    OneToOneOwned = new OneToOneOwnedModel { Title = "Owned Entity" },
    OneToOneAssociated = associated
};

using (var context = new TestDbContext())
{
    context.OneToManyAssociatedModels.Add(manyAssociated);
    context.OneToOneAssociatedModels.Add(associated);
    context.SaveChanges();
} // Simulate detach

using (var context = new TestDbContext())
{
    // Setup mapping
    node = context.UpdateGraph(node, map => map
        .OwnedEntity(p => p.OneToOneOwned)
        .AssociatedEntity(p => p.OneToOneAssociated)
        .OwnedCollection(p => p.OneToManyOwned)
        .AssociatedCollection(p => p.OneToManyAssociated));

    context.SaveChanges();
}

{% endhighlight %}


