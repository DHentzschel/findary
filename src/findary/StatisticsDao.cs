namespace Findary
{
    public class StatisticsDao
    {
        public EntityStatistics Directories { get; set; } = new EntityStatistics(nameof(Directories));

        public EntityStatistics Files { get; set; } = new EntityStatistics(nameof(Files));

        public class EntityStatistics
        {
            private string _name;

            public EntityStatistics(string name)
            {
                _name = name;
            }

            public int Processed { get; set; }

            public int Total { get; set; }

            public override string ToString() => _name + ":" + Processed + " processed, " + Total + " total";
        }
    }
}
