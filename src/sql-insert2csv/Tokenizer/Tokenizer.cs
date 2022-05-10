using System.Text;

namespace SqlInsert2Sql;

public class Tokenizer
{
    protected readonly DataReader _dataReader;

    protected readonly char[] TABLE_NAME_AFC = new[] { CH_BRACKET_IN };
    protected readonly char[] COLUMN_NAME_AFC = new[] { CH_COMMA, CH_BRACKET_OUT };
    protected readonly char[] ROW_VALUE_AFC = new[] { CH_COMMA, CH_BRACKET_OUT };

    protected const string INSERT_START_PATTERN = "+INSERT+INTO+";
    protected const string LAST_COLUMN_NAME_PATTERN = "*)*VALUES*";

    protected const char CH_BRACKET_IN = '(';
    protected const char CH_BRACKET_OUT = ')';
    protected const char CH_COMMA = ',';
    protected const char CH_SEMICOLON = ';';
    protected const char CH_NEGATIVE = '-';
    protected const char CH_DOT = '.';
    protected const char CH_UNDERSCORE = '_';

    /// <summary>Identifier (table/column name) quote char.</summary>
    protected readonly char ID_QUOTE;

    /// <summary>Column value quote char.</summary>
    protected readonly char VALUE_QUOTE;

    /// <summary>Escape char.</summary>
    protected readonly char ESCAPE_CHAR;

    /// <summary>Null value string.</summary>
    protected readonly string NULL_VALUE;

    public long InsertsCaptured { get; private set; } = 0;
    public long LastInsertRowsCount { get; private set; } = 0;
    public long TotalRowsCount { get; private set; } = 0;

    public Tokenizer(DataReader dataReader, char idQoute, char valQuote, char escape, string nullVal)
    {
        _dataReader = dataReader;

        ID_QUOTE = idQoute;
        VALUE_QUOTE = valQuote;
        ESCAPE_CHAR = escape;
        NULL_VALUE = nullVal;
    }

    public TPR InsertStart(int initOffset)
    {
        var cp = ContainsPattern(INSERT_START_PATTERN, initOffset);
        if (cp > 0)
        {
            InsertsCaptured++;
            LastInsertRowsCount = 0;

            return new TPR
            {
                Success = true,
                Offset = cp
            };
        }

        return new TPR();
    }

    public TPR TableName(int initOffset)
    {
        return Val(initOffset, ID_QUOTE, TABLE_NAME_AFC, ESCAPE_CHAR);
    }

    public TPR ColumnNamesStart(int initOffset)
    {
        var blockOffset = initOffset - 1;
        while (true)
        {
            if (++blockOffset == _dataReader.BufferSize)
            {
                if (_dataReader.TryReadBlock()) blockOffset = 0;
                else break;
            }

            if (char.IsWhiteSpace(_dataReader.Buffer[blockOffset])) continue;

            if (_dataReader.Buffer[blockOffset] == CH_BRACKET_IN)
            {
                return new TPR
                {
                    Success = true,
                    Offset = ++blockOffset
                };
            }

            break;
        }

        return new TPR();
    }

    public TPR ColumnName(int initOffset)
    {
        return Val(initOffset, ID_QUOTE, COLUMN_NAME_AFC, ESCAPE_CHAR);
    }

    public TPR LookAfterColumnName(int initOffset)
    {
        var blockOffset = SkipWhitespaces(initOffset);

        if (blockOffset == _dataReader.BufferSize)
        {
            if (_dataReader.TryReadBlock()) blockOffset = 0;
            else return new TPR();
        }

        if (_dataReader.Buffer[blockOffset] == CH_COMMA)
        {
            return new TPR { Success = true, Data = ListPosition.Next, Offset = ++blockOffset };
        }

        blockOffset = ContainsPattern(LAST_COLUMN_NAME_PATTERN, blockOffset);
        if (blockOffset > 0)
        {
            return new TPR { Success = true, Data = ListPosition.End, Offset = blockOffset };
        }

        return new TPR();
    }

    public TPR RowStart(int initOffset)
    {
        var blockOffset = SkipWhitespaces(initOffset);
        if (blockOffset == _dataReader.BufferSize)
        {
            if (_dataReader.TryReadBlock()) blockOffset = 0;
            else return new TPR();
        }

        if (_dataReader.Buffer[blockOffset] == CH_BRACKET_IN)
        {
            return new TPR { Success = true, Offset = ++blockOffset };
        }

        return new TPR();
    }

    public TPR RowValue(int initOffset)
    {
        return Val(initOffset, VALUE_QUOTE, ROW_VALUE_AFC, ESCAPE_CHAR);
    }

    public TPR LookAfterRowValue(int initOffset)
    {
        var blockOffset = SkipWhitespaces(initOffset);

        if (blockOffset == _dataReader.BufferSize)
        {
            if (_dataReader.TryReadBlock()) blockOffset = 0;
            else return new TPR();
        }

        LastInsertRowsCount++;
        TotalRowsCount++;

        if (_dataReader.Buffer[blockOffset] == CH_COMMA)
        {
            return new TPR { Success = true, Data = ListPosition.Next, Offset = ++blockOffset };
        }

        if (_dataReader.Buffer[blockOffset] == CH_BRACKET_OUT)
        {
            return new TPR { Success = true, Data = ListPosition.End, Offset = ++blockOffset };
        }

        return new TPR();
    }

