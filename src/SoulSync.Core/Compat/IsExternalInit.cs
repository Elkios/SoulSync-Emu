#if !NET5_0_OR_GREATER
// The C# compiler requires this type to emit `init`-only setters and positional records.
// It exists in .NET 5+/.NET Standard 2.1; this shim lets SoulSync.Core compile for net48
// (so the .NET Framework EmuHawk host can reference the engine directly).
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
