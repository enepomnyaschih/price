using System.IO;
using System.Text;
using System.Collections.Generic;
using System;

namespace Price
{
    public class Price
    {
        public class Product : ICloneable
        {
            public int              code;
            public string           name;
            public string           groupName;
            public string           shortName;
            public int              block;
            public int              cost;
            public int              pv;

            public object Clone()
            {
                Product result = new Product();
                result.code     = code;
                result.name     = name;
                result.block    = block;
                result.cost     = cost;
                result.pv       = pv;

                return result;
            }
        }

        public class Collection
        {
            public string           name;
            public List<Product>    products    = new List<Product>();
            public List<Collection> collections = new List<Collection>();

            public Collection()
            {
            }

            public Collection(string name)
            {
                this.name = name;
            }
        }

        private class MergeInfo
        {
            public Product product;
            public List<int> codes;

            public MergeInfo(Product product)
            {
                this.product = product;
                this.codes = new List<int>();
            }
        }

        public Collection root;

        /**
         * Закачивает новосибирский прайс из текстового файла.
         */
        public Price(InputConfig config)
        {
            // Считываем исходную таблицу строк
            string[] lines = File.ReadAllLines(config.file, Encoding.Default);
            int length = lines.Length - config.start;

            string[][] content = new string[length][];

            char[] tabSeparator = new char[1];
            tabSeparator[0] = '\t';

            char[] quoteSeparator = new char[1];
            quoteSeparator[0] = '"';

            for (int i = 0; i < length; ++i)
            {
                content[i] = lines[i + config.start].Split(tabSeparator);
            }

            // Формируем структуру данных
            root = new Collection();

            Collection type     = null;
            Collection producer = null;
            Collection spec     = null;

            for (int lineIndex = 0; lineIndex < length; ++lineIndex)
            {
                string name = content[lineIndex][1].Trim();
                if (name.Length == 0)
                    continue;

                if (content[lineIndex][0].Length == 0)
                {
                    if (IndexOf(config.ignoreGroups, name) != -1)
                    {
                        type        = null;
                        producer    = null;
                        spec        = null;
                    }
                    else if (IndexOf(config.types, name) != -1)
                    {
                        type        = new Collection(name);
                        producer    = null;
                        spec        = null;

                        root.collections.Add(type);
                    }
                    else if (IndexOf(config.producers, name) != -1)
                    {
                        producer    = new Collection(name);
                        spec        = null;

                        type.collections.Add(producer);
                    }
                    else if (IndexOf(config.specs, name) != -1)
                    {
                        spec        = new Collection(name);

                        producer.collections.Add(spec);
                    }
                    else
                    {
                        throw new Exception("Коллекция с именем '" + name + "' не указана в конфигурации. Возможно, это новый производитель или новая группа товаров. Пожалуйста, добавьте новую коллекцию в файл конфигурации.");
                    }
                }
                else
                {
                    int code = Convert.ToInt32(content[lineIndex][0]);
                    if (IndexOf(config.ignores, code) != -1)
                        continue;

                    if (string.IsNullOrEmpty(content[lineIndex][3]))
                        continue;

                    Product product = new Product();
                    product.code   = code;
                    product.name   = name;
                    product.block  = Convert.ToInt32(content[lineIndex][4]);
                    product.cost   = Convert.ToInt32(content[lineIndex][2]);
                    product.pv     = Convert.ToInt32(content[lineIndex][3]);

                    if (null != spec)
                    {
                        spec.products.Add(product);
                    }
                    else if (null != producer)
                    {
                        producer.products.Add(product);
                    }
                    else if (null != type)
                    {
                        type.products.Add(product);
                    }
                    /*else
                    {
                        throw new Exception("Товар #" + product.code + " (" + name + ") некуда положить - не указана коллекция (тип товара/производитель)");
                    }*/
                }
            }
        }

        /**
         * Создает омский прайс на основе новосибирского прайса,
         * выполняя следующие преобразования:
         * 1) Группирует товары в соответствии с конфигурацией;
         * 2) Сортирует товары по кодам, сохраняя группировку.
         */
        public Price(Price source, ConvertConfig config)
        {
            root = ConvertCollection(source.root, config);
        }

        /**
         * Сохраняет прайс в html-формате в виде "книжки".
         */
        public void Save(string fileName, OutputConfig config)
        {
        }






        private static int IndexOf(string[] array, string value)
        {
            for (int i = 0; i < array.Length; ++i)
                if (array[i] == value)
                    return i;
            return -1;
        }

