using System;
using System.Collections.Generic;
using System.Text;

namespace Price
{
    public class Expression
    {
        public string source;
        public List<string> keys = new List<string>();
        public List<string> tokens = new List<string>();

        public Expression(string source)
        {
            this.source = source;

            int start = 0;
            int cur = 0;

            while (true)
            {
                while (true)
                {
                    if (cur == source.Length || source[cur] == '{')
                    {
                        tokens.Add(source.Substring(start, cur - start));
                        start = cur + 1;
                        break;
                    }
                    ++cur;
                }

                if (cur == source.Length)
                    break;

                ++cur;
                while (true)
                {
                    if (cur == source.Length)
                        break;

                    if (source[cur] == '}')
                    {
                        keys.Add(source.Substring(start, cur - start));
                        start = cur + 1;
                        break;
                    }
                    ++cur;
                }

                if (cur == source.Length)
                    break;

                ++cur;
            }
        }
        
        public Dictionary<string, string> Parse(string input)
        {
            Dictionary<string, string> result = new Dictionary<string,string>();

            int start = 0;
            for (int i = 0; i < tokens.Count; ++i)
            {
                string token = tokens[i];
                int index;
                if (i == tokens.Count - 1)
                {
                    if (token.Length == 0)
                        index = input.Length;
                    else
                        index = input.LastIndexOf(token);
                }
                else
                    index = input.IndexOf(token, start);

                if (index < start)
                    return null;

                if (i == 0 && index != 0)
                    return null;

                if (i != 0)
                    result[keys[i - 1]] = input.Substring(start, index - start);

                start = index + token.Length;
            }

            if (start != input.Length)
                return null;

            return result;
        }

        public string Format(Dictionary<string, string> values)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < keys.Count; ++i)
            {
                result.Append(tokens[i]);

                string key = keys[i];
                if (!values.ContainsKey(key))
                {
                    throw new Exception("При подстановке значений в выражение '" + source + "' невозможно найти ключ {" + key + "}. Исправьте ошибку в конфигурационном файле.");
                }

                result.Append(values[key]);
            }

            result.Append(tokens[keys.Count]);

            return result.ToString();
        }
    }
}
