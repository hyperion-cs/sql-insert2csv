﻿using System.Diagnostics;

namespace SqlInsert2Sql
{
    public static class Processor
    {
        public static void Start(string input,
            string outputDir,
            string inputEnc,
            string outputEnc,
            char idQuote,
            char valQuote,
            char escape,
            string nullVal,
            int bufferInput,
            int bufferOutput,
            bool showRowsCount
            )
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using var dataReader = new DataReader(input, inputEnc, bufferInput);
            using var dataWriter = new DataWriter(outputDir, outputEnc, bufferOutput);

            var tokenizer = new Tokenizer(dataReader, idQuote, valQuote, escape, nullVal);

            var blockOffset = 0;
            while (true)
            {
                var insertStart = tokenizer.InsertStart(blockOffset);
                if (!insertStart.Success)
                {
                    Console.WriteLine("There are no more INSERT statements.");
                    break;
                }

                var tableName = tokenizer.TableName(insertStart.Offset);
                if (!tableName.Success)
                {
                    DebugAndThrow(dataReader, blockOffset, "Error during token TableName.");
                }

                dataWriter.NextInsert(tokenizer.InsertsCaptured, tableName.Data);

                var columnNamesStart = tokenizer.ColumnNamesStart(tableName.Offset);
                blockOffset = columnNamesStart.Offset; // This token returns an offset in any case.

                if (columnNamesStart.Success)
                {
                    while (true)
                    {
                        var columnName = tokenizer.ColumnName(blockOffset);
                        if (!columnName.Success)
                        {
                            DebugAndThrow(dataReader, blockOffset, "Error during token ColumnName.");
                        }

                        dataWriter.WriteField(columnName.Data);

                        var lookAfterColumnName = tokenizer.LookAfterColumnName(columnName.Offset);
                        if (!lookAfterColumnName.Success)
                        {
                            DebugAndThrow(dataReader, blockOffset, "Error during token LookAfterColumnName.");
                        }

                        blockOffset = lookAfterColumnName.Offset;
                        if (lookAfterColumnName.Data == ListPosition.End)
                        {
                            dataWriter.NextLine();
                            break;
                        }
                    }
                }
                else
                {
                    var withoutColumnNamesDefinition = tokenizer.WithoutColumnNamesDefinition(blockOffset);
                    if (!withoutColumnNamesDefinition.Success)
                    {
                        DebugAndThrow(dataReader, blockOffset, "Error during tokens ColumnNamesStart/WithoutColumnNamesDefinition.");
                    }

                    blockOffset = withoutColumnNamesDefinition.Offset;
                }

                while (true)
                {
                    var rowStart = tokenizer.RowStart(blockOffset);
                    if (!rowStart.Success)
                    {
                        DebugAndThrow(dataReader, blockOffset, "Error during token RowStart.");
                    }

                    blockOffset = rowStart.Offset;
                    while (true)
                    {
                        var rowValue = tokenizer.RowValue(blockOffset);
                        if (!rowValue.Success)
                        {
                            DebugAndThrow(dataReader, blockOffset, "Error during token RowValue.");
                        }

                        dataWriter.WriteField(rowValue.Data);

                        var lookAfterRowValue = tokenizer.LookAfterRowValue(rowValue.Offset);
                        if (!lookAfterRowValue.Success)
                        {
                            DebugAndThrow(dataReader, blockOffset, "Error during token LookAfterRowValue.");
                        }

                        blockOffset = lookAfterRowValue.Offset;
                        if (lookAfterRowValue.Data == ListPosition.End)
                        {
                            dataWriter.NextLine();
                            break;
                        }
                    }

                    var lookAfterRow = tokenizer.LookAfterRow(blockOffset);
                    if (!lookAfterRow.Success)
                    {
                        DebugAndThrow(dataReader, blockOffset, "Error during token LookAfterRow.");
                    }

                    blockOffset = lookAfterRow.Offset;
                    if (lookAfterRow.Data == ListPosition.End)
                    {
                        break;
                    }
                }

                if (showRowsCount)
                {
                    Console.WriteLine($"INSERT statement #{tokenizer.InsertsCaptured} done. " +
                                      $"Rows: {tokenizer.LastInsertRowsCount}");
                }
            }

            stopWatch.Stop();

            Console.WriteLine($"Done. Total rows {tokenizer.TotalRowsCount}. " +
                              $"Time elapsed: {stopWatch.Elapsed}");
        }

        private static void DebugAndThrow(DataReader dataReader, int blockOffset, string problem)
        {
            var info = $"Block num: {dataReader.BlockNum}\n" +
                       $"Block offset: {blockOffset}\n" +
                       $"Problem: {problem}";

            File.WriteAllText("debug.info", info);
            File.WriteAllText("debug.buffer", new string(dataReader.Buffer));

            throw new Exception(problem);
        }
    }
}
