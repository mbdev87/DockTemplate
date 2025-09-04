using System;

namespace DockTemplate.Services;

public class ErrorMessage
{
    public ErrorEntry? Entry { get; set; }
}

public class BatchedErrorMessage
{
    public ErrorEntry[] Entries { get; set; } = Array.Empty<ErrorEntry>();
}
