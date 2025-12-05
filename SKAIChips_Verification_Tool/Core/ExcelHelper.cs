using ClosedXML.Excel;

namespace SKAIChips_Verification_Tool.Core
{
    public static class ExcelHelper
    {
        public static string[,] WorksheetToArray(IXLWorksheet ws)
        {
            var range = ws.RangeUsed();
            int rows = range.RowCount();
            int cols = range.ColumnCount();

            var data = new string[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var s = range.Cell(r + 1, c + 1).GetString();
                    s = string.IsNullOrWhiteSpace(s) ? null : s.Trim();
                    data[r, c] = s;
                }
            }

            return data;
        }
    }
}
