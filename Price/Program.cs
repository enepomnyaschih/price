using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Price
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = Config.Load("config.txt");
            Price source = new Price(config.input);
            Price result = new Price(source, config.convert);
            PriceRenderer renderer = new PriceRenderer(result, config.output);

            using (StreamWriter writer = new StreamWriter(File.Open("output.html", FileMode.Create), Encoding.Default))
            {
                writer.WriteLine("<html><body>");
                renderer.result.Output(writer);
                writer.WriteLine("</body></html>");
            }

            return;
        }
    }
}
