using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.InteropServices;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Various static functions for determining the size of .net objects.
    /// </summary>
    static public class Estimations
    {
        private static readonly MemoryCache _reflectionCache = new("Estimations:_reflectionCache");

        private static readonly CacheItemPolicy _slidingOneMinute = new()
        {
            SlidingExpiration = TimeSpan.FromSeconds(60)
        };

        /// <summary>
        /// Estimates the amount of memory that would be consumed by a class instance.
        /// </summary>
        static public int ObjectSize(object? obj)
            => ObjectSize(obj, new HashSet<object>(ReferenceEqualityComparer.Instance));

        private static int ObjectSize(object? obj, HashSet<object> visited)
        {
            if (obj == null || !visited.Add(obj))
            {
                return 0;
            }

            int totalSize = 0;

            var type = obj.GetType();

            var cacheKey = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            var fieldsAndProperties = (FieldInfo[])_reflectionCache.Get(cacheKey);
            if (fieldsAndProperties == null)
            {
                fieldsAndProperties = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _reflectionCache.Add(cacheKey, fieldsAndProperties, _slidingOneMinute);
            }

            foreach (var field in fieldsAndProperties)
            {
                var fieldType = field.FieldType;
                var fieldValue = field.GetValue(obj);

                totalSize += ObjectFieldSize(fieldType, fieldValue, visited);
            }

            return totalSize;
        }

        /// <summary>
        /// Estimates the amount of memory that would be consumed by a field in a class instance.
        /// </summary>
        static public int ObjectFieldSize(Type? type, object? obj)
            => ObjectFieldSize(type, obj, new HashSet<object>(ReferenceEqualityComparer.Instance));

        private static int ObjectFieldSize(Type? type, object? obj, HashSet<object> visited)
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
                    return ObjectSize(obj, visited);
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

                if (obj is Array array)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        var arrayValue = array.GetValue(i);
                        var arrayElementType = arrayValue?.GetType();

                        totalSize += ObjectFieldSize(arrayElementType, arrayValue, visited);
                    }
                }

                return totalSize;
            }
            else
            {
                return ObjectSize(obj, visited);
            }
        }
    }
}
