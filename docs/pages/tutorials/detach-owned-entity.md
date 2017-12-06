---
permalink: detach-owned-entity
---

## Definition

An owned entity can be described as **being a part of**, when updating a graph then GraphDiff also changes owned entities with its owner.

 - The child entity is a part of the aggregate and will be updated, added or removed if changed in the parent's navigational property. 
 - Owned entity or a collection of owned entities must be specified in the **OwnedEntity()** or **OwnedCollection()** method respectively.

## Add Entity to Parent

GraphDiff adds the owned entity to the parent entity when entity graph is updated.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode { Title = "Parent Node" };

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
} 

// Simulate detach
node.OneToOneOwned = new OneToOneOwnedModel { Title = "Owned Entity" };
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .OwnedEntity(p => p.OneToOneOwned));

    context.SaveChanges();
}

{% endhighlight %}

Similarly, a collection of owned entities can also be added to the parent entity by using the **OwnedCollection()** method.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode { Title = "Parent Node" };

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
}

// Simulate detach
node.OneToManyOwned = new List<OneToManyOwnedModel>
{
    new OneToManyOwnedModel { Title = "Owned Entity 1" },
    new OneToManyOwnedModel { Title = "Owned Entity 2" },
    new OneToManyOwnedModel { Title = "Owned Entity 3" }
};
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .OwnedCollection(p => p.OneToManyOwned));

    context.SaveChanges();
}

{% endhighlight %}
 

## Update Owned Entity

GraphDiff updates the owned entity with the parent entity when entity graph is updated.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode
{
    Title = "Parent Node",
    OneToOneOwned = new OneToOneOwnedModel
    {
        Title = "Owned Entity"
    } 
};

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
} // Simulate detach

node.OneToOneOwned.Title = "Updated Owned Entity";

using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .OwnedEntity(p => p.OneToOneOwned));

    context.SaveChanges();
}

{% endhighlight %}

## Remove Owned Entity

GraphDiff removes the owned entity from parent entity as well as from the database itself when entity graph is updated.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode
{
    Title = "Parent Node",
    OneToOneOwned = new OneToOneOwnedModel
    {
        Title = "Owned Entity"
    }
};

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
} 

// Simulate detach
node.OneToOneOwned = null;
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .OwnedEntity(p => p.OneToOneOwned));

    context.SaveChanges();
}
{% endhighlight %}

## Nested Owned Entity

GraphDiff can also handle nested owned entity by specifying it with nested **OwnedEntity** method.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode { Title = "Parent Node" };

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
}

// Simulate detach
node.OneToOneOwned = new OneToOneOwnedModel
{
    Title = "Owned Entity",
    OneToOneOneToOneOwned = new OneToOneOneToOneOwnedModel
    {
        Title = "Nested Owned Entity"
    }
};
using (var context = new TestDbContext())
{
    // Setup mapping
    context.UpdateGraph(node, map => map
        .OwnedEntity(p => p.OneToOneOwned, with => with
            .OwnedEntity(o => o.OneToOneOneToOneOwned)));

    context.SaveChanges();
}

{% endhighlight %}
