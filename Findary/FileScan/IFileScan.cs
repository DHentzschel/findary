using System.IO;

namespace Findary.FileScan
{
    public interface IFileScan
    {
        public StatisticsDao Statistics { get; set; }

        public bool IsFileBinary(string filePath);

        public bool IsFileBinary(Stream stream);
    }
}
