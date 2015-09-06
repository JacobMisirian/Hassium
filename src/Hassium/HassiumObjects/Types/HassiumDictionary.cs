﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hassium.Functions;

namespace Hassium.HassiumObjects.Types
{
    public class HassiumDictionary: HassiumObject, IEnumerable
    {
        public List<HassiumKeyValuePair> Value { get; private set; }

        public HassiumDictionary(Dictionary<HassiumObject, HassiumObject> value) : this(value.Select(x => (HassiumKeyValuePair)x).ToList())
        {
            Attributes.Add("length", new InternalFunction(x => Value.Count, true));
            Attributes.Add("toString", new InternalFunction(tostring));

            Attributes.Add("resize", new InternalFunction(ResizeArr));
            Attributes.Add("reverse", new InternalFunction(ArrayReverse));
            Attributes.Add("contains", new InternalFunction(ArrayContains));
            Attributes.Add("containsKey", new InternalFunction(ContainsKey));
            Attributes.Add("containsValue", new InternalFunction(ContainsValue));

            Attributes.Add("op", new InternalFunction(ArrayOp));
            Attributes.Add("select", new InternalFunction(ArraySelect));
            Attributes.Add("where", new InternalFunction(ArrayWhere));
            Attributes.Add("any", new InternalFunction(ArrayAny));
            Attributes.Add("first", new InternalFunction(ArrayFirst));
            Attributes.Add("last", new InternalFunction(ArrayLast));
            Attributes.Add("zip", new InternalFunction(ArrayZip));
        }

        public HassiumDictionary(List<HassiumKeyValuePair> ls)
        {
            Value = ls;
        }

        public HassiumObject ContainsKey(HassiumObject[] args)
        {
            return Value.Any(x => x.Key.ToString() == args[0].ToString());
        }

        public HassiumObject ContainsValue(HassiumObject[] args)
        {
            return Value.Any(x => x.Value.ToString() == args[0].ToString());
        }

        public HassiumObject this[HassiumObject key]
        {
            get { return Value.First(x => x.Key.ToString() == key.ToString()).Value; }
            set
            {
                if (Value.Any(x => x.Key.ToString() == key.ToString()))
                    Value =
                        Value.Select(x => x.Key.ToString() == key.ToString() ? new HassiumKeyValuePair(key, value) : x)
                            .ToList();
                else
                    Value.Add(new HassiumKeyValuePair(key, value));
            }
        }

        public HassiumObject ResizeArr(HassiumObject[] args)
        {
            HassiumObject[] objarr = Value.ToArray();

            HassiumObject[] newobj = new HassiumObject[objarr.Length + args[0].HDouble().ValueInt - 1];

            for (int x = 0; x < objarr.Length; x++)
                newobj[x] = objarr[x];

            return newobj;
        }

        public override string ToString()
        {
            return "Dictionary { " +
                   string.Join(", ", Value.Select(x => "[" + x.Key.ToString() + "] => " + x.Value.ToString())) + " }";
        }

        public HassiumDictionary(Dictionary<object, object> value)
            : this(value.Select(x => new HassiumKeyValuePair((HassiumObject) x.Key, (HassiumObject) x.Value)).ToList())
        {
        }

        public HassiumDictionary(IDictionary value) : this(value.Keys.Cast<object>()
                    .Zip(value.Values.Cast<object>(), (a, b) => new KeyValuePair<object, object>(a, b))
                    .Select(x => new HassiumKeyValuePair((HassiumObject)x.Key, (HassiumObject)x.Value)).ToList())
        {
        }
        public IEnumerator GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        public Dictionary<HassiumObject, HassiumObject> ToDictionary()
        {
            return Value.ToDictionary(x => x.Key, x => x.Value);
        }

        private HassiumObject tostring(HassiumObject[] args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (HassiumKeyValuePair obj in Value)
            {
                sb.Append(obj.ToString());
            }

            return sb.ToString();
        }

        public HassiumObject ArrayReverse(HassiumObject[] args)
        {
            Value.Reverse();
            return this;
        }

        public HassiumObject ArrayOp(HassiumObject[] args)
        {
            return Value.Aggregate((a, b) => (HassiumKeyValuePair)args[0].Invoke(a, b));
        }

        #region LINQ-like functions

        public HassiumObject ArraySelect(HassiumObject[] args)
        {
            return Value.Select(x => args[0].Invoke(x)).ToArray();
        }

        public HassiumObject ArrayWhere(HassiumObject[] args)
        {
            return Value.Where(x => args[0].Invoke(x)).ToArray();
        }

        public HassiumObject ArrayAny(HassiumObject[] args)
        {
            return Value.Any(x => args[0].Invoke(x));
        }

        public HassiumObject ArrayFirst(HassiumObject[] args)
        {
            return args.Length == 1 ? Value.First(x => args[0].Invoke(x)) : Value.First();
        }

        public HassiumObject ArrayLast(HassiumObject[] args)
        {
            return args.Length == 1 ? Value.Last(x => args[0].Invoke(x)) : Value.Last();
        }

        public HassiumObject ArrayContains(HassiumObject[] args)
        {
            return Value.Contains(args[0]);
        }

        public HassiumObject ArrayZip(HassiumObject[] args)
        {
            return Value.Zip(args[0].HArray().Value, (x, y) => args[1].Invoke(x, y)).ToArray();
        }

        #endregion
    }

    public class HassiumKeyValuePair : HassiumObject, IConvertible
    {
        public HassiumObject Key { get; set; }
        public HassiumObject Value { get; set; }

        public HassiumKeyValuePair(HassiumObject k, HassiumObject v)
        {
            Key = k;
            Value = v;

            Attributes.Add("key", new InternalFunction(x => Key, true));
            Attributes.Add("value", new InternalFunction(x => Value, true));
        }

        public static implicit operator KeyValuePair<HassiumObject, HassiumObject>(HassiumKeyValuePair kvp)
        {
            return new KeyValuePair<HassiumObject, HassiumObject>(kvp.Key, kvp.Value);
        }

        public static implicit operator HassiumKeyValuePair(KeyValuePair<HassiumObject, HassiumObject> kvp)
        {
            return new HassiumKeyValuePair(kvp.Key, kvp.Value);
        }

        public override string ToString()
        {
            return "[" + Key + " => " + Value + "]";
        }

        #region IConvertible stuff
        public TypeCode GetTypeCode()
        {
            return TypeCode.Single;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean((object)Value);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte((object)Value);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar((object)Value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime((object)Value);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal((object)Value);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble((object)Value);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16((object)Value);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32((object)Value);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64((object)Value);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte((object)Value);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle((object)Value);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(Value, conversionType);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16((object)Value);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32((object)Value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64((object)Value);
        }
        #endregion
    }
}

