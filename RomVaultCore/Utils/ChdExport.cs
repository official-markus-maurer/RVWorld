using FileScanner;
using RomVaultCore;
using RomVaultCore.RvDB;
using RomVaultCore.Scanner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RomVaultCore.Utils;

public static class ChdExport
{
    public static int Export(string chdPath, string outputDir, IReadOnlyList<string> expectedMemberNames, out string report)
    {
        List<RvFile> expected = new List<RvFile>();
        foreach (string n in expectedMemberNames ?? Array.Empty<string>())
            expected.Add(new RvFile(FileType.File) { Name = n });
        return Export(chdPath, outputDir, expected, out report);
    }

    public static int Export(string chdPath, string outputDir, IReadOnlyList<RvFile> expectedMembers, out string report)
    {
        report = "";
        if (string.IsNullOrWhiteSpace(chdPath) || !System.IO.File.Exists(chdPath))
        {
            report = "export failed: CHD not found";
            return 2;
        }
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            report = "export failed: output directory not specified";
            return 2;
        }

        try
        {
            Directory.CreateDirectory(outputDir);
        }
        catch (Exception ex)
        {
            report = "export failed: cannot create output directory: " + ex.Message;
            return 2;
        }

        List<RvFile> expectedList = expectedMembers?.Where(e => e != null && !string.IsNullOrWhiteSpace(e.Name)).ToList() ?? new List<RvFile>();
        bool expectsIso = expectedList.Any(n => n.Name.EndsWith(".iso", StringComparison.OrdinalIgnoreCase));
        bool expectsGdi = expectedList.Any(n => n.Name.EndsWith(".gdi", StringComparison.OrdinalIgnoreCase));
        string expectedIsoName = expectedList.FirstOrDefault(n => n.Name.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))?.Name;
        string expectedCueName = expectedList.FirstOrDefault(n => n.Name.EndsWith(".cue", StringComparison.OrdinalIgnoreCase))?.Name;
        string expectedGdiName = expectedList.FirstOrDefault(n => n.Name.EndsWith(".gdi", StringComparison.OrdinalIgnoreCase))?.Name;

        string baseTempDir = DB.GetToSortCache()?.FullName ?? Environment.CurrentDirectory;
        string tempDir = System.IO.Path.Combine(baseTempDir, "__RomVault.chdexport." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string chdman = FindChdmanExePath();
            IChdExtractor extractor = new ChdmanChdExtractor(chdman, tempDir);

            long? logicalSize = TryGetChdLogicalSizeBytes(extractor, chdPath);
            if (logicalSize.HasValue)
            {
                long free = GetFreeSpaceBytes(tempDir);
                if (free > 0 && free < logicalSize.Value + 256L * 1024 * 1024)
                {
                    report = $"export failed: insufficient free space. required={logicalSize.Value} free={free}";
                    return 4;
                }
            }

            List<string> exported = new List<string>();
            List<string> verifyErrors = new List<string>();

            if (expectsIso)
            {
                string destIsoName = string.IsNullOrWhiteSpace(expectedIsoName) ? "image.iso" : expectedIsoName;
                string destIso = System.IO.Path.Combine(outputDir, destIsoName);

                if (Settings.rvSettings != null && Settings.rvSettings.ChdStreamingEnabled)
                {
                    try
                    {
                        using (Stream s = CHDSharpLib.ChdLogicalStream.OpenRead(chdPath))
                        using (Stream o = System.IO.File.Create(destIso))
                        {
                            s.CopyTo(o);
                        }
                        exported.Add(destIsoName);
                    }
                    catch (Exception ex)
                    {
                        report = "export failed: streaming write iso: " + ex.Message;
                        return 3;
                    }
                }
                else
                {
                    string outIso = System.IO.Path.Combine(tempDir, "image.iso");
                    if (!extractor.ExtractDvd(chdPath, outIso, out string err))
                    {
                        report = "export failed: extractdvd: " + err;
                        return 3;
                    }
                    if (!System.IO.File.Exists(outIso))
                    {
                        report = "export failed: extractdvd produced no ISO";
                        return 3;
                    }
                    System.IO.File.Copy(outIso, destIso, true);
                    exported.Add(destIsoName);
                }

                RvFile expectedIso = expectedList.FirstOrDefault(n => n.Name.EndsWith(".iso", StringComparison.OrdinalIgnoreCase));
                if (expectedIso != null)
                    VerifyFileAgainstExpected(destIso, expectedIso, verifyErrors);

                report = BuildExportReport(chdPath, outputDir, exported, verifyErrors);
                return verifyErrors.Count == 0 ? 0 : 5;
            }

            bool wantsDescriptor = !string.IsNullOrWhiteSpace(expectedCueName) || !string.IsNullOrWhiteSpace(expectedGdiName);
            if (!wantsDescriptor && Settings.rvSettings != null && Settings.rvSettings.ChdStreamingEnabled)
            {
                if (CHDSharpLib.ChdMetadata.TryReadCdTrackLayout(chdPath, out List<CHDSharpLib.ChdCdTrackInfo> cdTracks, out string metaErr) && cdTracks.Count > 0)
                {
                    Dictionary<int, RvFile> expByTrack = BuildExpectedTrackMap(expectedList);
                    List<RvFile> expectedDataFiles = expectedList.Where(e => IsTrackDataFile(e.Name)).ToList();
                    expectedDataFiles.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

                    Dictionary<int, CHDSharpLib.ChdCdTrackInfo> trackByNo = new Dictionary<int, CHDSharpLib.ChdCdTrackInfo>();
                    for (int i = 0; i < cdTracks.Count; i++)
                        trackByNo[cdTracks[i].TrackNo] = cdTracks[i];

                    Dictionary<int, string> destNameByTrackNo = new Dictionary<int, string>();
                    HashSet<string> usedExpected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in expByTrack)
                    {
                        if (trackByNo.ContainsKey(kvp.Key) && kvp.Value?.Name != null)
                        {
                            destNameByTrackNo[kvp.Key] = kvp.Value.Name;
                            usedExpected.Add(kvp.Value.Name);
                        }
                    }

                    Dictionary<ulong, List<int>> tracksBySize = new Dictionary<ulong, List<int>>();
                    foreach (var t in cdTracks)
                    {
                        if (destNameByTrackNo.ContainsKey(t.TrackNo))
                            continue;
                        ulong size = (ulong)Math.Max(0, t.Frames) * (ulong)Math.Max(1, t.SectorSize);
                        if (!tracksBySize.TryGetValue(size, out var list))
                        {
                            list = new List<int>();
                            tracksBySize[size] = list;
                        }
                        list.Add(t.TrackNo);
                    }

                    for (int i = 0; i < expectedDataFiles.Count; i++)
                    {
                        RvFile exp = expectedDataFiles[i];
                        if (exp?.Name == null || usedExpected.Contains(exp.Name))
                            continue;
                        ulong size = exp.Size ?? 0;
                        if (size == 0)
                            continue;
                        if (!tracksBySize.TryGetValue(size, out var cands))
                            continue;
                        if (cands.Count != 1)
                            continue;
                        destNameByTrackNo[cands[0]] = exp.Name;
                        usedExpected.Add(exp.Name);
                        cands.Clear();
                    }

                    List<int> remainingTracks = cdTracks.Select(t => t.TrackNo).Where(tno => !destNameByTrackNo.ContainsKey(tno)).ToList();
                    remainingTracks.Sort();
                    int fallback = 0;
                    for (int i = 0; i < expectedDataFiles.Count && fallback < remainingTracks.Count; i++)
                    {
                        RvFile exp = expectedDataFiles[i];
                        if (exp?.Name == null || usedExpected.Contains(exp.Name))
                            continue;
                        destNameByTrackNo[remainingTracks[fallback++]] = exp.Name;
                        usedExpected.Add(exp.Name);
                    }

                    try
                    {
                        using (Stream logical = CHDSharpLib.ChdLogicalStream.OpenRead(chdPath))
                        {
                            ulong cur = 0;
                            foreach (var t in cdTracks)
                            {
                                ulong start = (ulong)Math.Max(0, t.StartFrame) * (ulong)Math.Max(1, t.SectorSize);
                                ulong len = (ulong)Math.Max(0, t.Frames) * (ulong)Math.Max(1, t.SectorSize);
                                if (start < cur)
                                    start = cur;
                                if (start > cur)
                                {
                                    SkipBytes(logical, start - cur);
                                    cur = start;
                                }

                                if (destNameByTrackNo.TryGetValue(t.TrackNo, out string destName) && !string.IsNullOrWhiteSpace(destName))
                                {
                                    string destPath = System.IO.Path.Combine(outputDir, destName);
                                    using (Stream o = System.IO.File.Create(destPath))
                                    {
                                        CopyBytes(logical, o, len);
                                    }
                                    exported.Add(destName);
                                    RvFile expected = expectedList.FirstOrDefault(e => string.Equals(e.Name, destName, StringComparison.OrdinalIgnoreCase));
                                    if (expected != null)
                                        VerifyFileAgainstExpected(destPath, expected, verifyErrors);
                                }
                                else
                                {
                                    SkipBytes(logical, len);
                                }
                                cur += len;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        report = "export failed: cd streaming: " + ex.Message;
                        return 3;
                    }

                    exported.Sort(StringComparer.OrdinalIgnoreCase);
                    report = BuildExportReport(chdPath, outputDir, exported, verifyErrors);
                    return verifyErrors.Count == 0 ? 0 : 5;
                }
                else
                {
                    verifyErrors.Add("streaming metadata unavailable: " + metaErr);
                }
            }

            string outDescriptor = System.IO.Path.Combine(tempDir, expectsGdi ? "disc.gdi" : "disc.cue");
            if (Settings.rvSettings != null && Settings.rvSettings.ChdStreamingEnabled)
            {
                if (CHDSharpLib.ChdMetadata.TryReadCdTrackLayout(chdPath, out var cdTracks, out string metaErr))
                {
                    try
                    {
                        var expByTrack = BuildExpectedTrackMap(expectedList);
                        string descText = expectsGdi
                            ? ChdDescriptorGenerator.BuildGdi(cdTracks, expByTrack)
                            : ChdDescriptorGenerator.BuildCue(cdTracks, expByTrack);
                        System.IO.File.WriteAllText(outDescriptor, descText ?? "");
                    }
                    catch (Exception ex)
                    {
                        verifyErrors.Add("descriptor streaming generation failed: " + ex.Message);
                    }
                }
                else
                {
                    verifyErrors.Add("descriptor streaming metadata unavailable: " + metaErr);
                }
            }
            bool needExtractCd = true;
            try
            {
                if (System.IO.File.Exists(outDescriptor) && new FileInfo(outDescriptor).Length > 0)
                    needExtractCd = false;
            }
            catch { }
            if (needExtractCd && !extractor.ExtractCd(chdPath, outDescriptor, out string errCd))
            {
                report = "export failed: extractcd: " + errCd;
                return 3;
            }
            if (!System.IO.File.Exists(outDescriptor))
            {
                report = "export failed: extractcd produced no descriptor";
                return 3;
            }

            List<(int trackNo, string fileName, string trackType)> tracks = expectsGdi ? ParseGdiTrackFiles(outDescriptor) : ParseCueTrackFiles(outDescriptor);
            HashSet<string> extractedNames = new HashSet<string>(tracks.Select(t => t.fileName).Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.OrdinalIgnoreCase);
            foreach (string extra in extractedNames.ToArray())
            {
                string full = System.IO.Path.Combine(tempDir, extra);
                if (!System.IO.File.Exists(full))
                    extractedNames.Remove(extra);
            }

            FileScan scanner = new FileScan();
            Dictionary<string, (ulong size, byte[] crc, byte[] sha1, byte[] md5)> hashCache = new Dictionary<string, (ulong, byte[], byte[], byte[])>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in extractedNames)
            {
                string full = System.IO.Path.Combine(tempDir, name);
                FileInfo fi = new FileInfo(full);
                ScannedFile sf = new ScannedFile(FileType.File)
                {
                    Name = name,
                    FileModTimeStamp = fi.LastWriteTime.ToFileTimeUtc(),
                    GotStatus = GotStatus.Got,
                    DeepScanned = true,
                    Size = (ulong)fi.Length
                };
                using (Stream s = System.IO.File.OpenRead(full))
                {
                    scanner.CheckSumRead(s, sf, (ulong)fi.Length, true, false, null, 0, 0);
                }
                hashCache[name] = ((ulong)fi.Length, sf.CRC, sf.SHA1, sf.MD5);
            }

            List<RvFile> expectedData = expectedList.Where(e => IsTrackDataFile(e.Name)).ToList();
            expectedData.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

            Dictionary<int, string> extractedByTrack = new Dictionary<int, string>();
            foreach (var t in tracks)
            {
                if (!string.IsNullOrWhiteSpace(t.fileName) && extractedNames.Contains(t.fileName))
                    extractedByTrack[t.trackNo] = t.fileName;
            }

            Dictionary<int, string> expectedByTrack = BuildExpectedTrackNumberMap(expectedData.Select(e => e.Name).ToList());
            Dictionary<string, string> mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> usedExtracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in expectedByTrack)
            {
                if (extractedByTrack.TryGetValue(kvp.Key, out string extName))
                {
                    mapping[kvp.Value] = extName;
                    usedExtracted.Add(extName);
                }
            }

            Dictionary<ulong, List<string>> extractedBySize = new Dictionary<ulong, List<string>>();
            foreach (var kvp in hashCache)
            {
                if (usedExtracted.Contains(kvp.Key))
                    continue;
                if (!extractedBySize.TryGetValue(kvp.Value.size, out var list))
                {
                    list = new List<string>();
                    extractedBySize[kvp.Value.size] = list;
                }
                list.Add(kvp.Key);
            }

            foreach (RvFile exp in expectedData)
            {
                if (mapping.ContainsKey(exp.Name))
                    continue;
                ulong size = exp.Size ?? 0;
                if (size == 0)
                    continue;
                if (!extractedBySize.TryGetValue(size, out var cands))
                    continue;
                if (cands.Count != 1)
                    continue;
                mapping[exp.Name] = cands[0];
                usedExtracted.Add(cands[0]);
            }

            List<string> extractedRemaining = extractedNames.Where(n => !usedExtracted.Contains(n)).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            int fallbackIdx = 0;
            foreach (RvFile exp in expectedData)
            {
                if (mapping.ContainsKey(exp.Name))
                    continue;
                if (fallbackIdx >= extractedRemaining.Count)
                    break;
                mapping[exp.Name] = extractedRemaining[fallbackIdx++];
            }

            foreach (var kvp in mapping)
            {
                string expectedName = kvp.Key;
                string extractedName = kvp.Value;
                if (string.IsNullOrWhiteSpace(expectedName) || string.IsNullOrWhiteSpace(extractedName))
                    continue;
                string src = System.IO.Path.Combine(tempDir, extractedName);
                if (!System.IO.File.Exists(src))
                    continue;
                string dst = System.IO.Path.Combine(outputDir, expectedName);
                System.IO.File.Copy(src, dst, true);
                exported.Add(expectedName);

                RvFile exp = expectedList.FirstOrDefault(e => string.Equals(e.Name, expectedName, StringComparison.OrdinalIgnoreCase));
                if (exp != null)
                    VerifyHashMatch(expectedName, exp, hashCache, extractedName, verifyErrors);
            }

            string destDescriptorName = expectsGdi ? expectedGdiName : expectedCueName;
            if (!string.IsNullOrWhiteSpace(destDescriptorName))
            {
                string dst = System.IO.Path.Combine(outputDir, destDescriptorName);
                System.IO.File.Copy(outDescriptor, dst, true);
                exported.Add(destDescriptorName);
            }

            exported.Sort(StringComparer.OrdinalIgnoreCase);
            report = BuildExportReport(chdPath, outputDir, exported, verifyErrors);
            return verifyErrors.Count == 0 ? 0 : 5;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch
            {
            }
        }
    }

    private static string BuildExportReport(string chdPath, string outputDir, List<string> files, List<string> verifyErrors)
    {
        List<string> lines = new List<string>();
        lines.Add("CHD export");
        lines.Add(chdPath);
        lines.Add("output=" + outputDir);
        lines.Add("");
        lines.Add("exported:");
        foreach (string f in files)
            lines.Add(f);
        lines.Add("");
        lines.Add("verify:");
        if (verifyErrors.Count == 0)
        {
            lines.Add("OK");
        }
        else
        {
            for (int i = 0; i < verifyErrors.Count; i++)
                lines.Add(verifyErrors[i]);
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static void VerifyHashMatch(string expectedName, RvFile expected, Dictionary<string, (ulong size, byte[] crc, byte[] sha1, byte[] md5)> extractedHashes, string extractedName, List<string> errors)
    {
        if (!extractedHashes.TryGetValue(extractedName, out var h))
            return;
        if (expected.Size.HasValue && expected.Size.Value != 0 && h.size != expected.Size.Value)
            errors.Add($"{expectedName}: size mismatch");
        if (expected.CRC != null && h.crc != null && !expected.CRC.SequenceEqual(h.crc))
            errors.Add($"{expectedName}: crc mismatch");
        if (expected.SHA1 != null && h.sha1 != null && !expected.SHA1.SequenceEqual(h.sha1))
            errors.Add($"{expectedName}: sha1 mismatch");
        if (expected.MD5 != null && h.md5 != null && !expected.MD5.SequenceEqual(h.md5))
            errors.Add($"{expectedName}: md5 mismatch");
    }

    private static void VerifyFileAgainstExpected(string filePath, RvFile expected, List<string> errors)
    {
        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                errors.Add($"{expected.Name}: missing after export");
                return;
            }
            FileInfo fi = new FileInfo(filePath);
            FileScan scanner = new FileScan();
            ScannedFile sf = new ScannedFile(FileType.File)
            {
                Name = expected.Name,
                FileModTimeStamp = fi.LastWriteTime.ToFileTimeUtc(),
                GotStatus = GotStatus.Got,
                DeepScanned = true,
                Size = (ulong)fi.Length
            };
            using (Stream s = System.IO.File.OpenRead(filePath))
            {
                scanner.CheckSumRead(s, sf, (ulong)fi.Length, true, false, null, 0, 0);
            }

            if (expected.Size.HasValue && expected.Size.Value != 0 && (ulong)fi.Length != expected.Size.Value)
                errors.Add($"{expected.Name}: size mismatch");
            if (expected.CRC != null && sf.CRC != null && !expected.CRC.SequenceEqual(sf.CRC))
                errors.Add($"{expected.Name}: crc mismatch");
            if (expected.SHA1 != null && sf.SHA1 != null && !expected.SHA1.SequenceEqual(sf.SHA1))
                errors.Add($"{expected.Name}: sha1 mismatch");
            if (expected.MD5 != null && sf.MD5 != null && !expected.MD5.SequenceEqual(sf.MD5))
                errors.Add($"{expected.Name}: md5 mismatch");
        }
        catch (Exception ex)
        {
            errors.Add($"{expected.Name}: verify failed: {ex.Message}");
        }
    }

    private static bool IsTrackDataFile(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        string ext = System.IO.Path.GetExtension(name).ToLowerInvariant();
        return ext == ".bin" || ext == ".raw" || ext == ".iso";
    }

    private static Dictionary<int, string> BuildExpectedTrackNumberMap(List<string> expectedNames)
    {
        Dictionary<int, string> map = new Dictionary<int, string>();
        System.Text.RegularExpressions.Regex[] patterns = new[]
        {
            new System.Text.RegularExpressions.Regex(@"\(Track\s*(\d+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled),
            new System.Text.RegularExpressions.Regex(@"track[\s_]*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled),
            new System.Text.RegularExpressions.Regex(@"track(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled)
        };
        foreach (string name in expectedNames)
        {
            foreach (var r in patterns)
            {
                var m = r.Match(name);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int tno))
                {
                    if (!map.ContainsKey(tno))
                        map.Add(tno, name);
                    break;
                }
            }
        }
        return map;
    }

    private static List<(int trackNo, string fileName, string trackType)> ParseCueTrackFiles(string cuePath)
    {
        List<(int, string, string)> list = new List<(int, string, string)>();
        string[] lines;
        try
        {
            lines = System.IO.File.ReadAllLines(cuePath);
        }
        catch
        {
            return list;
        }

        string currentFile = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.Length == 0)
                continue;

            if (trimmed.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
            {
                int q1 = trimmed.IndexOf('"');
                if (q1 >= 0)
                {
                    int q2 = trimmed.IndexOf('"', q1 + 1);
                    if (q2 > q1)
                    {
                        currentFile = trimmed.Substring(q1 + 1, q2 - q1 - 1);
                        continue;
                    }
                }
                string[] p = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 2)
                    currentFile = p[1].Trim('"');
                continue;
            }

            if (trimmed.StartsWith("TRACK", StringComparison.OrdinalIgnoreCase))
            {
                string[] p = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 3 && int.TryParse(p[1], out int trackNo))
                {
                    string trackType = p[2].Trim();
                    if (!string.IsNullOrWhiteSpace(currentFile))
                        list.Add((trackNo, currentFile, trackType));
                }
            }
        }

        return list;
    }

    private static List<(int trackNo, string fileName, string trackType)> ParseGdiTrackFiles(string gdiPath)
    {
        List<(int, string, string)> list = new List<(int, string, string)>();
        string[] lines;
        try
        {
            lines = System.IO.File.ReadAllLines(gdiPath);
        }
        catch
        {
            return list;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0)
                continue;

            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
                continue;

            if (!int.TryParse(parts[0], out int trackNo))
                continue;

            string name = parts[4].Trim().Trim('"');
            string trackType = parts.Length >= 3 ? parts[2] : "";
            if (!string.IsNullOrWhiteSpace(name))
                list.Add((trackNo, name, trackType));
        }

        return list;
    }

    private static Dictionary<int, RvFile> BuildExpectedTrackMap(List<RvFile> expectedChildren)
    {
        Dictionary<int, RvFile> map = new Dictionary<int, RvFile>();
        Regex[] patterns = new[]
        {
            new Regex(@"\(Track\s*(\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"track[\s_]*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"track(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };
        for (int i = 0; i < expectedChildren.Count; i++)
        {
            RvFile f = expectedChildren[i];
            if (f?.Name == null)
                continue;
            int tno = -1;
            for (int p = 0; p < patterns.Length; p++)
            {
                Match m = patterns[p].Match(f.Name);
                if (m.Success && int.TryParse(m.Groups[1].Value, out tno))
                    break;
            }
            if (tno > 0 && !map.ContainsKey(tno))
                map.Add(tno, f);
        }
        return map;
    }

    private static void SkipBytes(Stream s, ulong bytes)
    {
        if (bytes == 0)
            return;
        byte[] buffer = new byte[64 * 1024];
        while (bytes > 0)
        {
            int toRead = (int)Math.Min((ulong)buffer.Length, bytes);
            int read = s.Read(buffer, 0, toRead);
            if (read <= 0)
                break;
            bytes -= (ulong)read;
        }
    }

    private static void CopyBytes(Stream input, Stream output, ulong bytes)
    {
        byte[] buffer = new byte[128 * 1024];
        while (bytes > 0)
        {
            int toRead = (int)Math.Min((ulong)buffer.Length, bytes);
            int read = input.Read(buffer, 0, toRead);
            if (read <= 0)
                break;
            output.Write(buffer, 0, read);
            bytes -= (ulong)read;
        }
    }

    private static long GetFreeSpaceBytes(string path)
    {
        try
        {
            string root = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(path));
            DriveInfo di = new DriveInfo(root);
            return di.AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    private static long? TryGetChdLogicalSizeBytes(IChdExtractor extractor, string chdPath)
    {
        if (!extractor.Info(chdPath, out string infoText))
            return null;
        try
        {
            var m = System.Text.RegularExpressions.Regex.Match(infoText ?? "", @"\((\d+)\s+bytes\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success && long.TryParse(m.Groups[1].Value, out long v))
                return v;
        }
        catch
        {
        }
        return null;
    }

    private static string FindChdmanExePath()
    {
        string baseDir = "";
        try
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(baseDir))
        {
            string candidate = System.IO.Path.Combine(baseDir, "chdman.exe");
            if (System.IO.File.Exists(candidate))
                return candidate;
        }

        string cwd = "";
        try
        {
            cwd = Environment.CurrentDirectory;
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(cwd))
        {
            string candidate = System.IO.Path.Combine(cwd, "chdman.exe");
            if (System.IO.File.Exists(candidate))
                return candidate;
        }

        return "chdman.exe";
    }
}
