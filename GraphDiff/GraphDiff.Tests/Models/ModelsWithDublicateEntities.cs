using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RefactorThis.GraphDiff.Tests.Models
{
    public class ModelRoot
    {
        public Guid Id { get; set; }
        public virtual ICollection<ModelLevel1> MyModelsLevel1 { get; set; }
    }

    public class ModelLevel1
    {
        protected bool Equals(ModelLevel1 other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModelLevel1) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public Guid Id { get; set; }

        public virtual ModelLevel2 ModelLevel2 { get; set; } 
    }

    public class ModelLevel2
    {
        protected bool Equals(ModelLevel2 other)
        {
            return Code.Equals(other.Code);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModelLevel2) obj);
        }

        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }

        public Guid Code { get; set; }
        public string Name { get; set; }
    }
}
