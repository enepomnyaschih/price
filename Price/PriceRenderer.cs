using System;
using System.Collections.Generic;
using System.Text;

namespace Price
{
    public class PriceRenderer
    {
        private class Header
        {
            public string name;
            public string cls;

            public Header(string name, string cls)
            {
                this.name = name;
                this.cls  = cls;
            }
        }

        private class Change
        {
            public int code;

            public bool isCode;
            public bool isName;
            public bool isCost;
            public bool isPv;

            public Change(string source)
            {
                char[] sep = { '.' };
                string[] s = source.Split(sep);

                code = Convert.ToInt32(s[0]);
                isCode = s[1].Contains("d");
                isName = s[1].Contains("n");
                isCost = s[1].Contains("c");
                isPv   = s[1].Contains("p");
            }
        }

        private Price price;
        private OutputConfig config;
        private List<Table> tables = new List<Table>();
        private Table table = null;
        private int tableIndex = -1;
        private int rowIndex;
        private List<Header> headers = new List<Header>();
        private Dictionary<int, Change> changes;

        private string group = null;






        public Table result = new Table();

        public PriceRenderer(Price price, OutputConfig config)
        {
            this.price = price;
            this.config = config;

            BuildChanges();

            AddColumns();
            RenderCollection(price.root, -1);
            MergeTables();
        }






        private void BuildChanges()
        {
            changes = new Dictionary<int, Change>();

            for (int i = 0; i < config.changes.Length; ++i)
            {
                Change change = new Change(config.changes[i]);
                changes.Add(change.code, change);
            }
        }

        private void RenderCollection(Price.Collection collection, int level)
        {
            if ((null != collection.name) && (level != -1))
                AddHeader(collection.name, config.headers[level]);

            if (collection.products.Count != 0)
                FlushHeaders();

            for (int productIndex = 0; productIndex < collection.products.Count; ++productIndex)
            {
                Price.Product product = collection.products[productIndex];

                bool divideName     = false;
                int  groupCapacity  = 0;

                if (null != product.groupName)
                {
                    if (group == product.groupName)
                    {
                        // ����������� ������ ������
                        divideName = true;
                    }
                    else
                    {
                        for (int i = productIndex; i < collection.products.Count; ++i)
                        {
                            if (rowIndex + i + headers.Count - productIndex >= config.lines)
                                break;

                            if (product.groupName == collection.products[i].groupName)
                                ++groupCapacity;
                            else
                                break;
                        }

                        if (groupCapacity != 1)
                        {
                            // ������ ����� ������
                            divideName = true;
                        }
                    }
                }

                int opt  = (int)((product.cost * config.opt ) / 100 + 0.5);
                int rozn = (int)((opt          * config.rozn) / 100 + 0.5);

                Change change = null;
                changes.TryGetValue(product.code, out change);

                Table.Row row = AddRow();
                row.cells[0].text = Convert.ToString(RenderCode(product.code));
                row.cells[3].text = opt.ToString();
                row.cells[4].text = rozn.ToString();
                row.cells[5].text = product.pv.ToString();

                if (change != null && change.isCode)
                    row.cells[0].cls += " changed";

                if (change != null && change.isCost)
                {
                    row.cells[3].cls += " changed";
                    row.cells[4].cls += " changed";
                }

                if (change != null && change.isPv)
                    row.cells[5].cls += " changed";

                if (divideName)
                {
                    if (product.shortName.Length == 0 ||
                        product.shortName[0] == ',')
                        row.cells[2].text = RenderName(config.basename + product.shortName);
                    else
                        row.cells[2].text = RenderName(product.shortName);

                    if (change != null && change.isName)
                        row.cells[2].cls += " changed";

                    if (0 != groupCapacity)
                    {
                        row.cells[1].text = RenderName(product.groupName);
                        row.cells[1].rowspan = groupCapacity;
                        row.cells[1].cls = "group";

                        bool isChange = true;
                        for (int i = 0; i < groupCapacity; ++i)
                        {
                            Change subChange = null;
                            changes.TryGetValue(collection.products[productIndex + i].code, out subChange);
                            if (subChange == null || !subChange.isName)
                            {
                                isChange = false;
                                break;
                            }
                        }

                        if (isChange)
                            row.cells[1].cls += " changed";

                        group = product.groupName;
                    }
                }
                else
                {
                    row.cells[1].text = RenderName(product.name);
                    row.cells[1].cls = "simple-long";
                    row.cells[1].colspan = 2;

                    if (change != null && change.isName)
                        row.cells[1].cls += " changed";

                    group = null;
                }
            }

            foreach (Price.Collection subCollection in collection.collections)
                RenderCollection(subCollection, level + 1);
        }

