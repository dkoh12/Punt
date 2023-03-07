using System;

namespace punt
{
    public class Punt
    { 
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                runPrompt();
            } 
            else if (args.Length == 1)
            {
                runFile(args[0]);
            }
            else
            {
                Console.WriteLine("Pass in a single file or no arguments to punt.");
                Environment.Exit(64);
            }            
        }

        private static void runFile(string path)
        {
            // read a file and run

            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);

                run(content);

                // if (hadError) Environment.Exit(65);

                // if (hadRuntimeError) Environment.Exit(70);
            }
            else
            {
                Environment.Exit(65);
            }
        }

        // REPL
        private static void runPrompt()
        {
            // read a stream from user

            while(true)
            {
                Console.Write("> ");
                string? line = Console.ReadLine();
                if (line == null)
                {
                    break;
                }
                run(line);
                // hadError = false;
            }
        }

        private static void run(String source)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.scanTokens();

            Compiler compiler = new Compiler();
            compiler.compile(tokens);

            Chunk chunk = new Chunk();
            Stack<Value> stack = new Stack<Value>();

            VM vm = new VM(chunk, stack);
            vm.interpret(chunk);

            
            int constant = chunk.writeConstant(Value.numberVal(1.2));
            chunk.writeByte((byte)OpCode.CONSTANT, 123);
            chunk.writeByte((byte)constant, 123);


            constant = chunk.writeConstant(Value.numberVal(3.4));
            chunk.writeByte((byte)OpCode.CONSTANT, 123);
            chunk.writeByte((byte)constant, 123);

            chunk.writeByte((byte)OpCode.ADD, 123);

            constant = chunk.writeConstant(Value.numberVal(5.6));
            chunk.writeByte((byte)OpCode.CONSTANT, 123);
            chunk.writeByte((byte)constant, 123);

            chunk.writeByte((byte)OpCode.DIVIDE, 123);
            chunk.writeByte((byte)OpCode.NEGATE, 123);

            chunk.writeByte((byte)OpCode.RETURN, 123);



            Debug debug = new Debug();

            debug.disassembleChunk(chunk, "test chunk");
        }

    }
}