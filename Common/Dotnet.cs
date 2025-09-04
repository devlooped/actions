// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Devlooped;

public static class Dotnet
{
    public static FileInfo? Path { get; }

    static Dotnet()
    {
        var muxerFileName = ExecutableName("dotnet");
        var fxDepsFile = GetDataFromAppDomain("FX_DEPS_FILE");

        if (string.IsNullOrEmpty(fxDepsFile))
            return;

        var muxerDir = new FileInfo(fxDepsFile).Directory?.Parent?.Parent?.Parent;
        if (muxerDir == null)
            return;

        var muxerCandidate = new FileInfo(System.IO.Path.Combine(muxerDir.FullName, muxerFileName));
        if (muxerCandidate.Exists)
            Path = muxerCandidate;
    }

    public static Process Start(ProcessStartInfo info)
    {
        if (Path == null)
            throw new InvalidOperationException("Could not determine the path to the dotnet executable.");

        info.FileName = Path!.FullName;
        return Process.Start(info) ?? throw new InvalidOperationException("Could not start the dotnet process.");
    }

    static string? GetDataFromAppDomain(string propertyName)
        => AppContext.GetData(propertyName) as string;

    static string ExecutableName(this string withoutExtension)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? withoutExtension + ".exe"
            : withoutExtension;
}
