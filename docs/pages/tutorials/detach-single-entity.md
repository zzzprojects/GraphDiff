---
permalink: detach-single-entity
---

## Definition

GraphDiff merges an entire graph of detached entities into the database using **DbContext.UpdateGraph()**.

### Add Entity

Add a single detach entity to the database using DbContext.UpdateGraph method. This entity will be considered as a root entity.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode
{
    Title = "Hello"
};

using (var context = new TestDbContext())
{
    node = context.UpdateGraph(node);
    context.SaveChanges();
}

{% endhighlight %}

### Update Entity

Update a single detach entity to the database using DbContext.UpdateGraph method. The change is merged in another context by updating the entity graph.

{% include template-example.html %} 
{% highlight csharp %}
var node = new TestNode
{
    Title = "Hello"
};

using (var context = new TestDbContext())
{
    context.Nodes.Add(node);
    context.SaveChanges();
}

node.Title = "Updated Title";

using (var context = new TestDbContext())
{
    context.UpdateGraph(node);
    context.SaveChanges();
}

{% endhighlight %}
