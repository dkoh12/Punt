namespace punt
{
    public class Value
    {
        public Type type;
        public DataUnion val;

        // Create a New Value
        public Value(Type type, DataUnion val)
        {
            this.type = type; // valueType
            this.val = val; // value
        }

        public enum Type
        {
            Bool,
            Number,
            Nil,
            String,
            Function,
        }


        // can do some layout magic with [StructLayout(LayoutKind.Explicit)]
        public struct DataUnion
        {
            // [FieldOffset(0)]
            public bool Bool;

            // [FieldOffset(0)]
            public double Number;

            // [FieldOffset(0)]
            public string String;

            // [FieldOffset(0)]
            // public Func<> Function;

            // public System.Func<VM, int, Value> NativeFunc
        }

        public override string ToString()
        {
            switch (type)
            {
                case Type.Bool:
                    return val.Bool.ToString();
                case Type.Nil:
                    return "nil";
                case Type.Number:
                    return val.Number.ToString();
                case Type.String:
                    return val.String.ToString();
                case Type.Function:
                    return "<fn> name"; // todo change this to function name.
                default:
                    throw new System.NotImplementedException();
            }
        }

        public static bool valuesEqual(Value a, Value b)
        {
            if (a.type != b.type) return false;
            switch (a.type)
            {
                case Type.Bool:   return asBool(a) == asBool(b);
                case Type.Nil:    return true;
                case Type.Number: return asNumber(a) == asNumber(b);
                case Type.String: return asString(a) == asString(b);
                default: return false;
            }
        }


        #region C# value -> Punt Value
        public static Value boolVal(bool value) => new Value(Type.Bool, new DataUnion() { Bool = value });
        public static Value nilVal() => new Value(Type.Nil, new DataUnion() { Number = 0 });
        public static Value numberVal(double value) => new Value(Type.Number, new DataUnion() { Number = value });
        public static Value stringVal(string value) => new Value(Type.Number, new DataUnion() { String = value });

        #endregion

        #region Punt Value -> C# value
        public static bool asBool(Value value) => value.val.Bool;
        public static double asNumber(Value value) => value.val.Number;
        public static string asString(Value value) => value.val.String;
        // public static Func<> asFunc(Value value) => value.val.Function;

        #endregion

        #region Punt Value == C# value
        public static bool isBool(Value value) => value.type == Type.Bool;
        public static bool isNil(Value value) => value.type == Type.Nil;
        public static bool isNumber(Value value) => value.type == Type.Number;
        public static bool isString(Value value) => value.type == Type.String;
        public static bool isFunction(Value value) => value.type == Type.Function;

        #endregion
    }
}
