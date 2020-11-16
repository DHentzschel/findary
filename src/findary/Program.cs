using CommandLine;

namespace Findary
{
    public static class Program
    {
        public static void Main(string[] args)
            => Parser.Default.ParseArguments<Options>(args).WithParsed(p => new Findary(p).Run());
    }
}
