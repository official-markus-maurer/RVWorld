using Compress.Support.Utils;
using System.IO;
using System.Text;

namespace Compress.SevenZip.Structure
{
    internal class SignatureHeader
    {
        private static readonly byte[] Signature = [(byte)'7', (byte)'z', 0xBC, 0xAF, 0x27, 0x1C];

        public ulong NextHeaderOffset;
        public ulong NextHeaderSize;
        public uint NextHeaderCRC;

        public long BaseOffset { get; private set; }


        public ulong NextHeaderLocation
        {
            get { return (ulong)BaseOffset + NextHeaderOffset; } 
            set { NextHeaderOffset = value - (ulong)BaseOffset; }
        }



        /* First Signature is:
         * 6 bytes : '7','z',0xbc,0xaf,0x270x1c
         * 2 bytes : Major version, Minor version
         * 4 bytes : CRC of this header (next 20 bytes)
         * 8 bytes : Offset of the next header  (NextHeaderOffset)
         * 8 bytes : Size of the next header    (NextHeaderSize)
         * 4 bytes : CRC of the next header     (NextHeaderCRC)
         *
         * which should always make BaseOffset = 32
         */



        public bool Read(Stream stream)
        {
            using BinaryReader br = new(stream, Encoding.UTF8, true);
            byte[] signatureBytes = br.ReadBytes(6);
            if (!signatureBytes.Compare(Signature))
            {
                return false;
            }

            br.ReadByte(); // major version
            br.ReadByte(); // minor version

            uint startHeaderCRC = br.ReadUInt32();

            long pos = br.BaseStream.Position;
            byte[] mainHeader = new byte[8 + 8 + 4];
            br.BaseStream.Read(mainHeader, 0, mainHeader.Length);
            if (!CRC.VerifyDigest(startHeaderCRC, mainHeader, 0, (uint)mainHeader.Length))
            {
                return false;
            }

            br.BaseStream.Seek(pos, SeekOrigin.Begin);

            NextHeaderOffset = br.ReadUInt64();
            NextHeaderSize = br.ReadUInt64();
            NextHeaderCRC = br.ReadUInt32();

            BaseOffset = br.BaseStream.Position;
            return true;
        }

        public void Write(Stream stream)
        {
            using BinaryWriter bw = new(stream, Encoding.UTF8, true);
            //SignatureHeader
            //~~~~~~~~~~~~~~~

            bw.BaseStream.Position = 0;
            bw.Write(Signature);

            //ArchiveVersion
            bw.Write((byte)0); //  BYTE Major
            bw.Write((byte)3); //  BYTE Minor

            byte[] sigHeaderBytes;
            using (MemoryStream sigHeaderMem = new())
            {
                using BinaryWriter sigHeaderBw = new(sigHeaderMem, Encoding.UTF8, true);
                sigHeaderBw.Write(NextHeaderOffset); //NextHeaderOffset
                sigHeaderBw.Write(NextHeaderSize); //NextHeaderSize
                sigHeaderBw.Write(NextHeaderCRC); //NextHeaderCRC
                sigHeaderBytes = sigHeaderMem.ToArray();
            }

            uint sigHeaderCRC = CRC.CalculateDigest(sigHeaderBytes, 0, (uint)sigHeaderBytes.Length);



            bw.Write(sigHeaderCRC); //HeaderCRC

            //StartHeader
            bw.Write(sigHeaderBytes);

            BaseOffset = bw.BaseStream.Position;
        }
    }
}