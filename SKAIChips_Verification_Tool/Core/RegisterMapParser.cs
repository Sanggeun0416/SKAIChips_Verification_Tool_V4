using DocumentFormat.OpenXml.Drawing;
using System;

namespace SKAIChips_Verification_Tool.Core
{
    public static class RegisterMapParser
    {
        public static RegisterGroup MakeRegisterGroup(string groupName, string[,] regData)
        {
            var rg = new RegisterGroup(groupName);

            for (int xStart = 0; xStart < 3; xStart++)
            {
                for (int row = 0; row < regData.GetLength(0); row++)
                {
                    if ((row + 2 < regData.GetLength(0)) &&
                        regData[row, xStart] == "Bit" &&
                        regData[row + 1, xStart] == "Name" &&
                        regData[row + 2, xStart] == "Default")
                    {
                        string regName = null;
                        uint address = 0;

                        string strAddr = regData[row - 1, xStart + 1];

                        if (strAddr != null && strAddr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            strAddr = strAddr.Substring(2);

                        if (uint.TryParse(strAddr, System.Globalization.NumberStyles.HexNumber, null, out address))
                        {
                            regName = regData[row - 1, xStart + 2];
                            var reg = rg.AddRegister(regName, address);

                            uint resetValue = 0;    // <= 추가

                            for (int column = xStart + 1; column < regData.GetLength(1); column++)
                            {
                                if (!((regData[row + 2, column] == "X") ||
                                      (regData[row + 2, column] == "-" ||
                                      (regData[row + 2, column] == null) ||
                                      (regData[row + 1, column] == null))))
                                {
                                    string itemName = null;
                                    string itemDesc = null;
                                    int upperBit = 0;
                                    int lowerBit = 0;
                                    uint itemValue = 0;

                                    itemName = regData[row + 1, column];
                                    upperBit = int.Parse(regData[row, column]);
                                    lowerBit = int.Parse(regData[row, column]);
                                    itemValue = (uint)((regData[row + 2, column] == "0") ? 0 : 1);

                                    for (int x = column + 1; x < regData.GetLength(1); x++)
                                    {
                                        if (regData[row + 1, x] == null)
                                        {
                                            if (regData[row, x] != null)
                                            {
                                                lowerBit = int.Parse(regData[row, x]);
                                                itemValue = (itemValue << 1) |
                                                            (uint)((regData[row + 2, x] == "0") ? 0 : 1);
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    for (int y = row; y < regData.GetLength(0); y++)
                                    {
                                        if (regData[y, xStart] == itemName)
                                        {
                                            itemDesc = regData[y, xStart + 1];

                                            for (int descRow = y + 1; descRow < regData.GetLength(0); descRow++)
                                            {
                                                if (regData[descRow, xStart] == null)
                                                {
                                                    string lineDesc = "";

                                                    if (regData[descRow, xStart + 3] != null &&
                                                        regData[descRow, xStart + 4] != null)
                                                        lineDesc = "\n" + regData[descRow, xStart + 3] + "=" +
                                                                   regData[descRow, xStart + 4];
                                                    else if (regData[descRow, xStart + 4] != null)
                                                        lineDesc = "\n" + regData[descRow, xStart + 4];
                                                    else if (regData[descRow, xStart + 3] != null)
                                                        lineDesc = "\n" + regData[descRow, xStart + 3] + "=";

                                                    itemDesc += lineDesc;
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                    }

                                    reg.AddItem(itemName, upperBit, lowerBit, itemValue, itemDesc);

                                    int width = upperBit - lowerBit + 1;
                                    uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                                    uint val = itemValue & mask;

                                    resetValue &= ~(mask << lowerBit);
                                    resetValue |= (val << lowerBit);
                                }
                            }
                            reg.ResetValue = resetValue;
                        }
                    }
                }
            }

            return rg;
        }
    }
}
