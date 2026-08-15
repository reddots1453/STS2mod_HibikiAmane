namespace MaidenSuccubus.Util;

public static class Safe
{
    public static void Run(Action action, string operation)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"[{operation}] {ex.Message}\n{ex.StackTrace}");
        }
    }
}
