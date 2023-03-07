using System.Data.Common;

namespace punt
{
    // Reference https://github.com/stevehalliwell/ulox-work/blob/core_clox/Assets/ulox-working/Runtime/Compiler.cs
    public class Compiler
    {
        private Token currentToken;
        private Token previousToken;
        private List<Token> tokens;
        private int tokenIndex = 0;

        private Chunk currentChunk => compilerState.Peek().chunk; // current->function->chunk.
        private Dictionary<TokenType, ParseRule> rules;

        // private Stack<Value> locals; // local variables

        private Stack<CompilerState> compilerState = new Stack<CompilerState>();

        public class ParseRule
        {
            public Action<bool> prefix;
            public Action<bool> infix;
            public Precedence precedence;

            public ParseRule(Action<bool> prefix, Action<bool> infix, Precedence precedence)
            {
                this.prefix = prefix;
                this.infix = infix;
                this.precedence = precedence;
            }
        }

        /// <summary>
        /// Precedence Rules - Pratt Parser. 
        /// </summary>
        private Dictionary<TokenType, ParseRule> getRules()
        {
            return new Dictionary<TokenType, ParseRule>()
            {
                { TokenType.LEFT_PAREN,    new ParseRule(grouping, null, Precedence.NONE) },
                { TokenType.RIGHT_PAREN,   new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.LEFT_BRACE,    new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.RIGHT_BRACE,   new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.COMMA,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.DOT,           new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.MINUS,         new ParseRule(unary,    binary, Precedence.TERM) },
                { TokenType.PLUS,          new ParseRule(null,     binary, Precedence.TERM) },
                { TokenType.SEMICOLON,     new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.SLASH,         new ParseRule(null,     binary, Precedence.FACTOR) },
                { TokenType.STAR,          new ParseRule(null,     binary, Precedence.FACTOR) },
                { TokenType.BANG,          new ParseRule(unary,    null, Precedence.NONE) },
                { TokenType.BANG_EQUAL,    new ParseRule(null,     binary, Precedence.EQUALITY) },
                { TokenType.EQUAL,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.EQUAL_EQUAL,   new ParseRule(null,     binary, Precedence.EQUALITY) },
                { TokenType.GREATER,       new ParseRule(null,     binary, Precedence.COMPARISON) },
                { TokenType.GREATER_EQUAL, new ParseRule(null,     binary, Precedence.COMPARISON) },
                { TokenType.LESS,          new ParseRule(null,     binary, Precedence.COMPARISON) },
                { TokenType.LESS_EQUAL,    new ParseRule(null,     binary, Precedence.COMPARISON) },
                { TokenType.IDENTIFIER,    new ParseRule(variable,     null, Precedence.NONE) },
                { TokenType.STRING,        new ParseRule(puntString,   null, Precedence.NONE) },
                { TokenType.NUMBER,        new ParseRule(number,   null, Precedence.NONE) },
                { TokenType.AND,           new ParseRule(null,     and_, Precedence.AND) },
                { TokenType.CLASS,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.ELSE,          new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.FALSE,         new ParseRule(literal,  null, Precedence.NONE) },
                { TokenType.FOR,           new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.FUN,           new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.IF,            new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.NIL,           new ParseRule(literal,  null, Precedence.NONE) },
                { TokenType.OR,            new ParseRule(null,     or_, Precedence.OR) },
                { TokenType.PRINT,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.RETURN,        new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.SUPER,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.THIS,          new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.TRUE,          new ParseRule(literal,  null, Precedence.NONE) },
                { TokenType.VAR,           new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.WHILE,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.ERROR,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.EOF,           new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.BREAK,         new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.CONTINUE,      new ParseRule(null,     null, Precedence.NONE) },
                { TokenType.ARROW,         new ParseRule(null,     null, Precedence.NONE) }
            };
        }


        public enum FunctionType {
            FUNCTION,
            SCRIPT // Top Level
        }

