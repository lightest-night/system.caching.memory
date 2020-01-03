using System;
using System.Collections.Generic;
using System.Linq;

namespace Caching.Memory.Tests
{
    public class TestObject : IEquatable<TestObject>
    {
        public string? Property1 { get; }
        public string? Property2 { get; }
        public int Property3 { get; }

        public List<string> List { get; }

        public TestObject(string? property1, string? property2, int property3, List<string> list)
        {
            Property1 = property1;
            Property2 = property2;
            Property3 = property3;
            List = list;
        }

        public override bool Equals(object? obj)
            => Equals((TestObject?) obj);

        public bool Equals(TestObject? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            var equals = Property1 == other.Property1 && Property2 == other.Property2 && Property3 == other.Property3;
            return equals && List.All(item => other.List.Contains(item));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Property1, Property2, Property3, List);
        }
    }
}