using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CHDSharpLib;
using Compress;
using FileScanner;
using RomVaultCore.ReadDat;
using RomVaultCore.RvDB;
using RomVaultCore.Scanner;
using RomVaultCore.Utils;
using File = RVIO.File;
using FileInfo = RVIO.FileInfo;
using Path = RVIO.Path;

namespace RomVaultCore.FixFile.Utils
{
    public static partial class FixFileUtils
    {
        public static bool TryCreateChdFromDiscSource(RvFile sourceFile, RvFile destinationFile, out ReturnCode returnCode, out string errorMessage)
        {
            returnCode = ReturnCode.Good;
            errorMessage = "";

            if (sourceFile == null || destinationFile == null)
                return false;
            if (!destinationFile.IsFile && destinationFile.FileType != FileType.CHD)
                return false;
            if (!destinationFile.Name.EndsWith(".chd", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!sourceFile.IsFile)
                return false;

            RomVaultCore.DatRule rule = DatReader.FindDatRule(destinationFile.Parent?.DatTreeFullName + "\\");
            if (rule == null || !rule.DiscArchiveAsCHD)
                return false;

            if (destinationFile.IsFile)
            {
                if (!DBHelper.IsChdCreationAllowedForSet(destinationFile, out string incompleteReason))
                {
                    returnCode = ReturnCode.LogicError;
                    errorMessage = incompleteReason;
                    return true;
                }
            }

            string sourceExt = Path.GetExtension(sourceFile.NameCase);
            if (!IsDiscSourceExtension(sourceExt))
                return false;

            string sourcePath = null;
            if (sourceFile.FileType == FileType.File)
            {
                sourcePath = ResolveExistingFilePath(sourceFile.FullNameCase);
                if (!File.Exists(sourcePath))
                {
                    string tfc = sourceFile.TreeFullNameCase ?? "";
                    if (tfc.StartsWith("ToSort", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            string remainder = tfc.Length > 7 && tfc[6] == System.IO.Path.DirectorySeparatorChar
                                ? tfc.Substring(7)
                                : tfc.Length > 6 && tfc[6] == '/' ? tfc.Substring(7) : tfc;
                            string root = DB.GetToSortPrimary()?.FullNameCase;
                            if (!string.IsNullOrWhiteSpace(root))
                            {
                                string attempt = System.IO.Path.Combine(root, remainder);
                                attempt = ResolveExistingFilePath(attempt);
                                if (File.Exists(attempt))
                                {
                                    sourcePath = attempt;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    if (!File.Exists(sourcePath))
                    {
                        returnCode = ReturnCode.FileSystemError;
                        errorMessage = "Disc image source file not found on disk.";
                        return true;
                    }
                }
            }
            else if (sourceFile.FileType != FileType.FileZip && sourceFile.FileType != FileType.FileSevenZip)
            {
                returnCode = ReturnCode.LogicError;
                errorMessage = "Disc image source is not a supported file type.";
                return true;
            }

            string destinationPath = ResolveOutputFilePath(destinationFile.FullName);
            string destinationDir = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir) && !System.IO.Directory.Exists(destinationDir))
                System.IO.Directory.CreateDirectory(destinationDir);

            string inputPath;
            string workingDir;
            List<string> tempPathsToDelete;
            returnCode = MaterializeDiscInput(sourceFile, destinationFile, sourcePath, out inputPath, out workingDir, out tempPathsToDelete, out errorMessage);
            if (returnCode != ReturnCode.Good)
            {
                CleanupFailedChd(destinationPath);
                CleanupTempPaths(tempPathsToDelete);
                return true;
            }

            string inputExt = Path.GetExtension(inputPath);

            string command = GetChdmanCommand(inputExt);
            if (command == null)
            {
                returnCode = ReturnCode.LogicError;
                errorMessage = "Disc image type not supported for CHD creation.";
                CleanupTempPaths(tempPathsToDelete);
                return true;
            }

            if (File.Exists(destinationPath))
            {
                try
                {
                    File.SetAttributes(destinationPath, RVIO.FileAttributes.Normal);
                }
                catch
                {
                }
                try
                {
                    File.Delete(destinationPath);
                }
                catch
                {
                }
            }

            string chdmanExe = FindChdmanExePath();

            string args = BuildChdmanArguments(command, inputPath, destinationPath, destinationFile, rule.ChdCompressionType);

            returnCode = RunChdman(chdmanExe, args, workingDir, out string output);
            if (returnCode != ReturnCode.Good)
            {
                CleanupFailedChd(destinationPath);
                CleanupTempPaths(tempPathsToDelete);
                errorMessage = output;
                return true;
            }

            if (!File.Exists(destinationPath))
            {
                returnCode = ReturnCode.FileSystemError;
                errorMessage = "CHD creation finished but output file was not created.";
                CleanupTempPaths(tempPathsToDelete);
                return true;
            }

            returnCode = VerifyAndMergeCreatedChd(destinationPath, destinationFile, out errorMessage);
            if (returnCode != ReturnCode.Good)
            {
                CleanupFailedChd(destinationPath);
                CleanupTempPaths(tempPathsToDelete);
                return true;
            }

            CleanupSourceFiles(inputPath);
            CleanupTempPaths(tempPathsToDelete);
            return true;
        }

        public static bool TryCreateChdFromAudioTracks(List<(int trackNo, RvFile expected, RvFile source)> tracks, RvFile destinationFile, out ReturnCode returnCode, out string errorMessage)
        {
            returnCode = ReturnCode.Good;
            errorMessage = "";

            if (tracks == null || tracks.Count == 0 || destinationFile == null)
                return false;
            if (destinationFile.FileType != FileType.CHD && !destinationFile.IsFile)
                return false;
            if (destinationFile.Name == null || !destinationFile.Name.EndsWith(".chd", StringComparison.OrdinalIgnoreCase))
                return false;

            RomVaultCore.DatRule rule = DatReader.FindDatRule(destinationFile.Parent?.DatTreeFullName + "\\");
            if (rule == null || !rule.DiscArchiveAsCHD)
                return false;

            string destinationPath = ResolveOutputFilePath(destinationFile.FullName);
            string destinationDir = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir) && !System.IO.Directory.Exists(destinationDir))
                System.IO.Directory.CreateDirectory(destinationDir);

            string baseTempDir = DB.GetToSortCache()?.FullName ?? Environment.CurrentDirectory;
            baseTempDir = ResolveExistingDirectoryPath(baseTempDir);
            if (string.IsNullOrWhiteSpace(baseTempDir))
                baseTempDir = Environment.CurrentDirectory;
            string tempDir = System.IO.Path.Combine(baseTempDir, "__RomVault.chdtracks." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            List<string> tempPathsToDelete = new List<string> { tempDir };

            List<string> copiedNames = new List<string>();
            try
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    string outName = tracks[i].expected?.NameCase ?? tracks[i].source?.NameCase ?? ("track" + (i + 1).ToString("D2") + ".bin");
                    outName = System.IO.Path.GetFileName(outName.Replace('\\', '/'));
                    string outPath = System.IO.Path.Combine(tempDir, outName);
                    ReturnCode rc = MaterializeSingleFile(tracks[i].source, outPath, out string err);
                    if (rc != ReturnCode.Good)
                    {
                        returnCode = rc;
                        errorMessage = err;
                        CleanupFailedChd(destinationPath);
                        CleanupTempPaths(tempPathsToDelete);
                        return true;
                    }
                    copiedNames.Add(outName);
                }

                string cuePath = System.IO.Path.Combine(tempDir, "disc.cue");
                string cueText = BuildAudioCue(copiedNames);
                System.IO.File.WriteAllText(cuePath, cueText, Encoding.ASCII);

                if (File.Exists(destinationPath))
                {
                    try { File.SetAttributes(destinationPath, RVIO.FileAttributes.Normal); } catch { }
                    try { File.Delete(destinationPath); } catch { }
                }

                string chdmanExe = FindChdmanExePath();
                string args = BuildChdmanArguments("createcd", cuePath, destinationPath, destinationFile, rule.ChdCompressionType);
                returnCode = RunChdman(chdmanExe, args, tempDir, out string output);
                if (returnCode != ReturnCode.Good)
                {
                    CleanupFailedChd(destinationPath);
                    CleanupTempPaths(tempPathsToDelete);
                    errorMessage = output;
                    return true;
                }

                if (!File.Exists(destinationPath))
                {
                    returnCode = ReturnCode.FileSystemError;
                    errorMessage = "CHD creation finished but output file was not created.";
                    CleanupTempPaths(tempPathsToDelete);
                    return true;
                }

                returnCode = VerifyAndMergeCreatedChd(destinationPath, destinationFile, out errorMessage);
                if (returnCode != ReturnCode.Good)
                {
                    CleanupFailedChd(destinationPath);
                    CleanupTempPaths(tempPathsToDelete);
                    return true;
                }

                CleanupTempPaths(tempPathsToDelete);
                return true;
            }
            catch (Exception ex)
            {
                CleanupFailedChd(destinationPath);
                CleanupTempPaths(tempPathsToDelete);
                returnCode = ReturnCode.FileSystemError;
                errorMessage = ex.Message;
                return true;
            }
        }