    public TPR LookAfterRow(int initOffset)
    {
        var blockOffset = SkipWhitespaces(initOffset);
        if (blockOffset == _dataReader.BufferSize)
        {
            if (_dataReader.TryReadBlock()) blockOffset = 0;
            else return new TPR();
        }

        if (_dataReader.Buffer[blockOffset] == CH_COMMA)
        {
            return new TPR { Success = true, Data = ListPosition.Next, Offset = ++blockOffset };
        }

        if (_dataReader.Buffer[blockOffset] == CH_SEMICOLON)
        {
            return new TPR { Success = true, Data = ListPosition.End, Offset = ++blockOffset };
        }

        return new TPR();
    }

    protected TPR Val(int initOffset, char quote, char[] afterCharset, char escape)
    {
        var val = new StringBuilder();
        var startFound = false;
        var quotedStart = false;
        var possiblyContainsPrefix = false;
        char prevBlockLastChar = '\0'; // Make compiler happy

        var blockOffset = SkipWhitespaces(initOffset) - 1;
        while (true)
        {
            if (++blockOffset == _dataReader.BufferSize)
            {
                prevBlockLastChar = _dataReader.Buffer.Last();

                if (_dataReader.TryReadBlock()) blockOffset = 0;
                else break;
            }

            var c = _dataReader.Buffer[blockOffset];

            if (!startFound)
            {
                startFound = true;

                if (c == quote)
                {
                    quotedStart = true;
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    possiblyContainsPrefix = true;
                    continue;
                }

                if (IsPrimitiveChar(c))
                {
                    continue;
                }

                return new TPR();
            }

            if (quotedStart)
            {
                if (c == escape)
                {
                    if (++blockOffset == _dataReader.BufferSize)
                    {
                        if (_dataReader.TryReadBlock()) blockOffset = 0;
                        else return new TPR();
                    }

                    val.Append(_dataReader.Buffer[blockOffset]);
                    continue;
                }

                if (c == quote)
                {
                    blockOffset = SkipWhitespaces(++blockOffset);
                    return GetValIfEndCorrect(afterCharset, val, blockOffset);
                }

                val.Append(c);
                continue;
            }

            if (possiblyContainsPrefix)
            {
                if (c == quote)
                {
                    quotedStart = true;
                    continue;
                }

                val.Append(blockOffset > 0 ? _dataReader.Buffer[blockOffset - 1] : prevBlockLastChar);
                possiblyContainsPrefix = false;
            }

            if (IsPrimitiveChar(c))
            {
                val.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                blockOffset = SkipWhitespaces(blockOffset);
            }

            return GetValIfEndCorrect(afterCharset, val, blockOffset);
        }

        return new TPR();
    }

    protected TPR GetValIfEndCorrect(char[] afterCharset, StringBuilder val, int blockOffset)
    {
        if (blockOffset >= _dataReader.BufferSize)
        {
            if (_dataReader.TryReadBlock()) blockOffset = 0;
            else return new TPR();
        }

        if (afterCharset.Any(ac => ac == _dataReader.Buffer[blockOffset]))
        {
            var data = val.ToString();
            return new TPR
            {
                Success = true,
                Data = data == NULL_VALUE ? string.Empty : data,
                Offset = blockOffset
            };
        }

        return new TPR();
    }

    /// <summary>Returns true if the char does not require quotes.</summary>
    protected static bool IsPrimitiveChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == CH_UNDERSCORE || c == CH_NEGATIVE || c == CH_DOT;
    }

    protected int SkipWhitespaces(int initOffset)
    {
        var blockOffset = initOffset - 1;
        while (true)
        {
            if (++blockOffset == _dataReader.BufferSize)
            {
                if (_dataReader.TryReadBlock()) blockOffset = 0;
                else break;
            }

            if (!char.IsWhiteSpace(_dataReader.Buffer[blockOffset])) break;
        }

        return blockOffset;
    }

    /// <summary>
    /// Try to find the simplest pattern in the string (case insensitive)..
    /// It is a "greedy" algorithm, working for a time complexity of O(N).
    /// </summary>
    /// <returns>The index of the last found character, or -1 if no occurrence was found.</returns>
    protected int ContainsPattern(string pattern, int blockOffset)
    {
        const char CH_WS_M = '*'; // The equivalent of \s*
        const char CH_WS_P = '+'; // The equivalent of (?:^|\s+)

        var ptnPos = 0;
        var leftHasWs = _dataReader.BlockNum == 0;
        var hasFalseIncrement = false;

        var i = blockOffset - 1;
        while (true)
        {
            if (++i == _dataReader.BufferSize)
            {
                if (_dataReader.TryReadBlock()) i = 0;
                else break;
            }

            hasFalseIncrement = false;

            if (pattern[ptnPos] == CH_WS_M)
            {
                if (!char.IsWhiteSpace(_dataReader.Buffer[i]))
                {
                    hasFalseIncrement = true;

                    if (++ptnPos >= pattern.Length) break;
                }
                else continue;
            }

            if (pattern[ptnPos] == CH_WS_P)
            {
                if (char.IsWhiteSpace(_dataReader.Buffer[i]))
                {
                    leftHasWs = true;
                    continue;
                }

                if (leftHasWs)
                {
                    if (ptnPos + 1 >= pattern.Length) break;

                    ptnPos++;
                    leftHasWs = false;
                }
            }

            if (char.ToUpper(_dataReader.Buffer[i]) == pattern[ptnPos])
            {
                hasFalseIncrement = true;
                leftHasWs = false;
                ptnPos++;

                continue;
            }

            ptnPos = 0;
        }

        if (hasFalseIncrement)
        {
            ptnPos--;
        }

        // The case when the string has already ended,
        // but the last CH_WS_M rule has not been processed in the pattern.
        if (i == _dataReader.BufferSize && ptnPos == pattern.Length - 2 && pattern.Last() == CH_WS_M) ptnPos++;

        return ptnPos >= pattern.Length - 1 ? i : -1;
    }
}
