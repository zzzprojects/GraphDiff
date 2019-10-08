---
permalink: detach-associated-entity
---

## Definition

GraphDiff can handle associated entities, when updating an entity graph then associated entities are not changed by GraphDiff.

 - The child entity is not a part of the aggregate. The parent's navigation property will be updated, but changes to the child will not be saved.
 - Only the relation is added, but any changes made to the associated entity will not be updated.
 - Associated entity or a collection of associated entities must be specified in the **AssociatedEntity()** or **AssociatedCollection()** method respectively.

## Add Associated Entity

GraphDiff adds the relation of an associated entity to the parent entity when entity graph is updated.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode { Title = "Parent Node" };

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
}

// Simulate detach
node.OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Entity" };
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .AssociatedEntity(p => p.OneToOneAssociated));

    context.SaveChanges();
}

{% endhighlight %}

Similarly, the relation of a collection of associated entities can also be added to the parent entity by using the **AssociatedCollection()** method.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode { Title = "Parent Node" };

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
}

// Simulate detach
node.OneToManyAssociated = new List<OneToManyAssociatedModel>
{
    new OneToManyAssociatedModel { Title = "Associated Entity 1" },
    new OneToManyAssociatedModel { Title = "Associated Entity 2" },
    new OneToManyAssociatedModel { Title = "Associated Entity 3" }
};
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .AssociatedCollection(p => p.OneToManyAssociated));

    context.SaveChanges();
}

{% endhighlight %}

## Change Associated Entity

Any change in an associated entity will not be saved with parent's navigation property. 

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode
{
    Title = "Parent Node",
    OneToOneAssociated = new OneToOneAssociatedModel
    {
        Title = "Associated Entity"
    }
};

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
} // Simulate detach

node.OneToOneAssociated.Title = "Updated Associated Entity";
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .AssociatedEntity(p => p.OneToOneAssociated));

    context.SaveChanges();
}

{% endhighlight %}

In the above example, the Title of associated entity is updated, but **GraphDiff** will not save the updated Title when entity graph is updated.
