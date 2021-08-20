using System;
using System.IO;
using NLog;

namespace Findary.FileScan
{
    public class FileScan : Bom, IFileScan
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public StatisticsDao Statistics { get; set; } = new();

        public bool IsFileBinary(string filePath)
        {
            try
            {
                var fileStream = File.OpenRead(filePath);
                return IsFileBinary(fileStream);
            }
            catch (Exception e)
            {
                _logger.Warn("Could not read file " + filePath + ": " + e.Message);
                if (e is UnauthorizedAccessException)
                {
                    ++Statistics.Files.AccessDenied.Value;
                }
                return false;
            }
        }

        public bool IsFileBinary(Stream stream)
        {
            Statistics ??= new StatisticsDao();

            var bytes = new byte[1024];
            int bytesRead;
            var isFirstBlock = true;
            var bom = new Bom();
            while ((bytesRead = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                if (isFirstBlock)
                {
                    ++Statistics.Files.Processed.Value;
                    bom.InputArray = bytes;
                    if (bom.HasBom())
                    {
                        return false;
                    }
                }

                var zeroIndex = Array.FindIndex(bytes, p => p == '\0');
                if (zeroIndex > -1 && zeroIndex < bytesRead)
                {
                    return true;
                }

                isFirstBlock = false;
            }
            return false;
        }
    }
}
