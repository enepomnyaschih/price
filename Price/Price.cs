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

        public Collection root;

        /**
         * ���������� ������������� ����� �� ���������� �����.
         */
        public Price(InputConfig config)
        {
            // ��������� �������� ������� �����
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

                string name = content[i][1];
                if (name.Length != 0 && name[0] == '"')
                {
                    string[] tmp = name.Substring(1, name.Length - 2).Trim().Split(quoteSeparator);
                    for (int j = 1; j < tmp.Length - 1; ++j)
                    {
                        if (tmp[j].Length == 0)
                            tmp[j] = "\"";
                    }
                    content[i][1] = string.Join(string.Empty, tmp);
                }
            }

            // ��������� ��������� ������
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
                        throw new Exception("��������� � ������ '" + name + "' �� ������� � ������������. ��������, ��� ����� ������������� ��� ����� ������ �������. ����������, �������� ����� ��������� � ���� ������������.");
                    }
                }
                else
                {
                    Product product = new Product();
                    product.code   = Convert.ToInt32(content[lineIndex][0]);
                    product.name   = name;
                    product.block  = Convert.ToInt32(content[lineIndex][2]);
                    product.cost   = Convert.ToInt32(content[lineIndex][3]);
                    product.pv     = Convert.ToInt32(content[lineIndex][4]);

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
                        throw new Exception("����� #" + product.code + " (" + name + ") ������ �������� - �� ������� ��������� (��� ������/�������������)");
                    }*/
                }
            }
        }

        /**
         * ������� ������ ����� �� ������ �������������� ������,
         * �������� ��������� ��������������:
         * 1) ���������� ������ � ������������ � �������������;
         * 2) ��������� ������ �� �����, �������� �����������.
         */
        public Price(Price source, ConvertConfig config)
        {
            root = ConvertCollection(source.root, config);
        }

        /**
         * ��������� ����� � html-������� � ���� "������".
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

        private Collection ConvertCollection(Collection collection, ConvertConfig config)
        {
            Collection result = new Collection();
            result.name = collection.name;

            List<Product> products = new List<Product>();
            foreach (Product product in collection.products)
                products.Add((Product)product.Clone());

            products.Sort(CompareProductsByCode);

            // ���������� ������ �������
            for (int productIndex = 0; productIndex < products.Count; ++productIndex)
            {
                Product product = products[productIndex];

                // ��������� ���������� ���������� ���������
                for (int groupIndex = 0; groupIndex < config.groups.Length; ++groupIndex)
                {
                    ConvertConfig.GroupConfig groupConfig = config.groups[groupIndex];
                    Dictionary<string, string> values = groupConfig.inputExpression.Parse(product.name);

                    if (null != values)
                    {
                        if (null != groupConfig.groupExpression)
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

            // ���������� ������
            for (int productIndex = 0; productIndex < products.Count; ++productIndex)
            {
                Product product = products[productIndex];
                if (null == product)
                    continue;

                if (null != product.shortName &&
                    null != product.groupName)
                {
                    // ������� ������ �������
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
                    // �������� ����� � ������
                    result.products.Add(product);
                }
            }

            // ��������� ���������
            foreach (Collection subCollection in collection.collections)
                result.collections.Add(ConvertCollection(subCollection, config));

            return result;
        }

        private int CompareProductsByCode(Product x, Product y)
        {
            Product a = (Product)x;
            Product b = (Product)y;

            return a.name.CompareTo(b.name);
/*            if (a.code > b.code)
                return 1;
            if (a.code < b.code)
                return -1;
            return 0;
 */       }
    }
}