        private void AddHeader(string name, string cls)
        {
            headers.Add(new Header(name, cls));
        }

        private void FlushHeaders()
        {
            if (rowIndex + headers.Count >= config.lines)
                AddColumns();

            foreach (Header header in headers)
            {
                Table.Row row = AddRow();

                row.cells[0].text = header.name;
                row.cells[0].cls  = header.cls;
                row.cells[0].colspan = 6;
            }

            headers.Clear();
        }

        private Table.Row AddRow()
        {
            Table.Row result = new Table.Row();
            for (int i = 0; i < 6; ++i)
            {
                Table.Cell cell = new Table.Cell();
                cell.cls = "simple-" + i.ToString();
                result.cells.Add(cell);
            }

            table.rows.Add(result);
            ++rowIndex;

            if (rowIndex >= config.lines)
                AddColumns();

            return result;
        }

        private void AddColumns()
        {
            group = null;

            table = new Table();
            tables.Add(table);
            ++tableIndex;
            rowIndex = 0;

            if (tableIndex % 2 == 0)
            {
                Table.Row row = AddRow();
                row.cells[0].text = config.columns[0];
                row.cells[1].text = config.columns[1];
                row.cells[3].text = config.columns[2];
                row.cells[4].text = config.columns[3];
                row.cells[5].text = config.columns[4];
                row.cells[0].rowspan = 2;
                row.cells[1].rowspan = 2;
                row.cells[3].rowspan = 2;
                row.cells[4].rowspan = 2;
                row.cells[5].rowspan = 2;
                row.cells[1].colspan = 2;
                row.cells[0].cls = "column";
                row.cells[1].cls = "column";
                row.cells[3].cls = "column";
                row.cells[4].cls = "column";
                row.cells[5].cls = "column";
                AddRow();
            }
        }

        private string RenderName(string name)
        {
            return name.Replace('?', '-');
        }

        private string RenderCode(int code)
        {
            if (code == 0)
                return "";

            string result = "";
            for (int i = 0; i < 4; ++i)
            {
                result = (code % 10).ToString() + result;
                code /= 10;
            }

            return result;
        }

        private void MergeTables()
        {
            bool extraPage = (tables.Count + 3) % 8 < 2;
            int pageCount = (tables.Count + 3) / 8;
            if (!extraPage)
                ++pageCount;

            for (int i = 0; i < pageCount; ++i)
            {
                int j = 2 * pageCount - i - 2;

                int I = i * 4;
                int J = j * 4;

                AddTables(I + 0, 2);
                AddSpacing(4);
                AddTables(J + 1, 2);

                if (i != j)
                {
                    AddSpacing(2);

                    AddTables(J + 0, 2);
                    AddSpacing(4);
                    AddTables(I + 1, 2);
                }

                if (i != pageCount - 1)
                    AddSpacing(2);
            }

            if (extraPage)
            {
                AddSpacing(2);
                AddTables(pageCount * 8 - 4, 1);
            }
        }

        private void AddTables(int a, int diff)
        {
            int b = a + diff;

            for (int i = 0; i < config.lines; ++i)
            {
                Table.Row row = new Table.Row();

                AddTableRow(row, a, i);

                Table.Cell spacer = new Table.Cell();
                spacer.cls = "spacer";
                row.cells.Add(spacer);

                AddTableRow(row, b, i);

                result.rows.Add(row);
            }
        }

        private void AddTableRow(Table.Row row, int t, int r)
        {
            if (t >= tables.Count)
            {
                AddEmptyCells(row, 6);
                return;
            }

            Table table = tables[t];
            if (r >= table.rows.Count)
            {
                AddEmptyCells(row, 6);
                return;
            }

            Table.Row inputRow = table.rows[r];
            for (int i = 0; i < 6; ++i)
                row.cells.Add(inputRow.cells[i]);
        }

        private void AddEmptyCells(Table.Row row, int count)
        {
            for (int i = 0; i < count; ++i)
                row.cells.Add(new Table.Cell());
        }

        private void AddSpacing(int n)
        {
            for (int i = 0; i < n; ++i)
                result.rows.Add(new Table.Row());
        }
    }
}
