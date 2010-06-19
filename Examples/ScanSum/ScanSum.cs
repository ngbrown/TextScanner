namespace ScanSum
{
    using System;
    using System.Globalization;
    using System.IO;

    using TextScanner;

    internal class ScanSum
    {
        private static void Main(string[] args)
        {
            TextScanner s = null;
            double sum = 0;

            try
            {
                s = new TextScanner(new StreamReader("usnumbers.txt"));
                s.UseCulture(new CultureInfo("en-US"));

                while (s.HasNext())
                {
                    if (s.HasNextDouble())
                    {
                        sum += s.NextDouble();
                    }
                    else
                    {
                        s.Next();
                    }
                }
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            Console.WriteLine(sum);
        }
    }
}
