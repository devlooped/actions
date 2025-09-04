#:package Spectre.Console@0.50.*
using System.Runtime.InteropServices;
using System.Text;
using static Spectre.Console.AnsiConsole;

// Ensure even the dev console on Windows can display UTF-8 characters.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    System.Console.InputEncoding = System.Console.OutputEncoding = Encoding.UTF8;

MarkupLine("[blue]Hello[/] :globe_showing_americas:!");