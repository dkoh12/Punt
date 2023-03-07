using System;
using System.Collections.Generic;

namespace punt
{
    public class Chunk
    {
        // https://github.com/stevehalliwell/ulox-work/blob/core_clox/Assets/ulox-working/Runtime/Chunk.cs

        public List<Value> constants; // array of values. "constant pool"
        public List<byte> instructions; // array of bytes
        public List<int> lines; // array of line numbers
        
        public Chunk()
        {
            constants = new List<Value>();
            instructions = new List<byte>();
            lines = new List<int>();
        }

        public void writeByte(byte Byte, int line)
        {
            instructions.Add(Byte);
            lines.Add(line);
        }

        public byte writeConstant(Value constant)
        {
            constants.Add(constant);
            return (byte) (constants.Count - 1);
        }

    }
}
