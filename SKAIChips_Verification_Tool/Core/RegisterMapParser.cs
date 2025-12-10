using System;
using System.Globalization;

namespace SKAIChips_Verification_Tool.Core
{
    public static class RegisterMapParser
    {
        public static RegisterGroup MakeRegisterGroup(string groupName, string[,] regData)
        {
            var registerGroup = new RegisterGroup(groupName);

            if (regData == null)
                return registerGroup;

            var rowCount = regData.GetLength(0);
            var columnCount = regData.GetLength(1);

            for (var columnStart = 0; columnStart < Math.Min(3, columnCount); columnStart++)
            {
                for (var row = 0; row < rowCount; row++)
                {
                    if (!IsRegisterBlockHeader(regData, row, columnStart, rowCount))
                        continue;

                    if (!TryParseRegisterAddress(regData, row, columnStart, columnCount, out var address))
                        continue;

                    var registerName = GetRegisterName(regData, row, columnStart, columnCount);
                    var register = registerGroup.AddRegister(registerName, address);

                    register.ResetValue = PopulateRegisterItems(
                        regData,
                        register,
                        row,
                        columnStart,
                        rowCount,
                        columnCount);
                }
            }

            return registerGroup;
        }

        private static bool IsRegisterBlockHeader(string[,] regData, int row, int columnStart, int rowCount)
        {
            if (row < 1 || row + 2 >= rowCount)
                return false;

            return regData[row, columnStart] == "Bit" &&
                   regData[row + 1, columnStart] == "Name" &&
                   regData[row + 2, columnStart] == "Default";
        }

        private static bool TryParseRegisterAddress(
            string[,] regData,
            int row,
            int columnStart,
            int columnCount,
            out uint address)
        {
            address = 0;

            var addressText = columnStart + 1 < columnCount ? regData[row - 1, columnStart + 1] : null;

            if (string.IsNullOrWhiteSpace(addressText))
                return false;

            if (addressText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                addressText = addressText.Substring(2);

            return uint.TryParse(addressText, NumberStyles.HexNumber, null, out address);
        }

        private static string GetRegisterName(string[,] regData, int row, int columnStart, int columnCount)
        {
            return columnStart + 2 < columnCount ? regData[row - 1, columnStart + 2] : null;
        }

        private static uint PopulateRegisterItems(
            string[,] regData,
            Register register,
            int headerRow,
            int columnStart,
            int rowCount,
            int columnCount)
        {
            uint resetValue = 0;

            for (var column = columnStart + 1; column < columnCount; column++)
            {
                var defaultText = regData[headerRow + 2, column];
                var nameText = regData[headerRow + 1, column];

                if (defaultText == "X" ||
                    defaultText == "-" ||
                    defaultText == null ||
                    nameText == null)
                {
                    continue;
                }

                if (!int.TryParse(regData[headerRow, column], out var upperBit))
                    continue;

                var lowerBit = upperBit;
                var itemValue = defaultText == "0" ? 0u : 1u;

                DetermineBitRangeAndValue(regData, headerRow, column, columnCount, ref lowerBit, ref itemValue);

                var itemName = nameText;
                var itemDescription = ExtractItemDescription(regData, itemName, headerRow, columnStart, rowCount, columnCount);

                register.AddItem(itemName, upperBit, lowerBit, itemValue, itemDescription);

                resetValue = UpdateResetValue(resetValue, upperBit, lowerBit, itemValue);
            }

            return resetValue;
        }

        private static void DetermineBitRangeAndValue(
            string[,] regData,
            int headerRow,
            int bitColumn,
            int columnCount,
            ref int lowerBit,
            ref uint itemValue)
        {
            for (var column = bitColumn + 1; column < columnCount; column++)
            {
                if (regData[headerRow + 1, column] != null)
                    break;

                var bitText = regData[headerRow, column];
                if (bitText == null)
                    continue;

                if (!int.TryParse(bitText, out var bit))
                    continue;

                lowerBit = bit;

                var bitDefault = regData[headerRow + 2, column];
                itemValue = (itemValue << 1) | (bitDefault == "0" ? 0u : 1u);
            }
        }

        private static string ExtractItemDescription(
            string[,] regData,
            string itemName,
            int headerRow,
            int columnStart,
            int rowCount,
            int columnCount)
        {
            string description = null;

            for (var row = headerRow; row < rowCount; row++)
            {
                if (regData[row, columnStart] != itemName)
                    continue;

                if (columnStart + 1 < columnCount)
                    description = regData[row, columnStart + 1];

                AppendDescriptionLines(regData, ref description, row, columnStart, rowCount, columnCount);

                break;
            }

            return description;
        }

        private static void AppendDescriptionLines(
            string[,] regData,
            ref string description,
            int startRow,
            int columnStart,
            int rowCount,
            int columnCount)
        {
            for (var row = startRow + 1; row < rowCount; row++)
            {
                if (regData[row, columnStart] != null)
                    break;

                var thirdColumn = columnStart + 3 < columnCount ? regData[row, columnStart + 3] : null;
                var fourthColumn = columnStart + 4 < columnCount ? regData[row, columnStart + 4] : null;

                var line = string.Empty;

                if (thirdColumn != null && fourthColumn != null)
                    line = "\n" + thirdColumn + "=" + fourthColumn;
                else if (fourthColumn != null)
                    line = "\n" + fourthColumn;
                else if (thirdColumn != null)
                    line = "\n" + thirdColumn + "=";

                if (!string.IsNullOrEmpty(line))
                    description += line;
            }
        }

        private static uint UpdateResetValue(uint resetValue, int upperBit, int lowerBit, uint itemValue)
        {
            var width = upperBit - lowerBit + 1;
            if (width <= 0)
                return resetValue;

            var mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
            var maskedValue = itemValue & mask;

            resetValue &= ~(mask << lowerBit);
            resetValue |= maskedValue << lowerBit;

            return resetValue;
        }
    }
}
