// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GenUnicodeProp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Verbose = false;
            // TODO: parse args

            var defaultCategoryValues = "Cn,L";

            // Create a 12:4:4 table for Unicode category
            // "Cn", Not assigned.  The value is 1 byte to indicate Unicode category
            // Make sure to put the default value into the slot 0 in $categoriesValueTable
            var categoriesIndexTable = new DataTable();
            // Create a 12:4:4 table for decimal digit value/digit value/numeric value
            var numericIndexTable = new DataTable();
            // Create a flat table for Unicode category and BiDi category
            var categoriesValueTable = new FlatDataTable(defaultCategoryValues, GetCategoriesValueBytes);
            // Create a flat table. 
            // GetNumericValueBytes() is the callback used to generate the bytes of each item.
            var numericValueTable = new FlatDataTable("-1", GetNumericValueBytes);
            // Create a flat table for digit values
            // GetDigitValueBytes() is the callback used to generate the bytes of each item.
            var digitValueTable = new FlatDataTable("255,255", GetDigitValueBytes);

            // Add a default item into the category value table.  This will be the item 0 in the category value table.
            GetCategoryValueItem(categoriesValueTable, defaultCategoryValues);
            NumberValues.Add("-1,255,255", 0);
            numericValueTable.AddData(0, "-1");
            digitValueTable.AddData(0, "255,255");

            ReadSourceFile("UnicodeData.txt", categoriesIndexTable, categoriesValueTable, numericIndexTable, numericValueTable, digitValueTable);

            categoriesIndexTable.GenerateTable(nameof(categoriesIndexTable), 5, 4);
            //categoriesIndexTable.CalculateTableVariants();
            numericIndexTable.GenerateTable(nameof(numericIndexTable), 4, 4, cutOff: true);
            //numericIndexTable.CalculateTableVariants(cutOff: true);

            // generate the data C# source
            using (StreamWriter file = File.CreateText(SOURCE_NAME))
            {
                file.Write("// Licensed to the .NET Foundation under one or more agreements.\n");
                file.Write("// The .NET Foundation licenses this file to you under the MIT license.\n");
                file.Write("// See the LICENSE file in the project root for more information.\n\n");

                file.Write("namespace System.Globalization\n");
                file.Write("{\n");
                file.Write("    public static partial class CharUnicodeInfo\n    {\n");

                file.Write("        // THE FOLLOWING DATA IS AUTO GENERATED BY GenUnicodeProp program UNDER THE TOOLS FOLDER\n");
                file.Write("        // PLEASE DON'T MODIFY BY HAND\n\n\n");

                file.Write("        // 11:5:4 index table of the Unicode category data.");
                PrintSourceIndexArray("CategoryLevel1Index", categoriesIndexTable, file);

                PrintValueArray("CategoriesValue", categoriesValueTable, file);

                file.Write("\n        // 12:4:4 index table of the Unicode numeric data.");
                PrintSourceIndexArray("NumericLevel1Index", numericIndexTable, file);

                file.Write("\n        // Every item contains the value for numeric value.");
                PrintValueArray("NumericValues", numericValueTable, file);

                PrintValueArray("DigitValues", digitValueTable, file);

                file.Write("\n    }\n}");
            }
        }

        private static bool Verbose;

        private const string SOURCE_NAME = "CharUnicodeInfoData.cs";

        private static readonly Dictionary<string, byte> UnicodeCategoryMap = new Dictionary<string, byte>
        {
            ["Lu"] = (byte)UnicodeCategory.UppercaseLetter,
            ["Ll"] = (byte)UnicodeCategory.LowercaseLetter,
            ["Lt"] = (byte)UnicodeCategory.TitlecaseLetter,
            ["Lm"] = (byte)UnicodeCategory.ModifierLetter,
            ["Lo"] = (byte)UnicodeCategory.OtherLetter,
            ["Mn"] = (byte)UnicodeCategory.NonSpacingMark,
            ["Mc"] = (byte)UnicodeCategory.SpacingCombiningMark,
            ["Me"] = (byte)UnicodeCategory.EnclosingMark,
            ["Nd"] = (byte)UnicodeCategory.DecimalDigitNumber,
            ["Nl"] = (byte)UnicodeCategory.LetterNumber,
            ["No"] = (byte)UnicodeCategory.OtherNumber,
            ["Zs"] = (byte)UnicodeCategory.SpaceSeparator,
            ["Zl"] = (byte)UnicodeCategory.LineSeparator,
            ["Zp"] = (byte)UnicodeCategory.ParagraphSeparator,
            ["Cc"] = (byte)UnicodeCategory.Control,
            ["Cf"] = (byte)UnicodeCategory.Format,
            ["Cs"] = (byte)UnicodeCategory.Surrogate,
            ["Co"] = (byte)UnicodeCategory.PrivateUse,
            ["Pc"] = (byte)UnicodeCategory.ConnectorPunctuation,
            ["Pd"] = (byte)UnicodeCategory.DashPunctuation,
            ["Ps"] = (byte)UnicodeCategory.OpenPunctuation,
            ["Pe"] = (byte)UnicodeCategory.ClosePunctuation,
            ["Pi"] = (byte)UnicodeCategory.InitialQuotePunctuation,
            ["Pf"] = (byte)UnicodeCategory.FinalQuotePunctuation,
            ["Po"] = (byte)UnicodeCategory.OtherPunctuation,
            ["Sm"] = (byte)UnicodeCategory.MathSymbol,
            ["Sc"] = (byte)UnicodeCategory.CurrencySymbol,
            ["Sk"] = (byte)UnicodeCategory.ModifierSymbol,
            ["So"] = (byte)UnicodeCategory.OtherSymbol,
            ["Cn"] = (byte)UnicodeCategory.OtherNotAssigned,
        };

        /// <summary>
        /// Map BiDi symbols in UnicodeData.txt to their numeric values stored in the output table.
        /// </summary>
        private static readonly Dictionary<string, byte> BiDiCategory = new Dictionary<string, byte>
        {
            ["L"] = 0,    // Left-to-Right 
            ["LRE"] = 1,  // Left-to-Right Embedding 
            ["LRO"] = 2,  // Left-to-Right Override 
            ["R"] = 3,    // Right-to-Left 
            ["AL"] = 4,   // Right-to-Left Arabic 
            ["RLE"] = 5,  // Right-to-Left Embedding 
            ["RLO"] = 6,  // Right-to-Left Override 
            ["PDF"] = 7,  // Pop Directional Format 
            ["EN"] = 8,   // European Number 
            ["ES"] = 9,   // European Number Separator 
            ["ET"] = 10,  // European Number Terminator 
            ["AN"] = 11,  // Arabic Number 
            ["CS"] = 12,  // Common Number Separator 
            ["NSM"] = 13, // Non-Spacing Mark 
            ["BN"] = 14,  // Boundary Neutral 
            ["B"] = 15,   // Paragraph Separator 
            ["S"] = 16,   // Segment Separator 
            ["WS"] = 17,  // Whitespace 
            ["ON"] = 18,  // Other Neutrals 
            ["LRI"] = 19, // LeftToRightIsolate
            ["RLI"] = 20, // RightToLeftIsolate
            ["FSI"] = 21, // FirstStrongIsolate
            ["PDI"] = 22, // PopDirectionIsolate
        };

        // Store the current combinations of categories (Unicode category, BiDi category)
        private static readonly Dictionary<string, byte> CategoryValues = new Dictionary<string, byte>();

        private static readonly Dictionary<string, byte> NumberValues = new Dictionary<string, byte>();

        /// <summary>
        /// Check if we need to add a new item in the categoriesValueTable.  If yes,
        /// add one item and return the new item number.  Otherwise, return the existing
        /// item number.
        /// </summary>
        /// <param name="categoriesValueTable"></param>
        /// <param name="allCategoryValues">The combination of Unicode category and BiDi category.
        /// They should use the original form in UnicodeData.txt (such as "Cn" for not assigned and "L" for Left-To-Right")
        /// and are separated by a comma.</param>
        /// <returns>The item number in the categoriesValueTable</returns>
        private static byte GetCategoryValueItem(FlatDataTable categoriesValueTable, string allCategoryValues)
        {
            if (!CategoryValues.TryGetValue(allCategoryValues, out var categoryItem))
            {
                // This combination of Unicode category and BiDi category has not shown up before.
                if (CategoryValues.Count >= 255)
                    throw new InvalidOperationException("The possible number of values exceeds 255.");

                // Get the current element count of the hash table and update the category item
                categoryItem = (byte)CategoryValues.Count;
                CategoryValues.Add(allCategoryValues, categoryItem);
                // Add the category values.
                categoriesValueTable.AddData(categoryItem, allCategoryValues);
            }
            return categoryItem;
        }

        /// <summary>
        /// Read UnicodeData.txt and call DataTable.AddData() to add values for codepoints.
        /// </summary>
        private static void ReadSourceFile(string sourceFileName, DataTable categoriesIndexTable, FlatDataTable categoriesValueTable, DataTable numericIndexTable, FlatDataTable numericValueTable, FlatDataTable digitValueTable)
        {
            var lineCount = 0; // The line count
            var codePointCount = 0; // The count of the total characters in the file.

            Console.Write($"Read {sourceFileName}");

            // Field	Name in UnicodeData.txt
            // 0	Code value
            // 1	Character name
            // 2	General Category
            // 	
            // 3	Canonical Combining Classes
            // 4	Bidirectional Category
            // 5	Character Decomposition Mapping
            // 6	Decimal digit value
            // 7	Digit value
            // 8	Numeric value
            // 9	Mirrored
            // 10	Unicode 1.0 Name
            // 11	10646 comment field
            // 12	Uppercase Mapping
            // 13	Lowercase Mapping
            // 14	Titlecase Mapping

            using (StreamReader sourceFile = File.OpenText(sourceFileName))
                while (sourceFile.ReadLine() is string line)
                {
                    var fields = line.Split(';');
                    var code = uint.Parse(fields[0], NumberStyles.HexNumber);
                    var comments = fields[1];
                    var category = fields[2];

                    var bidiCategory = fields[4];
                    var decimalDigitValue = fields[6];
                    var digitValue = fields[7];
                    var numericValue = fields[8];

                    var allCategoryValues = category + "," + bidiCategory;
                    var allDigitValue = (decimalDigitValue == "" ? "255" : decimalDigitValue) + "," + (digitValue == "" ? "255" : digitValue);
                    var allNumValues = numericValue == "" ? "-1" : numericValue;
                    var allValues = allNumValues + "," + allDigitValue;

                    if (Verbose)
                    {
                        Console.WriteLine($"[{code:X4}]- Cat: [{category}], BiDi Category: [{bidiCategory}], Numeric: [{numericValue}], Comments: [{comments}]");
                    }

                    if (!NumberValues.TryGetValue(allValues, out var numItem))
                    {
                        if (NumberValues.Count >= 255)
                            throw new InvalidOperationException("The possible number of values exceeds 255.");
                        // Get the current element count of the hash table
                        numItem = (byte)NumberValues.Count;
                        NumberValues[allValues] = numItem;
                        numericValueTable.AddData(numItem, allNumValues);
                        digitValueTable.AddData(numItem, allDigitValue);
                    }

                    var categoryItem = GetCategoryValueItem(categoriesValueTable, allCategoryValues);

                    if (comments[0] == '<' && comments.EndsWith(", First>", StringComparison.Ordinal))
                    {
                        if (Verbose)
                        {
                            Console.WriteLine($"Range start: {code:X4} [{category}] [{comments}]");
                        }

                        // Read the next line to get the end of the range.
                        var endFields = sourceFile.ReadLine().Split(';');
                        var codeEndRange = uint.Parse(endFields[0], NumberStyles.HexNumber);
                        var valueEndRange = endFields[2];
                        var commentsEndRange = endFields[1];

                        if (Verbose)
                        {
                            Console.WriteLine($"Range   end: {codeEndRange:X4} [{valueEndRange}] [{commentsEndRange}]");
                        }

                        if (category != valueEndRange)
                        {
                            Console.WriteLine("Different categories in the beginning of the range and the end of the range");
                            Environment.Exit(1);
                        }

                        // Add data for a range of code points
                        for (var i = code; i <= codeEndRange; i++)
                        {
                            categoriesIndexTable.AddData(i, categoryItem);
                            numericIndexTable.AddData(i, numItem);
                            codePointCount++;
                            if (Verbose)
                            {
                                Console.WriteLine($"Read: {i:X8} [{allCategoryValues}]");
                            }
                        }
                    }
                    else
                    {
                        // Add data for a single code point.
                        categoriesIndexTable.AddData(code, categoryItem);
                        numericIndexTable.AddData(code, numItem);
                        codePointCount++;
                        if (Verbose)
                        {
                            Console.WriteLine($"Read: {code:X8} [{allCategoryValues}]");
                        }
                    }
                    lineCount++;
                    if (lineCount % 256 == 0)
                    {
                        Console.Write('.');
                    }
                }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"    Total lines in the file: {lineCount}");
            Console.WriteLine($"    Total characters: {codePointCount}");

            var allValueCount = CategoryValues.Count;
            Console.WriteLine($"    Total possible categories values: {allValueCount + 1}.  Maximum allowed: 256");

            allValueCount = NumberValues.Count;
            Console.WriteLine($"    Total possible number values: {allValueCount + 1}.  Maximum allowed: 256");
            Console.WriteLine($"    Finish reading {sourceFileName}.");
        }

        private static byte[] GetCategoriesValueBytes(string value)
        {
            if (Verbose)
                Console.WriteLine($"[{value}]");

            var values = value.Split(',');
            var unicodeCategoryValue = values[0];
            var bidiCategoryValue = values[1];

            return new[] { GetUnicodeCategoryValue(unicodeCategoryValue), GetBiDiCategoryValue(bidiCategoryValue) };
        }

        private static byte[] GetNumericValueBytes(string value)
        {
            double d;
            var i = value.IndexOf('/');
            if (i < 0)
                d = double.Parse(value, CultureInfo.InvariantCulture);
            else
                d = double.Parse(value.Substring(0, i), CultureInfo.InvariantCulture) / double.Parse(value.Substring(i + 1), CultureInfo.InvariantCulture);
            return BitConverter.GetBytes(d);
        }

        private static byte[] GetDigitValueBytes(string value)
        {
            if (Verbose)
                Console.WriteLine($"[{value}]");

            var values = value.Split(',');
            var decimalDigitValue = values[0];
            var digitValue = values[1];

            return new[] { byte.Parse(decimalDigitValue), byte.Parse(digitValue) };
        }

        /// <summary>
        /// Map a Unicode category symbol in UnicodeData.txt to a numeric value
        /// </summary>
        /// <param name="str">A two-letter abbreviation of Unicode category</param>
        /// <returns>A numeric value for the corresponding two-letter Unicode category</returns>
        private static byte GetUnicodeCategoryValue(string str)
        {
            return UnicodeCategoryMap.TryGetValue(str, out var v) ? v : throw new ArgumentException($"The str [{str}] is not a valid two-letter Unicode category.");
        }

        private static byte GetBiDiCategoryValue(string str)
        {
            return BiDiCategory.TryGetValue(str, out var v) ? v : throw new ArgumentException($"The str [{str}] is not a valid BiDi category.");
        }

        private static void PrintSourceIndexArray(string tableName, DataTable d, StreamWriter file)
        {
            Console.WriteLine("    ******************************** .");

            var levels = d.GetBytes();

            PrintByteArray(tableName, file, levels[0]);
            PrintByteArray(tableName.Replace('1', '2'), file, levels[1]);
            PrintByteArray(tableName.Replace('1', '3'), file, levels[2]);
        }

        private static void PrintValueArray(string tableName, FlatDataTable d, StreamWriter file)
        {
            Console.WriteLine("    ******************************** .");
            PrintByteArray(tableName, file, d.GetBytesFlat());
        }

        private static void PrintByteArray(string tableName, StreamWriter file, byte[] str)
        {
            file.Write("\n        private static ReadOnlySpan<byte> " + tableName + " => new byte[" + str.Length + "]\n        {\n");
            file.Write("            0x{0:x2}", str[0]);
            for (var i = 1; i < str.Length; i++)
            {
                file.Write(i % 16 == 0 ? ",\n            " : ", ");
                file.Write("0x{0:x2}", str[i]);
            }
            file.Write("\n        };\n");
        }
    }
}
