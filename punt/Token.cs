namespace punt
{
    public class Token
    {
        public TokenType type; // token type enum
        public string lexeme; // actual token character in the text. This will contain string with quotes.
        public object literal; // literal token character. This will contain just string chars.
        public int line; // line number for error reporting

        public Token(TokenType type, string lexeme, object literal, int line)
        {
            this.type = type;
            this.lexeme = lexeme;
            this.literal = literal;
            this.line = line;
        }

        public string toString()
        {
            return type + " " + lexeme + " " + literal;
        }
    }
}
