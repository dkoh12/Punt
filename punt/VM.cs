using System.Collections;
using System.Collections.Generic;

namespace punt
{
    // not sure if this is needed.
    public class Function
    {
        int arity;
        Chunk chunk;
        string name;

        public Function(int arity, Chunk chunk, string name)
        {
            this.arity = 0;
            this.chunk = chunk;
            this.name = null;
        }
    }

    /* Instead of storing return address in the callee's frame, the caller stores its 
     * own ip. When we return from a function, VM will jump to ip of caller's CallFrame
     * and resume from there.
     * 
     * in C
     * ObjFunction* function
     * uint8_t* ip
     * Value* slots
     */
    public class CallFrame
    {
        public int ip; // instruction pointer
        public int stackStart;
        // public ClosureInternal closure;
    }

    public class VM
    {
        Chunk chunk; // this could be a list
        Stack<Value> stack;  // should this also go inside a CallFrame?
        Dictionary<string, Value> globals;

        /* Function Call Frames have stack semantics. 
         * In each function if we define local variables, the relative locations of each
         * local variables are fixed. At compile time we calculate the relative slots.
         * At runtme we convert that relative slot to absolute stack index by adding the 
         * function call's starting slot.
         */
        List<CallFrame> callFrames = new List<CallFrame>(); // what makes this callframe grow?

        // what's the difference between arrow and = for C# properties?
        int currentCallFrame => callFrames.Count - 1;

        public int currentIP
        {
            get { return callFrames[currentCallFrame].ip; }
            set { callFrames[currentCallFrame].ip = value; }
        }


        public VM(Chunk chunk, Stack<Value> stack)
        {
            this.chunk = chunk;
            this.stack = stack;
            globals = new Dictionary<string, Value>();
        }

        // main function to run
        public InterpretResult interpret(Chunk chunk)
        {
            this.chunk = chunk;
            currentIP = 0; // this may change in the future. but for now ip = vm.chunk->code


            // ObjFunction* function = compile(source) -> compiler.compile()
            // ObjClosure* closure = newClosure(function)
            // run()


            return run();
        }
        
        public byte read_byte()
        {
            return this.chunk.instructions[currentIP++];
        }

        public Value read_constant()
        {

            return this.chunk.constants[read_byte()];
        }

        public string read_string()
        {
            return Value.asString(read_constant());
        }

        public ushort read_short()
        {
            var x = chunk.instructions[currentIP];
            currentIP++;
            var y = chunk.instructions[currentIP];
            currentIP++;
            return (ushort)((x << 8) | y);
        }

        public void binary_op(OpCode opcode)
        {
            do
            {
                if (opcode != OpCode.ADD && (!Value.isNumber(this.stack.ElementAt(0)) || !Value.isNumber(this.stack.ElementAt(1))))
                {
                    runtimeError("Operands must be numbers.");
                    throw new Exception(InterpretResult.RUNTIME_ERROR.ToString());
                }

                // could be an error for OpCode.Add
                Value b = this.stack.Pop();
                Value a = this.stack.Pop();

                switch (opcode)
                {
                    case OpCode.ADD:
                        {
                            if (Value.isString(a) && Value.isString(b))
                            {
                                string val = Value.asString(a) + Value.asString(b);
                                this.stack.Push(Value.stringVal(val));
                            }
                            else if (Value.isNumber(a) && Value.isNumber(b))
                            {
                                double val = Value.asNumber(a) + Value.asNumber(b);
                                this.stack.Push(Value.numberVal(val));
                            }
                            else
                            {
                                runtimeError("Operands must be two numbers or two strings.");
                                throw new Exception(InterpretResult.RUNTIME_ERROR.ToString());
                            }
                            break;
                        }
                    case OpCode.SUBTRACT:
                        {
                            double val = Value.asNumber(a) - Value.asNumber(b);
                            this.stack.Push(Value.numberVal(val));
                            break;
                        }
                    case OpCode.MULTIPLY:
                        {
                            double val = Value.asNumber(a) * Value.asNumber(b);
                            this.stack.Push(Value.numberVal(val));
                            break;
                        }
                    case OpCode.DIVIDE:
                        {
                            double val = Value.asNumber(a) / Value.asNumber(b);
                            this.stack.Push(Value.numberVal(val));
                            break;
                        }
                    default:
                        break;
                }
            } while (false);
        }

        public void doComparison(OpCode opcode)
        {
            do
            {
                if (!Value.isNumber(this.stack.ElementAt(0)) || !Value.isNumber(this.stack.ElementAt(1)))
                {
                    runtimeError("Operands must be numbers.");
                    throw new Exception(InterpretResult.RUNTIME_ERROR.ToString());
                }

                Value b = this.stack.Pop();
                Value a = this.stack.Pop();

                switch (opcode)
                {
                    case OpCode.GREATER:
                        {
                            bool val = a.val.Number > b.val.Number;
                            this.stack.Push(Value.boolVal(val));
                            break;
                        }
                    case OpCode.LESS:
                        {
                            bool val = a.val.Number < b.val.Number;
                            this.stack.Push(Value.boolVal(val));
                            break;
                        }

                    default:
                        break;
                }
            } while (false);
        }