        struct Local
        {
            public Token name;
            public int depth;
        }

        // Stacks in C# act weird. For now following the guide.
        class CompilerState
        {
            public Local[] locals = new Local[byte.MaxValue + 1];
            public int localCount;
            public int scopeDepth;

            public Chunk chunk;
            public FunctionType functionType;

            public CompilerState(FunctionType funcType)
            {
                localCount = 0;
                scopeDepth = 0;
                functionType = funcType;
            }
        }

        public Compiler()
        {
            rules = getRules();

            // current = new CompilerState();
            // locals = new Stack<Value>();

            PushCompilerState(FunctionType.SCRIPT);

        }

        public void PushCompilerState(FunctionType funcType)
        {

            // object initializer. Just like properties.
            // MyObject myObjectInstance = new MyObject(param1, param2);
            // myObjectInstance.MyProperty = someValue;
            // https://stackoverflow.com/questions/740658/whats-the-difference-between-an-object-initializer-and-a-constructor
            compilerState.Push(new CompilerState(funcType)
            {
                chunk = new Chunk()
            });

        }

        // called by VM.cs
        // calls scanner
        public void compile(List<Token> scannedTokens)
        {
            tokens = scannedTokens;

            advance();

            while (!match(TokenType.EOF))
            {
                declaration();
            }

            endCompiler();
        }

        void advance()
        {
            previousToken = currentToken;
            currentToken = tokens[tokenIndex];
            tokenIndex++;
        }

        void expression()
        {
            parsePrecendence(Precedence.ASSIGNMENT);
        }

        void block()
        {
            while (!check(TokenType.RIGHT_BRACE) && !check(TokenType.EOF))
            {
                declaration();
            }

            consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        }

        void varDeclaration()
        {
            byte global = parseVariable("Expect variable name.");

            if (match(TokenType.EQUAL))
            {
                expression();
            }
            else
            {
                emitByte(OpCode.NIL);
            }

            consume(TokenType.SEMICOLON, "Expect ';' after variable declaration."); // is this right?

            defineVariable(global);
        }

        void declaration()
        {
            if (match(TokenType.VAR))
            {
                varDeclaration();
            }
            else
            {
                statement();
            }
        }

        void statement()
        {
            if (match(TokenType.PRINT))
            {
                printStatement();
            }
            else if (match(TokenType.FOR))
            {
                forStatement();
            }
            else if (match(TokenType.IF))
            {
                ifStatement();
            }
            else if (match(TokenType.WHILE))
            {
                whileStatement();
            }
            else if (match(TokenType.LEFT_BRACE))
            {
                beginScope();
                block();
                endScope();
            }
            else
            {
                expressionStatement();
            }
        }

        void printStatement()
        {
            expression();
            consume(TokenType.SEMICOLON, "Expect ';' after value."); // is this correct?
            emitByte(OpCode.PRINT);
        }

        void expressionStatement()
        {
            expression();
            consume(TokenType.SEMICOLON, "Expect ';' after expression."); // is this correct?
            emitByte(OpCode.POP);
        }

        void forStatement()
        {
            beginScope();
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            if (match(TokenType.SEMICOLON))
            {
                // no initializer;
            } 
            else if (match(TokenType.VAR))
            {
                varDeclaration();
            } 
            else
            {
                expressionStatement();
            }

            int loopStart = currentChunk.instructions.Count;

            int exitJump = -1;
            if (!match(TokenType.SEMICOLON))
            {
                expression();
                consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

                // jump out of the loop if the condition is false.
                exitJump = emitJump(OpCode.JUMP_IF_FALSE);
                emitByte(OpCode.POP);
            }

            // increment clause
            if (!match(TokenType.RIGHT_PAREN))
            {
                int bodyJump = emitJump(OpCode.JUMP);
                int incrementStart = currentChunk.instructions.Count;
                expression();
                emitByte(OpCode.POP);
                consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

                // takes use back to the top of the 'for' loop.
                emitLoop(loopStart);
                loopStart = incrementStart;
                patchJump(bodyJump);
            }

            statement();
            emitLoop(loopStart);

            if (exitJump != -1)
            {
                patchJump(exitJump);
                emitByte(OpCode.POP);
            }

            endScope();
        }