        private static int IndexOf(int[] array, int value)
        {
            for (int i = 0; i < array.Length; ++i)
                if (array[i] == value)
                    return i;
            return -1;
        }

        private Collection ConvertCollection(Collection collection, ConvertConfig config)
        {
            Collection result = new Collection();
            result.name = collection.name;

            List<Product> products = new List<Product>();
            foreach (Product product in collection.products)
                products.Add((Product)product.Clone());

            products.Sort(CompareProductsByCode);

            Dictionary<Product, MergeInfo> mergesByProduct = new Dictionary<Product, MergeInfo>();
            Dictionary<string, MergeInfo> mergesByIndex = new Dictionary<string, MergeInfo>();

            // Определяем группы товаров
            for (int productIndex = 0; productIndex < products.Count; ++productIndex)
            {
                Product product = products[productIndex];

                // Проверяем выполнение регулярных выражений
                for (int groupIndex = 0; groupIndex < config.groups.Length; ++groupIndex)
                {
                    ConvertConfig.GroupConfig groupConfig = config.groups[groupIndex];
                    Dictionary<string, string> values = groupConfig.inputExpression.Parse(product.name);

                    if (null != values)
                    {
                        if (null != groupConfig.merge)
                        {
                            if (null != groupConfig.textExpression)
                                product.name = groupConfig.textExpression.Format(values).Trim();

                            string mid = product.name + ":" + product.cost.ToString();
                            if (!mergesByIndex.ContainsKey(mid))
                            {
                                MergeInfo mergeInfo = new MergeInfo(product);
                                mergesByIndex.Add(mid, mergeInfo);
                                mergesByProduct.Add(product, mergeInfo);
                            }
                            else
                            {
                                mergesByProduct.Add(product, null);
                            }

                            mergesByIndex[mid].codes.Add(product.code);
                        }
                        else if (null != groupConfig.groupExpression)
                        {
                            product.groupName = groupConfig.groupExpression.Format(values).Trim();
                            product.shortName = groupConfig.textExpression.Format(values).Trim();
                        }
                        else
                        {
                            product.name = groupConfig.textExpression.Format(values).Trim();
                        }

                        break;
                    }
                }
            }

            // Группируем товары
            for (int productIndex = 0; productIndex < products.Count; ++productIndex)
            {
                Product product = products[productIndex];
                if (null == product)
                    continue;

                if (mergesByProduct.ContainsKey(product))
                {
                    MergeInfo mergeInfo = mergesByProduct[product];
                    if (null == mergeInfo)
                        continue;

                    product.code = 0;

                    StringBuilder name = new StringBuilder();
                    name.Append(product.name);
                    name.Append(" (");

                    mergeInfo.codes.Sort();

                    int codeStartIndex = 0;
                    for (int codeIndex = 1; codeIndex <= mergeInfo.codes.Count; ++codeIndex)
                    {
                        if (codeIndex < mergeInfo.codes.Count &&
                            mergeInfo.codes[codeIndex] == mergeInfo.codes[codeStartIndex] + codeIndex - codeStartIndex)
                            continue;

                        if (codeStartIndex != 0)
                            name.Append(", ");

                        name.Append(mergeInfo.codes[codeStartIndex]);
                        if (codeIndex - codeStartIndex > 1)
                        {
                            name.Append("-");
                            name.Append(mergeInfo.codes[codeIndex - 1]);
                        }

                        codeStartIndex = codeIndex;
                    }

                    name.Append(")");

                    product.name = name.ToString();

                    result.products.Add(product);
                }
                else if (null != product.shortName && null != product.groupName)
                {
                    // Создаем группу товаров
                    for (int i = productIndex; i < products.Count; ++i)
                    {
                        Product inputProduct = products[i];
                        if (null == inputProduct)
                            continue;

                        if (inputProduct.groupName == product.groupName)
                        {
                            result.products.Add(inputProduct);
                            products[i] = null;
                        }
                    }
                }
                else
                {
                    // Помещаем товар в список
                    result.products.Add(product);
                }
            }

            // Прогоняем подгруппы
            foreach (Collection subCollection in collection.collections)
                result.collections.Add(ConvertCollection(subCollection, config));

            return result;
        }

        private int CompareProductsByCode(Product x, Product y)
        {
            Product a = (Product)x;
            Product b = (Product)y;

            return a.name.CompareTo(b.name);
        }
    }
}
