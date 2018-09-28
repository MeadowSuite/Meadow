using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport
{
    public class AsciiTable
    {
        public string[] Columns { get; set; }
        public string[][] Rows { get; set; }
        public string[] Footer { get; set; }

        public char SplitterChar = '|';

        public void WriteToString(StringBuilder sb)
        {
            var lengthByColumnDictionary = GetColumnSpaces();
            var tableWidth = lengthByColumnDictionary.Values.Aggregate((a, b) => a + b + 3) + 2;
            string hr = new string('-', tableWidth);

            sb.AppendLine(hr);

            AppendColumns(sb, lengthByColumnDictionary);
            sb.AppendLine(hr);

            AppendRows(sb, lengthByColumnDictionary);
            sb.AppendLine(hr);

            if (Footer != null)
            {
                AppendRow(sb, Footer, lengthByColumnDictionary);
                sb.AppendLine(hr);
            }
        }

        void AppendRows(StringBuilder sb, IReadOnlyDictionary<int, int> lengthByColumnDict)
        {
            for (var i = 0; i < Rows.Length; i++)
            {
                AppendRow(sb, Rows[i], lengthByColumnDict);
            }
        }

        void AppendRow(StringBuilder sb, string[] row, IReadOnlyDictionary<int, int> lengthByColumnDict)
        {
            for (var j = 0; j < Columns.Length; j++)
            {
                sb.Append(PadCell(row[j], lengthByColumnDict[j], leftPad: j > 0));
            }

            sb.AppendLine();
        }

        void AppendColumns(StringBuilder sb, IReadOnlyDictionary<int, int> lenghtByColumnDict)
        {
            for (var i = 0; i < Columns.Length; i++)
            {
                var columName = Columns[i];
                var paddedColumNames = PadCell(columName, lenghtByColumnDict[i], leftPad: false);
                sb.Append(paddedColumNames);
            }

            sb.AppendLine();
        }

        Dictionary<int, int> GetColumnSpaces()
        {
            var lengthByColumn = new Dictionary<int, int>();
            for (var i = 0; i < Columns.Length; i++)
            {
                var length = new int[Rows.Length + 1];
                for (var j = 0; j < Rows.Length; j++)
                {
                    length[j] = Rows[j][i].Length;
                }

                length[length.Length - 1] = Footer?[i].Length ?? 0;
                lengthByColumn[i] = length.Max();
            }

            return MinByColumnName(lengthByColumn);
        }

        Dictionary<int, int> MinByColumnName(IReadOnlyDictionary<int, int> lengthByColumnDict)
        {
            var dictionary = new Dictionary<int, int>();
            for (var i = 0; i < Columns.Length; i++)
            {
                var columnNameLength = Columns[i].Length;
                dictionary[i] = columnNameLength > lengthByColumnDict[i]
                    ? columnNameLength
                    : lengthByColumnDict[i];
            }

            return dictionary;
        }

        string PadCell(string value, int totalColumnLength, bool leftPad)
        {
            var remaningSpace = value.Length < totalColumnLength
                ? totalColumnLength - value.Length
                : value.Length - totalColumnLength;

            var spaces = new string(' ', remaningSpace);
            
            return leftPad
                ? (spaces + value + " " + SplitterChar + " ") 
                : (value + spaces + " " + SplitterChar + " ");
        }
    }
}
