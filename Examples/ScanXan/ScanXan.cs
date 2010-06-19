namespace ScanXan
{
    using System;
    using System.IO;

    using TextScanner;

    internal class ScanXan
    {
        private static void Main(string[] args)
        {
            TextScanner s = null;

            try
            {
                s = new TextScanner(new StreamReader("xanadu.txt"));

                while (s.HasNext())
                {
                    Console.WriteLine(s.Next());
                }
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }
        }
    }
}