        void ifStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

            int thenJump = emitJump(OpCode.JUMP_IF_FALSE);
            emitByte(OpCode.POP);
            statement();

            int elseJump = emitJump(OpCode.JUMP);

            patchJump(thenJump);
            emitByte(OpCode.POP);

            if (match(TokenType.ELSE)) statement();
            patchJump(elseJump);
        }

        void whileStatement()
        {
            int loopStart = currentChunk.instructions.Count;
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
        
            int exitJump = emitJump(OpCode.JUMP_IF_FALSE);
            emitByte(OpCode.POP);
            statement();
            emitLoop(loopStart);

            patchJump(exitJump);
            emitByte(OpCode.POP);
        }

        void synchronize()
        {
            // paniceMode = false;

            while (currentToken.type != TokenType.EOF)
            {
                if (previousToken.type == TokenType.SEMICOLON) return;

                switch (currentToken.type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                                        
                }

                advance();
            }
        }

        bool match(TokenType type)
        {
            if (!check(type)) return false;
            advance();
            return true;
        }
        
        bool check(TokenType type)
        {
            return currentToken.type == type;
        }

        void consume(TokenType type, string message)
        {
            if (currentToken.type == type)
            {
                advance();
                return;
            }

            throw new Exception(message);
        }

        void emitByte(OpCode op)
        {
            currentChunk.writeByte((byte) op, previousToken.line);
        }

        void emitBytes(OpCode opA, OpCode opB)
        {
            emitByte(opA);
            emitByte(opB);
        }

        void emitLoop(int loopStart)
        {
            emitByte(OpCode.LOOP);

            int offset = currentChunk.instructions.Count - loopStart + 2;
            if (offset > ushort.MaxValue) error("Loop body too large.");

            emitByte((OpCode) ((offset >> 8) & 0xff));
            emitByte((OpCode) (offset & 0xff));
        }

        int emitJump(OpCode instruction)
        {
            emitByte(instruction);
            emitByte((OpCode) 0xff);
            emitByte((OpCode) 0xff);
            return currentChunk.instructions.Count - 2;
        }

        void emitReturn()
        {
            emitByte(OpCode.RETURN);
        }

        void emitConstant(Value value)
        {
            // called by number() and string()
            emitBytes(OpCode.CONSTANT, (OpCode) makeConstant(value));
        }

        void patchJump(int offset)
        {
            // -2 to adjust for the bytecode for the jump offset itself.
            int jump = currentChunk.instructions.Count - offset - 2;

            if (jump > ushort.MaxValue)
            {
                error("Too much code to jump over.");
            }

            currentChunk.instructions[offset] = (byte)((jump >> 8) & 0xff);
            currentChunk.instructions[offset + 1] = (byte)(jump & 0xff);
        }

        byte makeConstant(Value value)
        {
            return currentChunk.writeConstant(value);
        }

        void endCompiler()
        {
            emitReturn();

            // if (!parser.hadError)
            Debug debug = new Debug();
            debug.disassembleChunk(currentChunk, "code");
        }

        void beginScope()
        {
            compilerState.Peek().scopeDepth++;
        }

        void endScope()
        {
            var current = compilerState.Peek();

            current.scopeDepth--;

            while (current.localCount > 0 && current.locals[current.localCount - 1].depth > current.scopeDepth)
            {
                emitByte(OpCode.POP);
                current.localCount--;

                current = compilerState.Peek(); // is this needed?
            }
        }


        #region Infix

