namespace SqlInsert2Sql;

/// <summary>Token parsing results</summary>
public struct TPR
{
    public bool Success { get; set; }
    public int Offset { get; set; }
    public object Data { get; set; }
}