        private static string BuildAudioCue(List<string> trackFileNames)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < trackFileNames.Count; i++)
            {
                string f = trackFileNames[i] ?? "";
                sb.Append("FILE \"").Append(f.Replace("\"", "")).AppendLine("\" BINARY");
                sb.Append("  TRACK ").Append((i + 1).ToString("D2")).AppendLine(" AUDIO");
                sb.AppendLine("    INDEX 01 00:00:00");
            }
            return sb.ToString();
        }

        private static ReturnCode MaterializeSingleFile(RvFile sourceFile, string outputPath, out string errorMessage)
        {
            errorMessage = "";
            if (sourceFile == null || !sourceFile.IsFile)
            {
                errorMessage = "Source track file is not valid.";
                return ReturnCode.LogicError;
            }

            if (sourceFile.FileType == FileType.File)
            {
                try
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath));
                    System.IO.File.Copy(sourceFile.FullNameCase, outputPath, true);
                    return ReturnCode.Good;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return ReturnCode.FileSystemError;
                }
            }

            if (sourceFile.FileType != FileType.FileZip && sourceFile.FileType != FileType.FileSevenZip)
            {
                errorMessage = "Source track file is not a supported file type.";
                return ReturnCode.LogicError;
            }

            if (sourceFile.Parent == null || (sourceFile.Parent.FileType != FileType.Zip && sourceFile.Parent.FileType != FileType.SevenZip))
            {
                errorMessage = "Archive source is missing its parent archive.";
                return ReturnCode.LogicError;
            }

            return ExtractArchiveEntryToPath(sourceFile.Parent, sourceFile.ZipFileIndex, outputPath, out errorMessage);
        }

        private static bool IsDiscSourceExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
                return false;
            switch (ext.ToLowerInvariant())
            {
                case ".cue":
                case ".gdi":
                case ".iso":
                    return true;
                default:
                    return false;
            }
        }

        private static string ResolveDiscInputPath(string sourcePath, string destinationName, RvFile destinationFile)
        {
            string dir = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return sourcePath;

            string baseName = Path.GetFileNameWithoutExtension(destinationName);
            bool preferGdi = IsGdiPreferredPlatform(destinationFile);

            string gdi = Path.Combine(dir, baseName + ".gdi");
            if (preferGdi && File.Exists(gdi))
                return gdi;

            string cue = Path.Combine(dir, baseName + ".cue");
            if (File.Exists(cue))
                return cue;

            if (File.Exists(gdi))
                return gdi;

            string iso = Path.Combine(dir, baseName + ".iso");
            if (File.Exists(iso))
                return iso;

            if (preferGdi)
            {
                string[] anyGdi = Directory.GetFiles(dir, "*.gdi", SearchOption.TopDirectoryOnly);
                if (anyGdi.Length > 0)
                    return anyGdi[0];
            }

            return sourcePath;
        }

        private static bool IsGdiPreferredPlatform(RvFile destinationFile)
        {
            string hint = GetDatHintText(destinationFile);
            if (string.IsNullOrWhiteSpace(hint))
                return false;

            return hint.IndexOf("Arcade - Namco - Sega - Nintendo - Triforce", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hint.IndexOf("Arcade - Sega - Chihiro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hint.IndexOf("Arcade - Sega - Naomi 2", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hint.IndexOf("Arcade - Sega - Naomi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hint.IndexOf("Sega - Dreamcast", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPspPlatform(RvFile destinationFile)
        {
            string hint = GetDatHintText(destinationFile);
            if (string.IsNullOrWhiteSpace(hint))
                return false;

            return hint.IndexOf("Sony - PlayStation Portable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hint.IndexOf("PlayStation Portable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hint.IndexOf("PSP", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetChdmanCommand(string inputExt)
        {
            if (string.IsNullOrWhiteSpace(inputExt))
                return null;

            switch (inputExt.ToLowerInvariant())
            {
                case ".cue":
                case ".gdi":
                    return "createcd";
                case ".iso":
                    return "createdvd";
                default:
                    return null;
            }
        }

        private static string BuildChdmanArguments(string command, string inputPath, string outputPath, RvFile destinationFile, RomVaultCore.ChdCompressionType chdCompressionType)
        {
            string compression = BuildChdmanCompressionArgument(command, chdCompressionType);

            if (string.Equals(command, "createdvd", StringComparison.OrdinalIgnoreCase) &&
                (chdCompressionType == RomVaultCore.ChdCompressionType.PSP || IsPspPlatform(destinationFile)) &&
                outputPath.EndsWith(".chd", StringComparison.OrdinalIgnoreCase))
            {
                return $"{command} -i \"{inputPath}\" -o \"{outputPath}\" {compression} -hs 2048 -f";
            }

            if (string.Equals(command, "createdvd", StringComparison.OrdinalIgnoreCase))
            {
                int hs = GetDvdHunkSizeBytes();
                if (hs > 0)
                    return $"{command} -i \"{inputPath}\" -o \"{outputPath}\" {compression} -hs {hs} -f";
            }

            return $"{command} -i \"{inputPath}\" -o \"{outputPath}\" {compression} -f";
        }

        private static int GetDvdHunkSizeBytes()
        {
            int kib = Settings.rvSettings.ChdDvdHunkSizeKiB;
            if (kib <= 0)
                return 0;
            if (kib < 4)
                kib = 4;
            if (kib > 1024)
                kib = 1024;
            int bytes = kib * 1024;
            int sector = 2048;
            bytes = bytes / sector * sector;
            if (bytes < 4096)
                bytes = 4096;
            return bytes;
        }

        private static string BuildChdmanCompressionArgument(string command, RomVaultCore.ChdCompressionType chdCompressionType)
        {
            if (chdCompressionType == RomVaultCore.ChdCompressionType.Auto)
            {
                if (string.Equals(command, "createcd", StringComparison.OrdinalIgnoreCase))
                    return "-c cdzs,cdzl,cdfl";
                if (string.Equals(command, "createdvd", StringComparison.OrdinalIgnoreCase))
                    return "-c zstd,zlib,huff,flac";
                return "-c zstd";
            }
            if (chdCompressionType == RomVaultCore.ChdCompressionType.CD)
            {
                if (string.Equals(command, "createcd", StringComparison.OrdinalIgnoreCase))
                    return "-c cdzs,cdzl,cdfl";
                return "-c zstd";
            }

            if (chdCompressionType == RomVaultCore.ChdCompressionType.DVD)
            {
                if (string.Equals(command, "createdvd", StringComparison.OrdinalIgnoreCase))
                    return "-c zstd,zlib,huff,flac";
                return "-c cdzs,cdzl,cdfl";
            }

            if (chdCompressionType == RomVaultCore.ChdCompressionType.PSP)
            {
                if (string.Equals(command, "createdvd", StringComparison.OrdinalIgnoreCase))
                    return "-c zstd,zlib,huff,flac";
                return "-c cdzs,cdzl,cdfl";
            }

            return "-c zstd";
        }

        private static ReturnCode RunChdman(string chdmanExe, string arguments, string workingDirectory, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                if (string.IsNullOrWhiteSpace(chdmanExe) || (!System.IO.Path.IsPathRooted(chdmanExe) && !System.IO.File.Exists(chdmanExe)))
                {
                    errorMessage = "chdman.exe not found. Place chdman.exe next to ROMVault, in a 'tools' subfolder, or add it to PATH.";
                    return ReturnCode.FileSystemError;
                }
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = chdmanExe,
                    Arguments = arguments,
                    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process p = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    System.Text.StringBuilder stdout = new System.Text.StringBuilder();
                    System.Text.StringBuilder stderr = new System.Text.StringBuilder();
                    int lastPercent = -1;

                    p.OutputDataReceived += (_, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data))
                            return;
                        stdout.AppendLine(e.Data);
                    };
                    p.ErrorDataReceived += (_, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data))
                            return;
                        stderr.AppendLine(e.Data);
                        int pct = TryParsePercent(e.Data);
                        if (pct >= 0 && pct <= 100 && pct != lastPercent)
                        {
                            lastPercent = pct;
                            try { Report.ReportProgress(new bgwText($"CHD {pct}%")); } catch { }
                        }
                    };

                    p.Start();
                    ChdmanProcessTracker.Register(p);
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    while (true)
                    {
                        if (p.WaitForExit(250))
                            break;

                        if (Report.CancellationPending())
                        {
                            ChdmanProcessTracker.Kill(p);
                            errorMessage = "Cancelled.";
                            return ReturnCode.Cancel;
                        }
                    }
                    p.WaitForExit();

                    if (p.ExitCode != 0)
                    {
                        errorMessage = $"{stdout}{Environment.NewLine}{stderr}".Trim();
                        if (string.IsNullOrWhiteSpace(errorMessage))
                            errorMessage = $"chdman exited with code {p.ExitCode}.";
                        return ReturnCode.FileSystemError;
                    }

                    errorMessage = $"{stdout}{Environment.NewLine}{stderr}".Trim();
                    return ReturnCode.Good;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return ReturnCode.FileSystemError;
            }
        }

        private static int TryParsePercent(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return -1;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != '%')
                    continue;
                int j = i - 1;
                while (j >= 0 && line[j] >= '0' && line[j] <= '9')
                    j--;
                int start = j + 1;
                int len = i - start;
                if (len <= 0 || len > 3)
                    continue;
                if (int.TryParse(line.Substring(start, len), out int pct))
                    return pct;
            }
            return -1;
        }

        private static ReturnCode VerifyAndMergeCreatedChd(string destinationPath, RvFile destinationFile, out string errorMessage)
        {
            errorMessage = "";

            FileInfo fi = new FileInfo(destinationPath);
            long ts = fi.LastWriteTime;

            uint? chdVersion;
            byte[] chdSha1;
            byte[] chdMd5;
            ReturnCode rc = ReadChdInternalHashes(destinationPath, true, out chdVersion, out chdSha1, out chdMd5, out errorMessage);
            if (rc != ReturnCode.Good)
                return rc;

            if (chdVersion != 5)
            {
                errorMessage = $"CHD is not V5 (found V{chdVersion ?? 0}).";
                return ReturnCode.DestinationCheckSumMismatch;
            }

            if (destinationFile.SHA1 != null && (chdSha1 == null || !ArrByte.BCompare(destinationFile.SHA1, chdSha1)))
            {
                errorMessage = "CHD internal SHA1 does not match DAT.";
                return ReturnCode.DestinationCheckSumMismatch;
            }
            if (destinationFile.MD5 != null && (chdMd5 == null || !ArrByte.BCompare(destinationFile.MD5, chdMd5)))
            {
                errorMessage = "CHD internal MD5 does not match DAT.";
                return ReturnCode.DestinationCheckSumMismatch;
            }

            ScannedFile sf = new ScannedFile(FileType.File)
            {
                Name = destinationPath,
                FileModTimeStamp = ts,
                GotStatus = GotStatus.Got,
                DeepScanned = false,
                Size = (ulong)fi.Length
            };
            sf.FileStatusSet(FileStatus.SizeVerified);
            sf.CHDVersion = chdVersion;
            sf.AltSHA1 = chdSha1;
            sf.AltMD5 = chdMd5;
            if (chdSha1 != null)
                sf.FileStatusSet(FileStatus.AltSHA1FromHeader | FileStatus.AltSHA1Verified);
            if (chdMd5 != null)
                sf.FileStatusSet(FileStatus.AltMD5FromHeader | FileStatus.AltMD5Verified);

            destinationFile.FileMergeIn(sf, false);
            destinationFile.CHDVersion = chdVersion;

            try
            {
                if (Settings.rvSettings.ChdTrustContainerForTracks)
                {
                    ScannedFile trust = new ScannedFile(FileType.CHD)
                    {
                        Name = destinationPath,
                        ZipStruct = ZipStructure.None,
                        Comment = ""
                    };
                    for (int i = 0; i < destinationFile.ChildCount; i++)
                    {
                        RvFile exp = destinationFile.Child(i);
                        if (exp == null || !exp.IsFile)
                            continue;
                        trust.Add(new ScannedFile(FileType.FileCHD)
                        {
                            Name = exp.Name,
                            FileModTimeStamp = ts,
                            GotStatus = GotStatus.Got,
                            DeepScanned = true
                        });
                    }
                    trust.Sort();
                    destinationFile.MergeInArchive(trust);
                }
                else
                {
                    ScannedFile chdContents = Populate.FromAZipFileArchive(destinationFile, EScanLevel.Level3, null);
                    if (chdContents != null)
                        destinationFile.MergeInArchive(chdContents);
                }
            }
            catch
            {
            }

            return ReturnCode.Good;
        }

        private static string GetDatHintText(RvFile destinationFile)
        {
            if (destinationFile == null)
                return "";

            string datName = destinationFile.Dat?.GetData(RvDat.DatData.DatName) ?? "";
            string datDescription = destinationFile.Dat?.GetData(RvDat.DatData.Description) ?? "";
            string datCategory = destinationFile.Dat?.GetData(RvDat.DatData.Category) ?? "";
            string datRootDir = destinationFile.Dat?.GetData(RvDat.DatData.RootDir) ?? "";

            string gameCategory = destinationFile.Parent?.Game?.GetData(RvGame.GameData.Category) ?? "";
            string gameSourceFile = destinationFile.Parent?.Game?.GetData(RvGame.GameData.Sourcefile) ?? "";

            return $"{datName} | {datDescription} | {datCategory} | {datRootDir} | {gameCategory} | {gameSourceFile}";
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
                string tools = System.IO.Path.Combine(baseDir, "tools", "chdman.exe");
                if (System.IO.File.Exists(tools))
                    return tools;
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

            try
            {
                string? path = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    string[] dirs = path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string d in dirs)
                    {
                        string candidate = System.IO.Path.Combine(d.Trim(), "chdman.exe");
                        if (System.IO.File.Exists(candidate))
                            return candidate;
                    }
                }
            }
            catch
            {
            }

            return "";
        }

        private static ReturnCode MaterializeDiscInput(RvFile sourceFile, RvFile destinationFile, string sourcePath, out string inputPath, out string workingDir, out List<string> tempPathsToDelete, out string errorMessage)
        {
            inputPath = null;
            workingDir = null;
            tempPathsToDelete = new List<string>();
            errorMessage = "";

            if (sourceFile.FileType == FileType.File)
            {
                inputPath = ResolveExistingFilePath(ResolveDiscInputPath(sourcePath, destinationFile.Name, destinationFile));
                workingDir = System.IO.Path.GetDirectoryName(inputPath);
                if (!ValidateDiscInputCompleteness(inputPath, workingDir, out errorMessage))
                    return ReturnCode.FileSystemError;
                return ReturnCode.Good;
            }

            if (sourceFile.FileType != FileType.FileZip && sourceFile.FileType != FileType.FileSevenZip)
            {
                errorMessage = "Disc image source is not a supported archive member.";
                return ReturnCode.LogicError;
            }

            if (sourceFile.Parent == null || (sourceFile.Parent.FileType != FileType.Zip && sourceFile.Parent.FileType != FileType.SevenZip))
            {
                errorMessage = "Archive source is missing its parent archive.";
                return ReturnCode.LogicError;
            }

            string baseTempDir = ResolveExistingDirectoryPath(DB.GetToSortCache()?.FullName);
            if (string.IsNullOrWhiteSpace(baseTempDir))
                baseTempDir = Environment.CurrentDirectory;
            string tempDir = System.IO.Path.Combine(baseTempDir, "__RomVault.chdman." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            tempPathsToDelete.Add(tempDir);

            ReturnCode rc = ExtractDiscSetFromArchive(sourceFile.Parent, destinationFile.Name, destinationFile, tempDir, out inputPath, out errorMessage);
            if (rc != ReturnCode.Good)
                return rc;

            workingDir = tempDir;
            return ReturnCode.Good;
        }

        private static bool ValidateDiscInputCompleteness(string inputPath, string workingDir, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(inputPath))
                return true;

            string ext = System.IO.Path.GetExtension(inputPath).ToLowerInvariant();
            if (ext != ".cue" && ext != ".gdi")
                return true;

            if (string.IsNullOrWhiteSpace(workingDir))
                workingDir = System.IO.Path.GetDirectoryName(inputPath);

            if (string.IsNullOrWhiteSpace(workingDir))
                return true;

            IEnumerable<string> refs = ext == ".cue"
                ? GetReferencedFilesFromCue(inputPath)
                : GetReferencedFilesFromGdi(inputPath);

            foreach (string r in refs)
            {
                if (string.IsNullOrWhiteSpace(r))
                    continue;

                string trimmed = r.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                string candidate = null;
                if (System.IO.Path.IsPathRooted(trimmed))
                {
                    candidate = trimmed;
                }
                else
                {
                    candidate = NormalizeChildPath(workingDir, trimmed);
                    if (candidate == null)
                    {
                        string baseName = System.IO.Path.GetFileName(trimmed);
                        if (!string.IsNullOrWhiteSpace(baseName))
                            candidate = System.IO.Path.Combine(workingDir, baseName);
                    }
                }

                if (string.IsNullOrWhiteSpace(candidate))
                {
                    errorMessage = "__SKIP_PARTIAL_SET__";
                    return false;
                }

                try
                {
                    if (!System.IO.File.Exists(candidate))
                    {
                        errorMessage = "__SKIP_PARTIAL_SET__";
                        return false;
                    }
                }
                catch
                {
                    errorMessage = "__SKIP_PARTIAL_SET__";
                    return false;
                }
            }

            return true;
        }

        private static ReturnCode ExtractDiscSetFromArchive(RvFile archiveFile, string destinationName, RvFile destinationFile, string tempDir, out string inputPath, out string errorMessage)
        {
            inputPath = null;
            errorMessage = "";

            Dictionary<string, int> entryIndex = BuildArchiveEntryIndex(archiveFile, out errorMessage);
            if (entryIndex == null)
                return ReturnCode.FileSystemError;

            string baseName = Path.GetFileNameWithoutExtension(destinationName);
            string preferExt = IsGdiPreferredPlatform(destinationFile) ? ".gdi" : ".cue";

            string[] candidates = new[]
            {
                baseName + preferExt,
                baseName + (preferExt == ".gdi" ? ".cue" : ".gdi"),
                baseName + ".iso"
            };

            string chosenEntry = null;
            int chosenIndex = -1;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (TryFindArchiveEntryIndex(entryIndex, candidates[i], out int idx))
                {
                    chosenEntry = candidates[i];
                    chosenIndex = idx;
                    break;
                }
            }

            if (chosenEntry == null)
            {
                errorMessage = "Could not find cue/gdi/iso inside archive matching the expected CHD base name.";
                return ReturnCode.FileSystemError;
            }

            string extractedMain = System.IO.Path.Combine(tempDir, chosenEntry);
            ReturnCode rc = ExtractArchiveEntryToPath(archiveFile, chosenIndex, extractedMain, out errorMessage);
            if (rc != ReturnCode.Good)
                return rc;

            string ext = System.IO.Path.GetExtension(extractedMain).ToLowerInvariant();
            if (ext == ".iso")
            {
                inputPath = extractedMain;
                return ReturnCode.Good;
            }

            List<string> referenced = new List<string>();
            if (ext == ".cue")
                referenced.AddRange(GetReferencedFilesFromCue(extractedMain));
            else if (ext == ".gdi")
                referenced.AddRange(GetReferencedFilesFromGdi(extractedMain));

            for (int i = 0; i < referenced.Count; i++)
            {
                string refName = referenced[i];
                if (string.IsNullOrWhiteSpace(refName))
                    continue;

                int refIndex;
                if (!TryFindArchiveEntryIndex(entryIndex, refName, out refIndex))
                {
                    errorMessage = $"Referenced file not found in archive: {refName}";
                    return ReturnCode.FileSystemError;
                }

                string outPath = System.IO.Path.Combine(tempDir, refName);
                rc = ExtractArchiveEntryToPath(archiveFile, refIndex, outPath, out errorMessage);
                if (rc != ReturnCode.Good)
                    return rc;
            }

            inputPath = extractedMain;
            return ReturnCode.Good;
        }

        private static Dictionary<string, int> BuildArchiveEntryIndex(RvFile archiveFile, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                ICompress z = archiveFile.FileType == FileType.Zip ? (ICompress)new Compress.StructuredZip.StructuredZip() : new Compress.SevenZip.SevenZ();
                ZipReturn zr = z.ZipFileOpen(archiveFile.FullNameCase, archiveFile.FileModTimeStamp, true);
                if (zr != ZipReturn.ZipGood)
                {
                    errorMessage = $"Error opening archive: {zr}";
                    return null;
                }

                Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < z.LocalFilesCount; i++)
                {
                    FileHeader fh = z.GetFileHeader(i);
                    if (fh == null || fh.IsDirectory)
                        continue;
                    string name = (fh.Filename ?? "").Replace('\\', '/');
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    if (!map.ContainsKey(name))
                        map.Add(name, i);
                }
                z.ZipFileClose();
                return map;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }

        private static bool TryFindArchiveEntryIndex(Dictionary<string, int> entryIndex, string requestedName, out int index)
        {
            index = -1;
            if (entryIndex == null || string.IsNullOrWhiteSpace(requestedName))
                return false;

            string reqNorm = requestedName.Replace('\\', '/').Trim().Trim('"');
            if (entryIndex.TryGetValue(reqNorm, out index))
                return true;

            string reqBase = System.IO.Path.GetFileName(reqNorm);
            if (string.IsNullOrWhiteSpace(reqBase))
                return false;

            int found = -1;
            foreach (KeyValuePair<string, int> kvp in entryIndex)
            {
                string baseName = System.IO.Path.GetFileName(kvp.Key);
                if (!string.Equals(baseName, reqBase, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (found != -1)
                    return false;
                found = kvp.Value;
            }

            if (found == -1)
                return false;

            index = found;
            return true;
        }

        private static ReturnCode ExtractArchiveEntryToPath(RvFile archiveFile, int fileIndex, string outputPath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                ICompress z = archiveFile.FileType == FileType.Zip ? (ICompress)new Compress.StructuredZip.StructuredZip() : new Compress.SevenZip.SevenZ();
                ZipReturn zr = z.ZipFileOpen(archiveFile.FullNameCase, archiveFile.FileModTimeStamp, true);
                if (zr != ZipReturn.ZipGood)
                {
                    errorMessage = $"Error opening archive: {zr}";
                    return ReturnCode.FileSystemError;
                }

                zr = z.ZipFileOpenReadStream(fileIndex, out Stream readStream, out ulong streamSize);
                if (zr != ZipReturn.ZipGood || readStream == null)
                {
                    z.ZipFileClose();
                    errorMessage = $"Error opening archive stream: {zr}";
                    return ReturnCode.FileSystemError;
                }

                string outDir = System.IO.Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outDir))
                    Directory.CreateDirectory(outDir);

                int openRet = RVIO.FileStream.OpenFileWrite(outputPath, RVIO.FileStream.BufSizeMax, out Stream writeStream);
                if (openRet != 0 || writeStream == null)
                {
                    z.ZipFileCloseReadStream();
                    z.ZipFileClose();
                    errorMessage = "Error creating output file for extraction.";
                    return ReturnCode.FileSystemError;
                }

                byte[] buffer = new byte[1024 * 1024];
                ulong remaining = streamSize;
                while (remaining > 0)
                {
                    int toRead = remaining > (ulong)buffer.Length ? buffer.Length : (int)remaining;
                    int read = readStream.Read(buffer, 0, toRead);
                    if (read <= 0)
                        break;
                    writeStream.Write(buffer, 0, read);
                    remaining -= (ulong)read;
                }

                writeStream.Flush();
                writeStream.Close();
                writeStream.Dispose();

                z.ZipFileCloseReadStream();
                z.ZipFileClose();
                return ReturnCode.Good;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return ReturnCode.FileSystemError;
            }
        }

        private static string ResolveExistingFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            try
            {
                if (System.IO.Path.IsPathRooted(path))
                    return path;
            }
            catch
            {
            }

            try
            {
                if (System.IO.File.Exists(path))
                    return System.IO.Path.GetFullPath(path);
            }
            catch
            {
            }

            try
            {
                string baseDir = "";
                try { baseDir = AppDomain.CurrentDomain.BaseDirectory; } catch { }
                DirectoryInfo di = string.IsNullOrWhiteSpace(baseDir) ? null : new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && di != null; i++)
                {
                    string attempt = System.IO.Path.Combine(di.FullName, path);
                    if (System.IO.File.Exists(attempt))
                        return attempt;
                    di = di.Parent;
                }
            }
            catch
            {
            }

            return path;
        }

        private static string ResolveOutputFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;
            try
            {
                if (System.IO.Path.IsPathRooted(path))
                    return path;
            }
            catch
            {
            }

            try
            {
                string baseDir = "";
                try { baseDir = AppDomain.CurrentDomain.BaseDirectory; } catch { }
                DirectoryInfo di = string.IsNullOrWhiteSpace(baseDir) ? null : new DirectoryInfo(baseDir);
                string firstSegment = "";
                try
                {
                    int sep = path.IndexOfAny(new[] { '\\', '/' });
                    firstSegment = sep >= 0 ? path.Substring(0, sep) : path;
                }
                catch
                {
                }

                for (int i = 0; i < 10 && di != null; i++)
                {
                    if (!string.IsNullOrWhiteSpace(firstSegment))
                    {
                        string candidateRoot = System.IO.Path.Combine(di.FullName, firstSegment);
                        if (System.IO.Directory.Exists(candidateRoot))
                            return System.IO.Path.Combine(di.FullName, path);
                    }

                    string attempt = System.IO.Path.Combine(di.FullName, path);
                    string attemptDir = System.IO.Path.GetDirectoryName(attempt);
                    if (!string.IsNullOrWhiteSpace(attemptDir) && System.IO.Directory.Exists(attemptDir))
                        return attempt;

                    di = di.Parent;
                }
            }
            catch
            {
            }

            try
            {
                return System.IO.Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static string ResolveExistingDirectoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            try
            {
                if (System.IO.Path.IsPathRooted(path))
                {
                    if (System.IO.Directory.Exists(path))
                        return path;
                }
            }
            catch
            {
            }

            try
            {
                if (System.IO.Directory.Exists(path))
                    return System.IO.Path.GetFullPath(path);
            }
            catch
            {
            }

            try
            {
                string baseDir = "";
                try { baseDir = AppDomain.CurrentDomain.BaseDirectory; } catch { }
                System.IO.DirectoryInfo di = string.IsNullOrWhiteSpace(baseDir) ? null : new System.IO.DirectoryInfo(baseDir);
                for (int i = 0; i < 10 && di != null; i++)
                {
                    string attempt = System.IO.Path.Combine(di.FullName, path);
                    if (System.IO.Directory.Exists(attempt))
                        return attempt;
                    di = di.Parent;
                }
            }
            catch
            {
            }

            try
            {
                return System.IO.Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static void CleanupTempPaths(List<string> tempPathsToDelete)
        {
            if (tempPathsToDelete == null || tempPathsToDelete.Count == 0)
                return;

            for (int i = tempPathsToDelete.Count - 1; i >= 0; i--)
            {
                string p = tempPathsToDelete[i];
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                try
                {
                    if (Directory.Exists(p))
                    {
                        Directory.Delete(p, true);
                        continue;
                    }
                }
                catch
                {
                }

                TryDeleteFile(p);
            }
        }

        private static void CleanupFailedChd(string destinationPath)
        {
            TryDeleteFile(destinationPath);
        }

        private static void CleanupSourceFiles(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return;
            if (!System.IO.File.Exists(inputPath))
                return;

            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            switch (ext)
            {
                case ".iso":
                    TryDeleteFile(inputPath);
                    break;
                case ".cue":
                    CleanupCue(inputPath);
                    break;
                case ".gdi":
                    CleanupGdi(inputPath);
                    break;
            }
        }

        private static void CleanupCue(string cuePath)
        {
            string dir = Path.GetDirectoryName(cuePath);
            if (string.IsNullOrWhiteSpace(dir))
                return;

            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            files.Add(cuePath);
            foreach (string refFile in GetReferencedFilesFromCue(cuePath))
            {
                string fullPath = NormalizeChildPath(dir, refFile);
                if (fullPath != null)
                    files.Add(fullPath);
            }

            foreach (string file in files)
                TryDeleteFile(file);
        }

        private static void CleanupGdi(string gdiPath)
        {
            string dir = Path.GetDirectoryName(gdiPath);
            if (string.IsNullOrWhiteSpace(dir))
                return;

            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            files.Add(gdiPath);
            foreach (string refFile in GetReferencedFilesFromGdi(gdiPath))
            {
                string fullPath = NormalizeChildPath(dir, refFile);
                if (fullPath != null)
                    files.Add(fullPath);
            }

            foreach (string file in files)
                TryDeleteFile(file);
        }

        private static IEnumerable<string> GetReferencedFilesFromCue(string cuePath)
        {
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(cuePath);
            }
            catch
            {
                yield break;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmed = line.Trim();
                if (!trimmed.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                    continue;

                int firstQuote = trimmed.IndexOf('"');
                if (firstQuote >= 0)
                {
                    int secondQuote = trimmed.IndexOf('"', firstQuote + 1);
                    if (secondQuote > firstQuote)
                    {
                        string name = trimmed.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            yield return name;
                        continue;
                    }
                }

                string[] parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    yield return parts[1].Trim();
            }
        }

        private static IEnumerable<string> GetReferencedFilesFromGdi(string gdiPath)
        {
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(gdiPath);
            }
            catch
            {
                yield break;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int firstQuote = line.IndexOf('"');
                if (firstQuote >= 0)
                {
                    int secondQuote = line.IndexOf('"', firstQuote + 1);
                    if (secondQuote > firstQuote)
                    {
                        string name = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            yield return name;
                        continue;
                    }
                }

                // Fallback for unquoted filenames that might contain spaces
                string[] parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    int startIndex = 4;
                    int endIndex = parts.Length - 1; // The last token is usually the offset (e.g., '0')
                    if (endIndex > startIndex)
                    {
                        string name = string.Join(" ", parts, startIndex, endIndex - startIndex).Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            yield return name;
                    }
                    else
                    {
                        string name = parts[4].Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            yield return name;
                    }
                }
            }
        }

        private static string NormalizeChildPath(string baseDir, string refPath)
        {
            if (string.IsNullOrWhiteSpace(baseDir) || string.IsNullOrWhiteSpace(refPath))
                return null;

            string combined;
            try
            {
                combined = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, refPath));
            }
            catch
            {
                return null;
            }

            string baseFull;
            try
            {
                baseFull = System.IO.Path.GetFullPath(baseDir);
            }
            catch
            {
                return null;
            }

            if (!baseFull.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) &&
                !baseFull.EndsWith(System.IO.Path.AltDirectorySeparatorChar.ToString()))
            {
                baseFull += System.IO.Path.DirectorySeparatorChar;
            }

            if (!combined.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                return null;

            return combined;
        }

        private static void TryDeleteFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return;

            try
            {
                if (!File.Exists(filename))
                    return;
            }
            catch
            {
                return;
            }

            try
            {
                File.SetAttributes(filename, RVIO.FileAttributes.Normal);
            }
            catch
            {
            }

            try
            {
                File.Delete(filename);
            }
            catch
            {
            }
        }

        private static ReturnCode ReadChdInternalHashes(string filename, bool deepCheck, out uint? chdVersion, out byte[] chdSha1, out byte[] chdMd5, out string errorMessage)
        {
            chdVersion = null;
            chdSha1 = null;
            chdMd5 = null;
            errorMessage = "";

            if (!File.Exists(filename))
            {
                errorMessage = "CHD file not found for verification.";
                return ReturnCode.FileSystemError;
            }

            Stream s = null;
            int retval = RVIO.FileStream.OpenFileRead(filename, RVIO.FileStream.BufSizeMax, out s);
            if (retval != 0 || s == null)
            {
                errorMessage = "CHD could not be opened for verification.";
                return ReturnCode.FileSystemError;
            }

            try
            {
                chd_error result = CHD.CheckFile(s, filename, deepCheck, out chdVersion, out chdSha1, out chdMd5);
                if (result != chd_error.CHDERR_NONE && result != chd_error.CHDERR_REQUIRES_PARENT)
                {
                    errorMessage = $"CHD verification error: {result}";
                    return ReturnCode.DestinationCheckSumMismatch;
                }

                return ReturnCode.Good;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return ReturnCode.FileSystemError;
            }
            finally
            {
                try
                {
                    s.Close();
                    s.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