        void binary(bool canAssign)
        {
            TokenType operatorType = previousToken.type;
            ParseRule rule = getRule(operatorType);

            parsePrecendence((Precedence)(rule.precedence + 1));

            switch (operatorType)
            {
                case TokenType.BANG_EQUAL:    emitBytes(OpCode.EQUAL, OpCode.NOT); break;
                case TokenType.EQUAL_EQUAL:   emitByte(OpCode.EQUAL); break;
                case TokenType.GREATER:       emitByte(OpCode.GREATER); break;
                case TokenType.GREATER_EQUAL: emitBytes(OpCode.LESS, OpCode.NOT); break;
                case TokenType.LESS:          emitByte(OpCode.LESS); break;
                case TokenType.LESS_EQUAL:    emitBytes(OpCode.GREATER, OpCode.NOT); break;
                case TokenType.PLUS:          emitByte(OpCode.ADD); break;
                case TokenType.MINUS:         emitByte(OpCode.SUBTRACT); break;
                case TokenType.STAR:          emitByte(OpCode.MULTIPLY); break;
                case TokenType.SLASH:         emitByte(OpCode.DIVIDE); break;
                default:
                    return;
            }
        }

        void and_(bool canAssign)
        {
            int endJump = emitJump(OpCode.JUMP_IF_FALSE);

            emitByte(OpCode.POP);
            parsePrecendence(Precedence.AND);

            patchJump(endJump);
        }

        void or_(bool canAssign)
        {
            int elseJump = emitJump(OpCode.JUMP_IF_FALSE);
            int endJump = emitJump(OpCode.JUMP);

            patchJump(elseJump);
            emitByte(OpCode.POP);

            parsePrecendence(Precedence.OR);
            patchJump(endJump);
        }

        #endregion

        #region Prefix

        void unary(bool canAssign)
        {
            TokenType operatorType = previousToken.type;

            parsePrecendence(Precedence.UNARY);

            switch (operatorType)
            {
                case TokenType.MINUS: emitByte(OpCode.NEGATE); break;
                case TokenType.BANG: emitByte(OpCode.NOT); break;
                default: return;
            }
        }
        void grouping(bool canAssign)
        {
            expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
        }

        void number(bool canAssign)
        {
            double value = ((IConvertible) previousToken.literal).ToDouble(null);
            emitConstant(Value.numberVal(value));
        }

        // 'string' is a C# reserved keyword.
        void puntString(bool canAssign)
        {            
            emitConstant(Value.stringVal(previousToken.lexeme));
        }

        void variable(bool canAssign)
        {
            namedVariable(previousToken, canAssign);
        }

        void namedVariable(Token token, bool canAssign)
        {
            OpCode getOp;
            OpCode setOp;

            int arg = resolveLocal(compilerState.Peek(), token);

            if (arg != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                arg = identifierConstant(token);
                getOp = OpCode.GET_GLOBAL;
                setOp = OpCode.SET_GLOBAL;
            }

            /*
             * This is for the case where we have
             * 
             * menu.brunch(sunday).beverage = "mimosa"
             * 
             * 'beverage' should be compiled as a set expression and not a getter.
             * 
             * '=' means variable assignment.
             */
            if (canAssign && match(TokenType.EQUAL))
            {
                expression();
                emitBytes(setOp, (OpCode) arg);
            }
            else
            {
                emitBytes(getOp, (OpCode) arg);
            }
        }

        void literal(bool canAssign)
        {
            switch (previousToken.type)
            {
                case TokenType.FALSE: emitByte(OpCode.FALSE); break;
                case TokenType.NIL: emitByte(OpCode.NIL); break;
                case TokenType.TRUE: emitByte(OpCode.TRUE); break;
                default: return;
            }
        }

        #endregion

