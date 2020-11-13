using CommandLine;

namespace findary
{
    public static class Program
    {
        public static void Main(string[] args)
            => Parser.Default.ParseArguments<Options>(args).WithParsed(p => new Findary().Run(p));
    }
}
