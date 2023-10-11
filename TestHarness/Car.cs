namespace TestHarness
{
    public class Car
    {
        public enum TransmissionType
        {
            Undefined,
            Automatic,
            Manual
        }

        public class TransmissionInfo
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int Price { get; set; }
            public int Gears { get; set; }
            public TransmissionType TransmissionType { get; set; }
        }

        public string[] Colors { get; set; } = { "Red", "Green", "Black" };
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Price { get; set; }
        public TransmissionInfo Transmission { get; set; } = new();
    }
}
