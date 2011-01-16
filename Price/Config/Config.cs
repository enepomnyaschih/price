using System.IO;
using System.Text;

using Jayrock.Json.Conversion;

namespace Price
{
    public class Config
    {
        public InputConfig      input;
        public ConvertConfig    convert;
        public OutputConfig     output;

        public static Config Load(string fileName)
        {
            string text = File.ReadAllText(fileName, Encoding.Default);
            Config result = (Config)JsonConvert.Import(typeof(Config), text);

            foreach (ConvertConfig.GroupConfig group in result.convert.groups)
            {
                group.inputExpression = new Expression(group.input);
                group. textExpression = new Expression(group.text );

                if (null != group.group)
                    group.groupExpression = new Expression(group.group);
            }

            return result;
        }
    }
}
