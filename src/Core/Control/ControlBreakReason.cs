namespace MaidenSuccubus.Core.Control;

public enum ControlBreakReason
{
    Direct,
    Escaped,
    SourceDied,
}

public enum ControlResolutionResult
{
    Ignored,
    Blocked,
    Applied,
}
