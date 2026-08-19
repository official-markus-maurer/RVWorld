namespace Compress
{
    public enum ZipReturn
    {
        ZipGood,
        ZipFileLocked,
        ZipFileCountError,
        ZipSignatureError,
        ZipExtraDataOnEndOfZip,
        ZipUnsupportedCompression,
        ZipLocalFileHeaderError,
        ZipCentralDirError,
        ZipEndOfCentralDirectoryError,
        Zip64EndOfCentralDirError,
        Zip64EndOfCentralDirectoryLocatorError,
        ZipReadingFromOutputFile,
        ZipWritingToInputFile,
        ZipErrorGettingDataStream,
        ZipCRCDecodeError,
        ZipDecodeError,
        ZipFileNameToLong,
        ZipFileAlreadyOpen,
        ZipCannotFastOpen,
        ZipErrorOpeningFile,
        ZipErrorFileNotFound,
        ZipErrorReadingFile,
        ZipErrorTimeStamp,
        ZipErrorRollBackFile,
        ZipTryingToAccessADirectory,
        ZipErrorWritingToOutputStream,
        ZipTrrntzipIncorrectCompressionUsed,
        ZipTrrntzipIncorrectFileOrder,
        ZipTrrntzipIncorrectDirectoryAddedToZip,
        ZipTrrntZipIncorrectDataStream,
        ZipTrrntZipIncorrectDirectorySeparator,
        ZipUntested

    }

    public enum ZipOpenType
    {
        Closed,
        OpenRead,
        OpenWrite,
        OpenFakeWrite
    }

    public enum ZipCompression
    {
        Stored = 0,
        Shrunk = 1,
        Reduced1 = 2,
        Reduced2 = 3,
        Reduced3 = 4,
        Reduced4 = 5,
        Imploded = 6,
        TokenizingCompressionAlgorithm = 7,
        Deflated = 8,
        Deflate64 = 9,
        Imploding = 10,
        Reserved1 = 11,
        Bzip2 = 12,
        Reserved2 = 13,
        LZMA = 14,
        Reserved3 = 15,
        IBMCMPSC = 16,
        Reserved4 = 17,
        IBMTERSE = 18,
        IBMLZ77z = 19,
        ZStandardDeprecated = 20,
        ZStandard = 93,
        MP3 = 94,
        XZ = 95,
        JPEG = 96,
        WavPack = 97,
        PPMd = 98,
        AEx = 99
    }
}
