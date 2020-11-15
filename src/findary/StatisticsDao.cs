namespace Findary
{
    public class StatisticsDao
    {
        public EntityStatistics Directories { get; set; } = new EntityStatistics(nameof(Directories));

        public EntityStatistics Files { get; set; } = new EntityStatistics(nameof(Files));

        public uint IgnoredFiles { get; set; }

        public class EntityStatistics
        {
            private readonly string _name;

            public EntityStatistics(string name)
            {
                _name = name;
            }

            public uint Processed { get; set; }

            public uint Total { get; set; }

            public uint AccessDenied { get; set; }

            public override string ToString() => _name + ": " + Processed + " processed, " + Total + " total, " + AccessDenied + " denied access";
        }
    }
}
