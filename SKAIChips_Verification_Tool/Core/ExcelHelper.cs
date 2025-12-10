using ClosedXML.Excel;

namespace SKAIChips_Verification_Tool.Core
{
    /// <summary>
    /// Provides helper methods for working with Excel worksheets.
    /// </summary>
    public static class ExcelHelper
    {
        #region Public Methods

        /// <summary>
        /// Converts the contents of an Excel worksheet into a two-dimensional string array.
        /// </summary>
        /// <param name="ws">The worksheet to read.</param>
        /// <returns>A 2D array containing trimmed cell values; empty when the worksheet or used range is missing.</returns>
        public static string[,] WorksheetToArray(IXLWorksheet ws)
        {
            if (ws == null)
                return new string[0, 0];

            var range = ws.RangeUsed();
            if (range == null)
                return new string[0, 0];

            var rows = range.RowCount();
            var cols = range.ColumnCount();

            var data = new string[rows, cols];

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var s = range.Cell(r + 1, c + 1).GetString();
                    s = string.IsNullOrWhiteSpace(s) ? null : s.Trim();
                    data[r, c] = s;
                }
            }

            return data;
        }

        #endregion
    }
}
