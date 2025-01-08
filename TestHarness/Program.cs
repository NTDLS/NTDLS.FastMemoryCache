using NTDLS.FastMemoryCache;
using System.Text;

namespace TestHarness
{
    internal class Program
    {
        static readonly PartitionedMemoryCache _cache = new(new PartitionedCacheConfiguration()
        {
            IsCaseSensitive = true,
            SizeLimitBytes = 1024 * 1024 * 10,
            PartitionCount = 16,
            EstimateObjectSize = true,
        });
        static readonly Random _random = new();

        static void Main(string[] args)
        {
            int threadCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount;

            Console.WriteLine($"Flooding cache with {threadCount} threads.");

            for (int i = 0; i < threadCount; i++)
            {
                new Thread(FloodCache).Start();
                new Thread(FloodCache).Start();
                new Thread(FloodCache).Start();
                new Thread(FloodCache).Start();
            }

            while (true)
            {
                long items = _cache.Count();
                double size = _cache.CurrentEstimatedSize();

                Console.WriteLine($"Size: {size:n0}b, Items: {items:n0}");
                Thread.Sleep(1000);
            }
        }

        public static void FloodCache()
        {
            while (true)
            {
                int randomNumber = _random.Next(0, 10000000);

                string cacheKey = $"Car{randomNumber}";

                if (_random.Next(0, 100) >= 75)
                {
                    _cache.TryGet<Car>(cacheKey, out var _);
                }

                var car = new Car()
                {
                    Name = RandomString(10),
                    Description = RandomString(100),
                    Price = 100,
                    Transmission = new()
                    {
                        Name = RandomString(20),
                        Description = RandomString(200),
                        Gears = 16,
                        TransmissionType = Car.TransmissionType.Manual,
                        Price = 45
                    }
                };

                _cache.Upsert(cacheKey, car, TimeSpan.FromSeconds(20));
            }
        }

        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var randomString = new StringBuilder();

            var random = new Random();

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(0, chars.Length);
                randomString.Append(chars[index]);
            }

            return randomString.ToString();
        }
    }
}
