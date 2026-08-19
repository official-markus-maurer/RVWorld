using Compress;
using Compress.StructuredZip;
using Compress.ZipFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using RVUtils;

namespace FileScanner;

internal static class FileScanParallel
{
    private static pZipReader tZipReader = null;

    internal static ZipReturn ParalleZipScanner(string filename, long timeStamp, bool deepScan, out ScannedFile scannedArchive, bool useDosDateTime, bool scanSHA256)
    {
        StructuredZip zip = new StructuredZip();
        ZipReturn zr = zip.ZipFileOpen(filename, timeStamp, true);
        if (zr != ZipReturn.ZipGood)
        {
            scannedArchive = null;
            return zr;
        }

        if (tZipReader == null)
            tZipReader = new pZipReader(8192, 65536);

        tZipReader.StartLoader(zip);

        int totalFiles = zip.LocalFilesCount;


        int localFileCounter = -1;

        List<Thread> tasks = new List<Thread>();
        ScannedFile[] scannedFiles = new ScannedFile[totalFiles];

        int procC = Environment.ProcessorCount - 1;
        procC = procC - 2;
        if (procC < 2) procC = 2;

        for (int i = 0; i < procC; i++)
        {
            Thread t = new Thread(() =>
            {
                FileScan fs = new FileScan();
                while (true)
                {
                    int localFileIndex = Interlocked.Increment(ref localFileCounter);
                    if (localFileIndex >= totalFiles)
                        return;

                    ZipFileData localFile = (ZipFileData)zip.GetLocalFileData(localFileIndex);
                    ScannedFile scannedFile = new ScannedFile(FileType.FileZip)
                    {
                        Name = localFile.Filename,
                        DeepScanned = deepScan,
                        Index = localFileIndex,
                        LocalHeaderOffset = localFile.LocalHead,
                        FileModTimeStamp = useDosDateTime ? localFile.HeaderLastModified : localFile.LastModified,
                    };
                    scannedFiles[localFileIndex] = scannedFile;

                    if (localFile.IsDirectory)
                    {
                        scannedFile.GotStatus = GotStatus.Got;
                        scannedFile.Size = 0;
                        scannedFile.CRC = ByteUtils.ZeroByteCRC;
                        scannedFile.SHA1 = ByteUtils.ZeroByteSHA1;
                        scannedFile.SHA256 = ByteUtils.ZeroByteSHA256;
                        scannedFile.MD5 = ByteUtils.ZeroByteMD5;

                        scannedFile.StatusFlags |= FileStatus.CRCFromHeader | FileStatus.SizeVerified | FileStatus.CRCVerified | FileStatus.SHA1Verified | FileStatus.MD5Verified | FileStatus.SHA256Verified;

                        continue;
                    }

                    using (ParallelReaderStream parallelReaderStream = tZipReader.GetReadStream(localFileIndex))
                    {
                        ZipReturn zr = ZipFileData.OpenStream(parallelReaderStream, localFile.CompressionMethod, localFile.CompressedSize, localFile.UncompressedSize, 0, out ulong thisStreamSize, out Stream fStream);
                        scannedFile.Size = thisStreamSize;
                        scannedFile.StatusFlags |= FileStatus.SizeFromHeader;

                        int res = fs.CheckSumRead(fStream, scannedFile, localFile.UncompressedSize, deepScan, scanSHA256, null, 0, 0);
                        if (res != 0)
                        {
                            scannedFile.GotStatus = GotStatus.Corrupt;
                            // corrupt zip should still returns its CRC, otherwise the corrupt file will not be push out to ToSort on a fix.
                            if (scannedFile.CRC == null)
                            {
                                scannedFile.CRC = localFile.CRC;
                                scannedFile.StatusFlags |= FileStatus.CRCFromHeader;
                            }
                        }
                        else
                        {
                            // if we are not testcrc'ing or deepScan'ing then we did not verify the data stream
                            // so we assume it is good.
                            if (!deepScan)
                            {
                                scannedFile.CRC = localFile.CRC;
                                scannedFile.StatusFlags |= FileStatus.CRCFromHeader;
                            }

                            if (localFile.CRC == null)
                            {
                                scannedFile.StatusFlags |= FileStatus.SizeVerified;
                                scannedFile.GotStatus = GotStatus.Got;
                            }
                            else if (ByteUtils.ByteArrEquals(localFile.CRC, scannedFile.CRC))
                            {
                                scannedFile.StatusFlags |= FileStatus.SizeVerified;
                                scannedFile.StatusFlags |= FileStatus.CRCFromHeader;
                                scannedFile.GotStatus = GotStatus.Got;
                            }
                            else
                            {
                                scannedFile.GotStatus = GotStatus.Corrupt;
                            }
                        }

                    }
                }
            });
            tasks.Add(t);
            t.Start();
        }


        for (int i = 0; i < procC; i++)
            tasks[i].Join();

        tZipReader.JoinReader();


        scannedArchive = new ScannedFile(FileType.Zip)
        {
            Name = filename,
            ZipStruct = zip.ZipStruct,
            Comment = zip.FileComment,
            FileModTimeStamp = timeStamp
        };
        for (int i = 0; i < totalFiles; i++)
            scannedArchive.Add(scannedFiles[i]);

        zip.ZipFileClose();

        return ZipReturn.ZipGood;
    }
}
