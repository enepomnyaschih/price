using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Price
{
    class Program
    {
        const string TEMPLATE_TABLE = "%table%";

        static void Main(string[] args)
        {
            Config config = Config.Load("config.txt");
            Price source = new Price(config.input);
            Price result = new Price(source, config.convert);
            PriceRenderer renderer = new PriceRenderer(result, config.output);

            string template = File.ReadAllText(config.input.template);
            int position = template.IndexOf(TEMPLATE_TABLE);

            using (StreamWriter writer = new StreamWriter(File.Open("output.html", FileMode.Create), Encoding.Default))
            {
                writer.Write(template.Substring(0, position));
                renderer.result.Output(writer);
                writer.Write(template.Substring(position + TEMPLATE_TABLE.Length));
            }

            return;
        }
    }
}
