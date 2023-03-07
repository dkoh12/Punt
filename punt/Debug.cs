namespace punt
{
    public class Debug
    {
        // https://github.com/stevehalliwell/ulox-work/blob/core_clox/Assets/ulox-working/Runtime/Disasembler.cs

        public Debug()
        {

        }

        public void disassembleChunk(Chunk chunk, string name)
        {
            Console.WriteLine(String.Format("== {0} ==", name));
            for (int offset = 0; offset < chunk.instructions.Count; )
            {
                offset = disassembleInstruction(chunk, offset);                
            }

            Console.WriteLine();
        }

        public int disassembleInstruction(Chunk chunk, int offset)
        {
            Console.Write(String.Format("{0,4}", offset));
            if (offset > 0 && chunk.lines[offset] == chunk.lines[offset - 1])
            {
                Console.Write("     | ");
            } else
            {
                Console.Write(String.Format("   {0} ", chunk.lines[offset]));
            }

            var opCode = (OpCode)chunk.instructions[offset];

            switch (opCode)
            {
                case OpCode.CONSTANT:
                    return constantInstruction("OP_CONSTANT", chunk, offset);
                case OpCode.NIL:
                    return simpleInstruction("OP_NIL", offset);
                case OpCode.TRUE:
                    return simpleInstruction("OP_TRUE", offset);
                case OpCode.FALSE:
                    return simpleInstruction("OP_FALSE", offset);
                case OpCode.POP:
                    return simpleInstruction("OP_POP", offset);
                case OpCode.GET_LOCAL:
                    return byteInstruction("OP_GET_LOCAL", chunk, offset);
                case OpCode.SET_LOCAL:
                    return byteInstruction("OP_SET_LOCAL", chunk, offset);
                case OpCode.GET_GLOBAL:
                    return constantInstruction("OP_GET_GLOBAL", chunk, offset);
                case OpCode.DEFINE_GLOBAL:
                    return constantInstruction("OP_DEFINE_GLOBAL", chunk, offset);
                case OpCode.SET_GLOBAL:
                    return constantInstruction("OP_SET_GLOBAL", chunk, offset);
                case OpCode.EQUAL:
                    return simpleInstruction("OP_EQUAL", offset);
                case OpCode.GREATER:
                    return simpleInstruction("OP_GREATER", offset);
                case OpCode.LESS:
                    return simpleInstruction("OP_LESS", offset);
                case OpCode.ADD:
                    return simpleInstruction("OP_ADD", offset);
                case OpCode.SUBTRACT:
                    return simpleInstruction("OP_SUBTRACT", offset);
                case OpCode.MULTIPLY:
                    return simpleInstruction("OP_MULTIPLY", offset);
                case OpCode.DIVIDE:
                    return simpleInstruction("OP_DIVIDE", offset);
                case OpCode.NOT:
                    return simpleInstruction("OP_NOT", offset);
                case OpCode.NEGATE:
                    return simpleInstruction("OP_NEGATE", offset);
                case OpCode.PRINT:
                    return simpleInstruction("OP_PRINT", offset);
                case OpCode.JUMP:
                    return jumpInstruction("OP_JUMP", 1, chunk, offset);
                case OpCode.JUMP_IF_FALSE:
                    return jumpInstruction("OP_JUMP_IF_FALSE", 1, chunk, offset);
                case OpCode.LOOP:
                    return jumpInstruction("OP_LOOP", -1, chunk, offset);
                case OpCode.RETURN:
                    return simpleInstruction("OP_RETURN", offset);
                default:
                    Console.WriteLine(String.Format("Unknown opcode {0}", opCode));
                    return offset + 1;
            }
        }

        public int constantInstruction(string name, Chunk chunk, int offset)
        {
            var opCode = chunk.instructions[offset + 1];

            Value constant = chunk.constants[opCode]; // uses enum values. Each enum value is assigned a number with later enums having a higher value.
            Console.Write(String.Format("{0} {1} ", name, opCode)); // "%-16s %4d '"
            Console.WriteLine(constant);
            return offset + 2; //one for opcode and one for operand
        }

        public int simpleInstruction(string name, int offset)
        {
            Console.WriteLine(name);
            return offset + 1;
        }

        public int byteInstruction(string name, Chunk chunk, int offset)
        {
            byte slot = chunk.instructions[offset + 1];
            Console.WriteLine(string.Format("{0} {1}", name, slot));
            return offset + 2;
        }

        public int jumpInstruction(string name, int sign, Chunk chunk, int offset)
        {
            ushort jump = (ushort)(chunk.instructions[offset + 1] << 8);
            jump |= chunk.instructions[offset + 2];
            Console.WriteLine(String.Format("{0} {1} -> {2}", name, offset, offset + 3 + sign * jump));
            return offset + 3;
        }

    }
}