        void parsePrecendence(Precedence precedence)
        {
            advance();
            Action<bool> prefixRule = getRule(previousToken.type).prefix;

            if (prefixRule == null)
            {
                error("Expect expression.");
                return;
            }

            /*
             * variable() should look for and consume the '=' only if it's in the context of a low-precedence expression.
             * 
             * If the variable happens to be the rhs of an infix operator or the operand of a unary operator, then
             * that containing expression is too high precedence to permit the '='
             * 
             * we don't want a case like
             * a * b = c * d (where the = tries to do an assignment)
             */
            bool canAssign = precedence <= Precedence.ASSIGNMENT;
            prefixRule(canAssign);

            while (precedence <= getRule(currentToken.type).precedence)
            {
                advance();
                Action<bool> infixRule = getRule(previousToken.type).infix;
                infixRule(canAssign);
            }

            if (canAssign && match(TokenType.EQUAL))
            {
                error("Invalid assignment target.");
            }
        }

        ParseRule getRule(TokenType operatorType)
        {
            return rules[operatorType];
        }
        
        byte parseVariable(string errorMessage)
        {
            consume(TokenType.IDENTIFIER, errorMessage);

            declareVariable();
            if (compilerState.Peek().scopeDepth > 0) return 0;

            return identifierConstant(previousToken);
        }

        byte identifierConstant(Token token)
        {
            return makeConstant(Value.stringVal(token.lexeme));
        }

        // todo - consider deleting this function.
        bool identifiersEqual(Token a, Token b)
        {
            return a == b;
        }

        int resolveLocal(CompilerState compiler, Token token)
        {
            for (int i = compiler.localCount - 1; i >= 0; i--)
            {
                Local local = compiler.locals[i];
                if (identifiersEqual(token, local.name))
                {
                    if (local.depth == -1)
                    {
                        error("Can't read local variable in its own initializer.");
                    }
                    return i;
                }
            }
            return -1;
        }

        void addLocal(Token name)
        {
            var current = compilerState.Peek();

            if (current.localCount == byte.MaxValue)
            {
                error("Too many local variables in function.");
                return;
            }

            Local local = current.locals[current.localCount++];
            local.name = name;
            local.depth = -1;
        }

        void declareVariable()
        {
            var current = compilerState.Peek();

            if (current.scopeDepth == 0) return;

            Token name = previousToken;

            for (int i = current.localCount - 1; i >= 0; i--)
            {
                Local local = current.locals[i];
                if (local.depth != -1 && local.depth < current.scopeDepth) break;

                if (identifiersEqual(name, local.name))
                {
                    error("Already a variable with this name in this scope.");
                }
            }

            addLocal(name);
        }

        /// <summary>
        /// Global variables are looked up by name at runtime.
        /// 
        /// That means the VM needs access to the name. 
        /// A whole string is too big to stuff into the bytecode stream as an operand.
        /// Instead we store the string in the constant table (dictionary)
        /// and the instruction then refers to the name by its index in the table.
        /// 
        /// This function returns that index all the way to varDeclaration().
        /// </summary>
        /// <param name="global"></param>
        void defineVariable(byte global)
        {
            if (compilerState.Peek().scopeDepth > 0)
            {
                markInitialized();
                return;
            }

            emitBytes(OpCode.DEFINE_GLOBAL, (OpCode) global);
        }

        void markInitialized()
        {
            var current = compilerState.Peek();
            current.locals[current.localCount - 1].depth = current.scopeDepth;
        }

        #region Error Handling

        void errorAtCurrent(string message)
        {
            errorAt(currentToken, message);
        }

        void error(string message)
        {
            errorAt(previousToken, message);
        }

        void errorAt(Token token, string message)
        {
            Console.Write(String.Format("[line {0}] Error", token.line));

            if (token.type == TokenType.EOF)
            {
                Console.Write(" at end");
            } 
            else if (token.type == TokenType.ERROR)
            {
                // do nothing
            }
            else
            {
                Console.Write(" at {0}", token.literal);
            }

            Console.WriteLine(message);
            // parser.hadError = true;
        }

        #endregion
    }
}
