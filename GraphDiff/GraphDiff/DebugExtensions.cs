using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace RefactorThis.GraphDiff.Internal
{
    public static class DebugExtensions
    {
        public static string DumpTrackedEntities(this DbContext context)
        {
            var trackedEntities = context
                    .ChangeTracker
                    .Entries()
                    .Where(t => t.State != EntityState.Detached && t.Entity != null)
                    .Select(t => new
                    {
                        t.State,
                        EntityType = t.Entity.GetType(),
                        Original = t.State != EntityState.Added
                                ? t.OriginalValues.PropertyNames.ToDictionary(pn => pn, pn => t.OriginalValues[pn])
                                : new Dictionary<string, object>(),
                        Current = t.State != EntityState.Deleted
                                ? t.CurrentValues.PropertyNames.ToDictionary(pn => pn, pn => t.CurrentValues[pn])
                                : new Dictionary<string, object>(),
                        HashCode = t.Entity.GetHashCode()
                    })
                    .OrderBy(e => e.State).ThenBy(e => e.EntityType.Name)
                    .ToList();

            var builder = new StringBuilder();

            EntityState? previousState = null;
            foreach (var entity in trackedEntities)
            {
                if (entity.State != previousState)
                {
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.AppendLine(entity.State.ToString());
                    builder.AppendLine("----------");
                }
                previousState = entity.State;

                builder.AppendFormat("{0} (# {1})", entity.EntityType.Name, entity.HashCode).AppendLine();

                bool outerIsOriginal = entity.Original.Count >= entity.Current.Count;
                Dictionary<string, object> outer = outerIsOriginal ? entity.Original : entity.Current;
                Dictionary<string, object> inner = outerIsOriginal ? entity.Current : entity.Original;

                var propertyValues = from fd in outer
                    join pd in inner on fd.Key equals pd.Key into joinedT
                    from pd in joinedT.DefaultIfEmpty()
                    select new
                    {
                        fd.Key,
                        OriginalValue = outerIsOriginal ? fd.Value : pd.Value,
                        CurrentValue = outerIsOriginal ? pd.Value : fd.Value
                    };

                foreach (var propertyValue in propertyValues)
                {
                    switch (entity.State)
                    {
                        case EntityState.Added:
                            builder.AppendFormat("  {0}: '{1}'", propertyValue.Key, propertyValue.CurrentValue).AppendLine();
                            break;
                        case EntityState.Deleted:
                            builder.AppendFormat("  {0}: '{1}'", propertyValue.Key, propertyValue.OriginalValue).AppendLine();
                            break;
                        default:
                            builder.AppendFormat("  {0}: changed from '{1}' to '{2}'", propertyValue.Key, propertyValue.OriginalValue, propertyValue.CurrentValue).AppendLine();
                            break;
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}