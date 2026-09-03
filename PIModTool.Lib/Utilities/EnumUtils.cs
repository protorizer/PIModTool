using System.Diagnostics;

namespace PIModTool.Lib.Utilities
{
    public static class EnumUtils
    {
        // Originally wanted this to extend all enum classes e.g. MyEnum.TryConvertInt(i) but that doesn't seem to be possible
        public static T TryConvertInt<T>(int i) where T: struct, Enum{
            if(!Enum.IsDefined(typeof(T), i)){
                throw new ArgumentException($"Value {i} is not a valid member of enum {typeof(T).Name}.");
            }

            return (T)Enum.ToObject(typeof(T), i);
        }

        public static T TryConvertByte<T>(byte i) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), i))
            {
                throw new ArgumentException($"Value {i} is not a valid member of enum {typeof(T).Name}.");
            }

            return (T)Enum.ToObject(typeof(T), i);
        }

        public static T TryConvertUInt<T>(uint i) where T : struct, Enum
        {
            try
            {
                if (!Enum.IsDefined(typeof(T), i))
                {
                    throw new ArgumentException($"Value {i} is not a valid member of enum {typeof(T).Name}.");
                }

                return (T)Enum.ToObject(typeof(T), i);
            }
            catch (Exception e)
            {
                Debug.Fail(e.Message);
                return (T)Enum.GetValues(typeof(T)).GetValue(0); // Failsafe value so the whole program doesn't crash
            }
        }
    }
}
