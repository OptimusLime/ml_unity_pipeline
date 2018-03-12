using System;
using System.Linq;
using System.IO;
using System.Collections;
using Google.Protobuf;

namespace Jobs
{
    public class ByteOperations
    {
        public class ByteReader
        {
            byte[] byteBuffer;
            int currentRead = 0;
            public ByteReader(byte[] array)
            {
                this.byteBuffer = array;
                this.currentRead = 0;
            }

            public Int32 ReadInt()
            {
                //slice 4 bytes out
                var iBytes = this.ReadBytes(sizeof(Int32));

                //reverse if platform is little endian
                //because this is packed with >L big endian order
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(iBytes);

                //
                Int32 result = (int)BitConverter.ToUInt32(iBytes, 0);
                return result;
            }

            public byte[] ReadBytes(int length)
            {
                // slice our the bytes 
                // TODO: pull from a byte pool if we want :)
                byte[] byteArray = new byte[length];

                // copy existing bytes into our temp array 
                Buffer.BlockCopy(this.byteBuffer, currentRead, byteArray, 0, length);

                // increment our read 
                this.currentRead += length;

                // send back the bytes we allocated
                return byteArray;
            }

            public int Length
            {
                get { return this.byteBuffer.Length; }
            }
        }

        public class ByteWriter
        {
            byte[] byteBuffer;
            int currentWrite = 0;

            public ByteWriter(int byteLength) : this(new byte[byteLength]) { }
            public ByteWriter(byte[] array)
            {
                this.byteBuffer = array;
                this.currentWrite = 0;
            }

            public void WriteInt(int value)
            {
                // convert our object into byte array
                byte[] iBytes = BitConverter.GetBytes(value);

                // all ints are treated as big endian
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(iBytes);

                // copy in big endian int bytes
                Buffer.BlockCopy(iBytes, 0, byteBuffer, currentWrite, sizeof(int));

                // move our write location
                currentWrite += sizeof(int);
            }
            public void WriteBytes(byte[] array)
            {
                //write our stream into the array
                Buffer.BlockCopy(array, 0, byteBuffer, currentWrite, array.Length);

                // move our write location
                currentWrite += array.Length;
            }
            public byte[] GetBytes()
            {
                return this.byteBuffer;
            }
            public int Length
            {
                get { return this.byteBuffer.Length; }
            }
        }

    }
}