using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VicTool.Main.Misc
{
    public struct ValuePair<T> : IEquatable<ValuePair<T>>
    {
        public T ObjectA { get; set; }
        public T ObjectB { get; set; }

        public ValuePair(T objectA, T objectB)
        {
            ObjectA = objectA;
            ObjectB = objectB;
        }

        public static bool operator ==(ValuePair<T> pairA, ValuePair<T> pairB)
        {
            return pairA.Equals(pairB);
        }

        public static bool operator !=(ValuePair<T> pairA, ValuePair<T> pairB)
        {
            return !pairA.Equals(pairB);
        }

        public bool Equals(ValuePair<T> other)
        {
            return (Equals(ObjectA, other.ObjectA) && Equals(ObjectB, other.ObjectB)) || (Equals(ObjectA, other.ObjectB) && Equals(ObjectB, other.ObjectA));
        }

        public override bool Equals(object obj)
        {
            return obj is ValuePair<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ObjectA != null ? ObjectA.GetHashCode() : 0) * 397) ^ (ObjectB != null ? ObjectB.GetHashCode() : 0);
            }
        }
    }
}
