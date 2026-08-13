/// <summary>
/// The host's entry point, declared as a partial so the type generated from the top-level statements
/// in <c>Program.cs</c> is visible outside this assembly.
/// </summary>
/// <remarks>
/// The compiler-generated entry-point class is internal by default. Widening it here is what lets the
/// test project name it as the type argument of its web application factory, which boots this exact
/// host rather than a re-declared copy of its configuration.
/// </remarks>
public partial class Program
{
}
