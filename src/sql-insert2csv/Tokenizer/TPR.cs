namespace SqlInsert2Sql;

/// <summary>Token parsing result without data</summary>
public struct TPR
{
    public bool Success { get; set; }
    public int Offset { get; set; }
}

/// <summary>Token parsing result with data</summary>
public struct TPR<T>
{
    public bool Success { get; set; }
    public int Offset { get; set; }
    public T Data { get; set; }
}
