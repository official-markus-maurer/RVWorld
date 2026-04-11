using CHDSharpLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CHDSharpLib;

public sealed class ChdCdTrackInfo
{
    public int TrackNo { get; set; }
    public string TrackType { get; set; }
    public long StartFrame { get; set; }
    public long Frames { get; set; }
    public long PreGapFrames { get; set; }
    public long PostGapFrames { get; set; }
    public int SectorSize { get; set; }
}

public static class ChdMetadata
{
    public static bool TryReadCdTrackLayout(string chdPath, out List<ChdCdTrackInfo> tracks, out string error)
    {
        tracks = new List<ChdCdTrackInfo>();
        error = "";
        if (string.IsNullOrWhiteSpace(chdPath) || !System.IO.File.Exists(chdPath))
        {
            error = "CHD not found.";
            return false;
        }

        try
        {
            using FileStream fs = System.IO.File.OpenRead(chdPath);
            if (!CHD.CheckHeader(fs, out _, out uint version))
            {
                error = "Invalid CHD header.";
                return false;
            }

            chd_error err;
            CHDHeader chd;
            switch (version)
            {
                case 1:
                    err = CHDHeaders.ReadHeaderV1(fs, out chd);
                    break;
                case 2:
                    err = CHDHeaders.ReadHeaderV2(fs, out chd);
                    break;
                case 3:
                    err = CHDHeaders.ReadHeaderV3(fs, out chd);
                    break;
                case 4:
                    err = CHDHeaders.ReadHeaderV4(fs, out chd);
                    break;
                case 5:
                    err = CHDHeaders.ReadHeaderV5(fs, out chd);
                    break;
                default:
                    error = "Unsupported CHD version: " + version;
                    return false;
            }

            if (err != chd_error.CHDERR_NONE)
            {
                error = "Header read failed: " + err;
                return false;
            }

            List<(uint tag, byte[] data)> metas = ReadMetadataEntries(fs, chd);
            if (metas.Count == 0)
            {
                error = "No metadata entries.";
                return false;
            }

            List<ChdCdTrackInfo> parsed = ParseCdTracks(metas);
            if (parsed.Count == 0)
            {
                error = "No CD track metadata found.";
                return false;
            }

            parsed.Sort((a, b) => a.TrackNo.CompareTo(b.TrackNo));
            long cursor = 0;
            for (int i = 0; i < parsed.Count; i++)
            {
                parsed[i].StartFrame = cursor + Math.Max(0, parsed[i].PreGapFrames);
                cursor += Math.Max(0, parsed[i].PreGapFrames) + Math.Max(0, parsed[i].Frames) + Math.Max(0, parsed[i].PostGapFrames);
            }

            tracks = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            tracks = new List<ChdCdTrackInfo>();
            return false;
        }
    }

    private static List<(uint tag, byte[] data)> ReadMetadataEntries(Stream file, CHDHeader chd)
    {
        using BinaryReader br = new BinaryReader(file, Encoding.UTF8, true);
        List<(uint tag, byte[] data)> list = new List<(uint tag, byte[] data)>();
        ulong metaoffset = chd.metaoffset;
        while (metaoffset != 0)
        {
            file.Seek((long)metaoffset, SeekOrigin.Begin);
            uint metaTag = br.ReadUInt32BE();
            uint metaLength = br.ReadUInt32BE();
            ulong metaNext = br.ReadUInt64BE();
            uint metaFlags = metaLength >> 24;
            metaLength &= 0x00ffffff;

            byte[] metaData = new byte[metaLength];
            file.Read(metaData, 0, metaData.Length);
            if ((metaFlags & CHDMetaData.CHD_MDFLAGS_CHECKSUM) != 0 || metaData.Length > 0)
                list.Add((metaTag, metaData));

            metaoffset = metaNext;
        }
        return list;
    }

    private static List<ChdCdTrackInfo> ParseCdTracks(List<(uint tag, byte[] data)> metas)
    {
        List<ChdCdTrackInfo> tracks = new List<ChdCdTrackInfo>();
        for (int i = 0; i < metas.Count; i++)
        {
            string tag = TagToString(metas[i].tag);
            if (!IsCdTrackTag(tag))
                continue;

            string text = Util.isAscii(metas[i].data) ? Encoding.ASCII.GetString(metas[i].data) : "";
            if (string.IsNullOrWhiteSpace(text))
                continue;

            ChdCdTrackInfo ti = TryParseCdTrackText(text);
            if (ti != null && ti.TrackNo > 0 && ti.Frames > 0)
                tracks.Add(ti);
        }
        return tracks;
    }

    private static bool IsCdTrackTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;
        return tag.StartsWith("CHT", StringComparison.OrdinalIgnoreCase) || tag.StartsWith("CHG", StringComparison.OrdinalIgnoreCase) || tag.StartsWith("CDR", StringComparison.OrdinalIgnoreCase);
    }

    private static string TagToString(uint tag)
    {
        char a = (char)((tag >> 24) & 0xFF);
        char b = (char)((tag >> 16) & 0xFF);
        char c = (char)((tag >> 8) & 0xFF);
        char d = (char)((tag >> 0) & 0xFF);
        return new string(new[] { a, b, c, d });
    }

    private static ChdCdTrackInfo TryParseCdTrackText(string text)
    {
        try
        {
            int trackNo = TryGetInt(text, "TRACK");
            long frames = TryGetLong(text, "FRAMES");
            long pregap = TryGetLong(text, "PREGAP");
            long postgap = TryGetLong(text, "POSTGAP");
            string type = TryGetString(text, "TYPE");
            if (string.IsNullOrWhiteSpace(type))
            {
                string mode = TryGetString(text, "MODE");
                if (!string.IsNullOrWhiteSpace(mode))
                    type = mode;
            }

            int sector = ResolveSectorSize(type);
            if (sector <= 0)
                sector = 2352;

            return new ChdCdTrackInfo
            {
                TrackNo = trackNo,
                TrackType = type ?? "",
                Frames = frames,
                PreGapFrames = pregap,
                PostGapFrames = postgap,
                SectorSize = sector
            };
        }
        catch
        {
            return null;
        }
    }

    private static int ResolveSectorSize(string trackType)
    {
        if (string.IsNullOrWhiteSpace(trackType))
            return 2352;
        string t = trackType.Trim().ToUpperInvariant();
        if (t.Contains("2048"))
            return 2048;
        if (t.Contains("AUDIO"))
            return 2352;
        if (t.Contains("2352"))
            return 2352;
        if (t.Contains("MODE1") || t.Contains("MODE2"))
            return 2352;
        return 2352;
    }

    private static int TryGetInt(string text, string key)
    {
        string s = TryGetString(text, key);
        return int.TryParse(s, out int v) ? v : 0;
    }

    private static long TryGetLong(string text, string key)
    {
        string s = TryGetString(text, key);
        return long.TryParse(s, out long v) ? v : 0;
    }

    private static string TryGetString(string text, string key)
    {
        Match m = Regex.Match(text, key + @":\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value.Trim();
        m = Regex.Match(text, key + @":\s*([^\s]+)", RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value.Trim();
        return "";
    }
}
