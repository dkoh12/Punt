namespace punt
{
    /// <summary>
    /// For generating bytecode instructions for IR
    /// one byte operation code (aka bytecode / opcode)
    /// </summary>
    public enum OpCode
    {
        CONSTANT,
        NIL,
        TRUE,
        FALSE,
        POP,
        GET_LOCAL,
        SET_LOCAL,
        GET_GLOBAL,
        DEFINE_GLOBAL,
        SET_GLOBAL,
        GET_UPVALUE,
        SET_UPVALUE,
        GET_PROPERY,
        SET_PROPERY,
        GET_SUPER,
        EQUAL,
        GREATER,
        LESS,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        NOT,
        NEGATE,
        PRINT,
        JUMP,
        JUMP_IF_FALSE,
        LOOP,
        CALL,
        INVOKE,
        SUPER_INVOKE,
        CLOSURE,
        CLOSE_UPVALUE,
        RETURN,
        CLASS,
        INHERIT,
        METHOD
    }

    /// <summary>
    /// For Scanning - Lexical Analysis
    /// </summary>
    public enum TokenType
    {
        // single character tokens
        LEFT_PAREN,
        RIGHT_PAREN,
        LEFT_BRACE,
        RIGHT_BRACE,
        LEFT_BRACKET,
        RIGHT_BRACKET,
        COMMA,
        DOT,
        MINUS,
        PLUS,
        SEMICOLON,
        SLASH,
        STAR,

        // one or two character tokens
        BANG,
        BANG_EQUAL,
        EQUAL,
        EQUAL_EQUAL,
        GREATER,
        GREATER_EQUAL,
        LESS,
        LESS_EQUAL,
        ARROW,

        // literals
        IDENTIFIER,
        STRING,
        NUMBER,

        // keywords
        AND,
        BREAK,
        CLASS,
        CONTINUE,
        ELSE,
        FALSE,
        FOR,
        FUN,
        IF,
        NIL,
        OR,
        PRINT,
        RETURN,
        SUPER,
        THIS,
        TRUE,
        VAR,
        WHILE,

        ERROR,
        EOF
    }

    /// <summary>
    /// The order of the Enums matter!
    /// 
    /// We walk down the grammar in recursive descent top down.
    /// The enums that come later have higher precedence
    /// </summary>
    public enum Precedence
    {
        NONE,
        ASSIGNMENT, // =
        OR,         // or
        AND,        // and
        EQUALITY,   // == !=
        COMPARISON, // < > <= >=
        TERM,       // + -
        FACTOR,     // * /
        UNARY,      // ! -
        CALL,       // . ()
        PRIMARY
    }

    public enum FunctionType
    {
        Script,
        Function,
        Method,
        Init
    }

    public enum InterpretResult
    {
        OK,
        COMPILE_ERROR,
        RUNTIME_ERROR
    }
}