        public InterpretResult run()
        {
            while (true)
            {
                Debug debug = new Debug();
                for (int i = 0; i < this.stack.Count; i++)
                {
                    Console.Write("[");
                    Console.Write(this.stack.ElementAt(i)); // need to override ToString()
                    Console.WriteLine(']');
                }
                debug.disassembleInstruction(this.chunk, currentIP);

                OpCode instruction = (OpCode) read_byte();
                switch (instruction)
                {
                    case OpCode.CONSTANT:
                        {
                            Value constant = read_constant();
                            this.stack.Push(constant);
                            Console.WriteLine(constant);
                            break;
                        }
                    case OpCode.NIL: this.stack.Push(Value.nilVal()); break;
                    case OpCode.TRUE: this.stack.Push(Value.boolVal(true)); break;
                    case OpCode.FALSE: this.stack.Push(Value.boolVal(false)); break;
                    case OpCode.POP: this.stack.Pop(); break;
                    case OpCode.GET_LOCAL:
                        {
                            byte slot = read_byte();
                            this.stack.Push(this.stack.ElementAt(slot));
                            break;
                        }
                    case OpCode.SET_LOCAL:
                        {
                            byte slot = read_byte();

                            // WTF? Stack is really an array not a stack.

                            // This is a real shame as this means setting local variables is slow.
                            Console.WriteLine(String.Format("slot index = {0}, stack peek value = {1}", slot, this.stack.Peek()));

                            Value[] tempArr = this.stack.ToArray();
                            tempArr[slot] = this.stack.Peek();
                            stack = new Stack<Value>(tempArr);
                            break;
                        }
                    case OpCode.GET_GLOBAL:
                        {
                            string name = read_string();
                            if (globals.TryGetValue(name, out Value value))
                            {
                                runtimeError(String.Format("Undefined variable {0}", name));
                                return InterpretResult.RUNTIME_ERROR;
                            }
                            this.stack.Push(value);
                            break;
                        }
                    case OpCode.DEFINE_GLOBAL:
                        {
                            string name = read_string(); // todo -- fix this
                            globals.Add(name, this.stack.Peek());
                            this.stack.Pop();
                            break;
                        }
                    case OpCode.SET_GLOBAL:
                        {
                            string name = read_string();

                            if (globals.ContainsKey(name))
                            {
                                runtimeError(String.Format("Undefined variable {0}", name));
                                return InterpretResult.RUNTIME_ERROR;
                            }
                            else
                            {
                                globals[name] = this.stack.Peek();
                            }
                            break;
                        }
                    case OpCode.EQUAL:
                        {
                            Value b = this.stack.Pop();
                            Value a = this.stack.Pop();
                            this.stack.Push(Value.boolVal(Value.valuesEqual(a, b)));
                            break;
                        }
                    case OpCode.GREATER:
                    case OpCode.LESS:
                        {
                            doComparison(instruction); 
                            break;
                        }
                    case OpCode.ADD:
                    case OpCode.SUBTRACT:
                    case OpCode.MULTIPLY:
                    case OpCode.DIVIDE:
                        {
                            binary_op(instruction);
                            break;
                        }
                    case OpCode.NOT:
                        {
                            this.stack.Push(Value.boolVal(isFalsey(this.stack.Pop())));
                            break;
                        }
                    case OpCode.NEGATE:
                        {
                            if (!Value.isNumber(this.stack.Peek()))
                            {
                                runtimeError("Operand must be a number.");
                                return InterpretResult.RUNTIME_ERROR;
                            }
                            this.stack.Push(Value.numberVal(-Value.asNumber(this.stack.Pop())));
                            break;
                        }
                    case OpCode.PRINT:
                        {
                            Console.WriteLine(this.stack.Pop());
                            break;
                        }
                    case OpCode.JUMP:
                        {
                            ushort offset = read_short();
                            currentIP += offset;
                            break;
                        }
                    case OpCode.JUMP_IF_FALSE:
                        {
                            ushort offset = read_short();
                            if (isFalsey(this.stack.Peek())) currentIP += offset;
                            break;
                        }
                    case OpCode.LOOP:
                        {
                            ushort offset = read_short();
                            currentIP -= offset;
                            break;
                        }
                    case OpCode.RETURN:
                        {
                            Console.WriteLine(this.stack.Pop());
                            return InterpretResult.OK;
                        }
                }
            }
        }

        // TODO - consider moving this from VM.cs to Value.cs
        bool isFalsey(Value value)
        {
            return Value.isNil(value) || (Value.isBool(value) && !Value.asBool(value));
        }
        
        // TODO - make the error message better. Ch 18 - pg 332
        void runtimeError(string message)
        {
            Console.WriteLine(message);
        }

    }
}
