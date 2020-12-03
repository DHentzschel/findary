namespace Findary
{
    public class StatisticsDao
    {
        public ThreadSafeUInt AlreadySupported { get; set; } = new ThreadSafeUInt();

        public EntityStatistics Directories { get; set; } = new EntityStatistics(nameof(Directories));

        public EntityStatistics Files { get; set; } = new EntityStatistics(nameof(Files));

        public ThreadSafeUInt IgnoredFiles { get; set; } = new ThreadSafeUInt();

        public ThreadSafeUInt TrackedFiles { get; set; } = new ThreadSafeUInt();

        public class EntityStatistics
        {
            private readonly string _name;

            public EntityStatistics(string name)
            {
                _name = name;
            }

            public ThreadSafeUInt AccessDenied { get; set; } = new ThreadSafeUInt();

            public ThreadSafeUInt Processed { get; set; } = new ThreadSafeUInt();

            public ThreadSafeUInt Total { get; set; } = new ThreadSafeUInt();

            public override string ToString() => _name + ": " + Processed.Value + " processed, " + Total.Value + " total, " + AccessDenied.Value + " denied access";
        }
    }
}
