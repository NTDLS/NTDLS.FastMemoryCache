using System.Reflection;
using System.Runtime.InteropServices;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Various static functions for determining the size of .net objects.
    /// </summary>
    static public class Estimations
    {
        /// <summary>
        /// Estimates the amount of memory that would be consumed by a class instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        static public int ObjectSize(object? obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int totalSize = 0;

            var type = obj.GetType();

            var fieldsAndProperties = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fieldsAndProperties)
            {
                var fieldType = field.FieldType;
                var fieldValue = field.GetValue(obj);

                totalSize += ObjectFieldSize(fieldType, fieldValue);
            }

            return totalSize;
        }

        /// <summary>
        /// Estimates the amount of memory that would be consumed by a field in a class instance.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        static public int ObjectFieldSize(Type? type, object? obj)
        {
            if (type == null || obj == null)
            {
                return 0;
            }

            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    return sizeof(int);
                }
                else if (type.IsGenericType)
                {
                    return ObjectSize(obj);
                }
                else
                {
                    return Marshal.SizeOf(type);
                }
            }
            else if (type == typeof(string))
            {
                var stringValue = obj as string;
                return (stringValue?.Length ?? 0) * sizeof(char);
            }
            else if (type.IsArray)
            {
                int totalSize = 0;

                var array = obj as Array;
                if (array != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        var arrayValue = array.GetValue(i);
                        var arrayElementType = arrayValue?.GetType();

                        totalSize += ObjectFieldSize(arrayElementType, arrayValue);
                    }
                }

                return totalSize;
            }
            else
            {
                return ObjectSize(obj);
            }
        }
    }
}
