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

        public void PushHash(Dictionary<object, object> value)
        {
            this.PushInt(value.Count);
            foreach(KeyValuePair<object, object> kv in value)
            {
                this.PushObject(kv.Key);
                this.PushObject(kv.Value);
            }
        }

        public void PushIntArray(int[] value)
        {
            this.PushInt(value.Length);
            foreach(int item in value)
            {
                this.PushInt(item);
            }
        }

        public void PushObject(object value)
        {
            Type type = value.GetType();
            if(type == typeof(byte))
            {
                this.PushByte((byte)'b');
                this.PushByte((byte)value);
            }
            else if(type == typeof(short))
            {
                this.PushByte((byte)'t');
                this.PushShort((short)value);
            }
            else if(type == typeof(int))
            {
                this.PushByte((byte)'i');
                this.PushInt((int)value);
            }
            else if (type == typeof(long))
            {
                this.PushByte((byte)'l');
                this.PushLong((long)value);
            }
            else if (type == typeof(string))
            {
                this.PushByte((byte)'s');
                this.PushString(value.ToString());
            }
            else if (type == typeof(Dictionary<object, object>))
            {
                this.PushByte((byte)'H');
                this.PushHash(value as Dictionary<object, object>);
            }
            else if (type == typeof(int[]))
            {
                this.PushByte((byte)'I');
                this.PushIntArray(value as int[]);
            }
        }

        public byte[] Bytes()
        {
            byte[] buf = new byte[this.offset];
            Buffer.BlockCopy(this.buffer, 0, buf, 0, offset);
            return buf;
        }
    }
}
