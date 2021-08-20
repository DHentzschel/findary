namespace Findary
{
    public class StatisticsDao
    {
        public ThreadSafeUInt AlreadySupported { get; set; } = new();

        public EntityStatistics Directories { get; set; } = new(nameof(Directories));

        public EntityStatistics Files { get; set; } = new(nameof(Files));

        public ThreadSafeUInt IgnoredFiles { get; set; } = new();

        public ThreadSafeUInt TrackedFiles { get; set; } = new();

        public class EntityStatistics
        {
            private readonly string _name;

            public EntityStatistics(string name)
            {
                _name = name;
            }

            public ThreadSafeUInt AccessDenied { get; set; } = new();

            public ThreadSafeUInt Processed { get; set; } = new();

            public ThreadSafeUInt Total { get; set; } = new();

            public override string ToString() => _name + ": " + Processed.Value + " processed, " + Total.Value + " total, " + AccessDenied.Value + " denied access";
        }
    }
}
