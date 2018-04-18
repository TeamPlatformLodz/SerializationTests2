using Shop.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop
{
    /// <summary>
    /// Represents percentage values to separate them from other numerics.
    /// 
    /// </summary>
    public class Percentage : IEquatable<Percentage>
    {
        private float value;

        public float Value
        {
            get => value;
            set
            {
                if (value >= 0 && value <= 100)
                {
                    this.value = value;
                }
                else
                    throw new PercentageException($"Cannot set {value} as percentage value");
            }
        }
        
        public Percentage(float percents) 
        {
            Value = percents;
        }

        public Percentage(int percents) 
        {
            Value = percents;
        }

        /// <summary>
        /// Returns value%. E.g. "73%"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Value}%";
        }

        public override int GetHashCode()
        {
            var hashCode = 1927018180;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + value.GetHashCode();
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Percentage p1, Percentage p2)
        {
            if (null == p1)
                return (null == p2);

            return p1.Equals(p2);
        }

        public static bool operator !=(Percentage p1, Percentage p2)
        {
            if (null == p1)
                return (null != p2);

            return ! p1.Equals(p2);
        }

        public bool Equals(Percentage other)
        {
            return value == other.value &&
                  Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
