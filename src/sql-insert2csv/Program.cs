using SqlInsert2Sql;
using System.CommandLine;

var rootCommand = new RootCommand("SQL INSERT statements to CSV converter.\n" +
                                  "A.sql dump is expected for input.\n" +
                                  "(c) MIT License. https://github.com/hyperion-cs/sql-insert2csv");

var input = new Option<string>(name: "--input", description: "Input file path") { IsRequired = true };
input.AddAlias("-i");

var outputDir = new Option<string>(name: "--output-dir", description: "Output dir path") { IsRequired = true };
outputDir.AddAlias("-o");

var inputEnc = new Option<string>(name: "--input-enc", description: "Input encoding (charset)");
inputEnc.SetDefaultValue("utf-8");
inputEnc.AddAlias("-c");

var outputEnc = new Option<string>(name: "--output-enc", description: "Output encoding (charset)");
outputEnc.SetDefaultValue("utf-8");
outputEnc.AddAlias("-C");

var idQuote = new Option<char>(name: "--id-quote", description: "Identifier (table/column name) quote char");
idQuote.SetDefaultValue('`');
idQuote.AddAlias("-q");

var valQuote = new Option<char>(name: "--val-quote", description: "Column value quote char");
valQuote.SetDefaultValue('\'');
valQuote.AddAlias("-Q");

var escape = new Option<char>(name: "--escape", description: "Escape char");
escape.SetDefaultValue('\\');
escape.AddAlias("-e");

var nullVal = new Option<string>(name: "--null-val", description: "Null value string");
nullVal.SetDefaultValue("NULL");
nullVal.AddAlias("-n");

var bufferInput = new Option<int>(name: "--buffer-input", description: "Input buffer size (characters number)");
bufferInput.SetDefaultValue(16384);
bufferInput.AddAlias("-b");

var bufferOutput = new Option<int>(name: "--buffer-output", description: "Output buffer size (bytes number)");
bufferOutput.SetDefaultValue(1048576);
bufferOutput.AddAlias("-B");

var showRowsCount = new Option<bool>(name: "--show-rows-count", description: "Shows the number of rows in each INSERT statement");
showRowsCount.SetDefaultValue(false);
showRowsCount.AddAlias("-s");

rootCommand.AddOption(input);
rootCommand.AddOption(outputDir);
rootCommand.AddOption(inputEnc);
rootCommand.AddOption(outputEnc);
rootCommand.AddOption(idQuote);
rootCommand.AddOption(valQuote);
rootCommand.AddOption(escape);
rootCommand.AddOption(nullVal);
rootCommand.AddOption(bufferInput);
rootCommand.AddOption(bufferOutput);
rootCommand.AddOption(showRowsCount);

rootCommand.SetHandler<string, string, string, string, char, char, char, string, int, int, bool>
     (Processor.Start, input, outputDir, inputEnc, outputEnc, idQuote, valQuote,
                       escape, nullVal, bufferInput, bufferOutput, showRowsCount);

return rootCommand.Invoke(args);
