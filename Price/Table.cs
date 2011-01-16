using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Price
{
    public class Table
    {
        public class Cell
        {
            public string   text;
            public string   style;
            public string   cls;
            public int      rowspan;
            public int      colspan;
        }

        public class Row
        {
            public List<Cell> cells = new List<Cell>();
        }

        public List<Row> rows = new List<Row>();

        public void Output(StreamWriter writer)
        {
            writer.WriteLine("<table cellspacing='0'>");

            List<int> columnCapacity = new List<int>();

            for (int i = 0; i < rows.Count; ++i)
            {
                Row row = rows[i];

                writer.WriteLine("<tr>");

                for (int j = 0; j < row.cells.Count; ++j)
                {
                    Cell cell = row.cells[j];
                    int colspan = (cell.colspan == 0) ? 1 : cell.colspan;

                    while (columnCapacity.Count < j + colspan)
                        columnCapacity.Add(0);

                    if (columnCapacity[j] > i)
                        continue;

                    writer.Write("<td");

                    if (cell.colspan != 0)
                        writer.Write(" colspan='" + cell.colspan.ToString() + "'");

                    if (cell.rowspan != 0)
                        writer.Write(" rowspan='" + cell.rowspan.ToString() + "'");

                    if (cell.style != null)
                        writer.Write(" style='" + cell.style + "'");

                    if (cell.cls != null)
                        writer.Write(" class='" + cell.cls + "'");

                    writer.Write(">");
                    writer.Write(cell.text);
                    writer.WriteLine("</td>");

                    for (int k = 0; k < colspan; ++k)
                        columnCapacity[j + k] = i + ((cell.rowspan == 0) ? 1 : cell.rowspan);

                    if (cell.colspan != 0)
                        j += cell.colspan - 1;
                }

                writer.WriteLine("</tr>");
            }

            writer.WriteLine("</table>");
        }
    }
}
