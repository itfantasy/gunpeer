using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace itfantasy.nodepeer.gnbuffers
{
    public class GnBuffer
    {
        byte[] buffer;
        int offset;

        public GnBuffer(int capacity)
        {
            this.buffer = new byte[capacity];
            this.offset = 0;
        }

        public void PushByte(byte value)
        {
            this.buffer[this.offset++] = value;
        }

        public void PushShort(short value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Buffer.BlockCopy(buffer, 0, this.buffer, this.offset, buffer.Length);
            this.offset += buffer.Length;
        }

        public void PushInt(int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Buffer.BlockCopy(buffer, 0, this.buffer, this.offset, buffer.Length);
            this.offset += buffer.Length;
        }

        public void PushLong(long value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Buffer.BlockCopy(buffer, 0, this.buffer, this.offset, buffer.Length);
            this.offset += buffer.Length;
        }

        public void PushString(string value)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value);
            this.PushInt(buffer.Length);
            Buffer.BlockCopy(buffer, 0, this.buffer, this.offset, buffer.Length);
            this.offset += buffer.Length;
        }

        public byte[] Bytes()
        {
            byte[] buf = new byte[this.offset];
            Buffer.BlockCopy(this.buffer, 0, buf, 0, offset);
            return buf;
        }
    }
}
