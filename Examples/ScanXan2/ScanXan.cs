namespace ScanXan
{
    using System;
    using System.IO;

    using TextScanner;

    internal class ScanXan
    {
        private static void Main(string[] args)
        {
            using (var s = new TextScanner(new StreamReader("xanadu.txt")))
            {
                foreach (var token in s)
                {
                    Console.WriteLine(token);
                }
            }
        }
    }
}
