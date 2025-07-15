using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPenaCompiler
{
    public class Lexer
    {
        private readonly string _texto;
        private int _pos = 0;
        private int _linha = 1;
        private char Atual => _pos < _texto.Length ? _texto[_pos] : '\0';

        private static readonly Dictionary<string, TipoToken> PalavrasReservadas = new()
    {
        { "if", TipoToken.TokenIF },
        { "else", TipoToken.TokenELSE },
        { "do", TipoToken.TokenDO },
        { "foreach", TipoToken.TokenFOREACH },
        { "in", TipoToken.TokenIN },
        { "while", TipoToken.TokenWHILE },
        { "for", TipoToken.TokenFOR },
        { "return", TipoToken.TokenRETURN },
        { "int", TipoToken.TokenINT },
        { "float", TipoToken.TokenFLOAT },
        { "string", TipoToken.TokenSTRING },
        { "bool", TipoToken.TokenBOOL },
        { "true", TipoToken.TokenTRUE },
        { "false", TipoToken.TokenFALSE },
        { "void", TipoToken.TokenVOID },
        { "switch", TipoToken.TokenSWITCH },
        { "case", TipoToken.TokenCASE },
        { "default", TipoToken.TokenDEFAULT },
        { "break", TipoToken.TokenBREAK },
        { "namespace", TipoToken.TokenNAMESPACE },
        { "using", TipoToken.TokenUSING },

        // Modificadores de acesso
        { "public", TipoToken.TokenPUBLIC },
        { "private", TipoToken.TokenPRIVATE },
        { "protected", TipoToken.TokenPROTECTED },
        { "internal", TipoToken.TokenINTERNAL },
        { "class", TipoToken.TokenCLASS },

    };

        public Lexer(string texto)
        {
            _texto = texto;
        }

        public List<Token> Tokenizar()
        {
            List<Token> tokens = new();

            while (Atual != '\0')
            {
                if (char.IsWhiteSpace(Atual))
                {
                    if (Atual == '\n') _linha++;
                    Avancar();
                    continue;
                }

                if (char.IsLetter(Atual) || Atual == '_')
                {
                    string id = LerEnquanto(c => char.IsLetterOrDigit(c) || c == '_');
                    TipoToken tipo = PalavrasReservadas.ContainsKey(id) ? PalavrasReservadas[id] : TipoToken.TokenIDENTIFIER;
                    tokens.Add(new Token(tipo, id, _linha));
                    continue;
                }

                if (char.IsDigit(Atual))
                {
                    StringBuilder num = new();
                    bool isFloat = false;
                    num.Append(LerEnquanto(char.IsDigit));
                    if (Atual == '.')
                    {
                        isFloat = true;
                        num.Append('.');
                        Avancar();
                        num.Append(LerEnquanto(char.IsDigit));
                    }
                    tokens.Add(new Token(isFloat ? TipoToken.TokenFLOAT_LITERAL : TipoToken.TokenNUMBER, num.ToString(), _linha));
                    continue;
                }

                if (Atual == '\"')
                {
                    Avancar();
                    StringBuilder literal = new();
                    while (Atual != '\"' && Atual != '\0')
                    {
                        if (Atual == '\n') _linha++;
                        literal.Append(Atual);
                        Avancar();
                    }
                    Avancar();
                    tokens.Add(new Token(TipoToken.TokenSTRING_LITERAL, literal.ToString(), _linha));
                    continue;
                }

                // Operadores e símbolos
                string dois = _texto.Substring(_pos, Math.Min(2, _texto.Length - _pos));
                if (dois == "==") { tokens.Add(new Token(TipoToken.TokenEQUALS, "==", _linha)); Avancar(); Avancar(); continue; }
                if (dois == "!=") { tokens.Add(new Token(TipoToken.TokenNOT_EQUALS, "!=", _linha)); Avancar(); Avancar(); continue; }
                if (dois == ">=") { tokens.Add(new Token(TipoToken.TokenGREATER_EQUAL, ">=", _linha)); Avancar(); Avancar(); continue; }
                if (dois == "<=") { tokens.Add(new Token(TipoToken.TokenLESS_EQUAL, "<=", _linha)); Avancar(); Avancar(); continue; }
                if (dois == "++") { tokens.Add(new Token(TipoToken.TokenINCREMENT, "++", _linha)); Avancar(); Avancar(); continue; }
                if (dois == "--") { tokens.Add(new Token(TipoToken.TokenDECREMENT, "--", _linha)); Avancar(); Avancar(); continue; }
                if (dois == "&&") { tokens.Add(new Token(TipoToken.TokenAND, "&&", _linha)); Avancar(); Avancar(); continue; }
                if (dois == "||") { tokens.Add(new Token(TipoToken.TokenOR, "||", _linha)); Avancar(); Avancar(); continue; }

                switch (Atual)
                {
                    case '+': tokens.Add(new Token(TipoToken.TokenPLUS, "+", _linha)); break;
                    case '-': tokens.Add(new Token(TipoToken.TokenMINUS, "-", _linha)); break;
                    case '*': tokens.Add(new Token(TipoToken.TokenMULTIPLY, "*", _linha)); break;
                    case '/': tokens.Add(new Token(TipoToken.TokenDIVIDE, "/", _linha)); break;
                    case '%': tokens.Add(new Token(TipoToken.TokenMODULO, "%", _linha)); break;
                    case '=': tokens.Add(new Token(TipoToken.TokenASSIGN, "=", _linha)); break;
                    case '>': tokens.Add(new Token(TipoToken.TokenGREATER, ">", _linha)); break;
                    case '<': tokens.Add(new Token(TipoToken.TokenLESS, "<", _linha)); break;
                    case '!': tokens.Add(new Token(TipoToken.TokenNOT, "!", _linha)); break;
                    case ';': tokens.Add(new Token(TipoToken.TokenSEMICOLON, ";", _linha)); break;
                    case ':': tokens.Add(new Token(TipoToken.TokenCOLON, ":", _linha)); break;
                    case ',': tokens.Add(new Token(TipoToken.TokenCOMMA, ",", _linha)); break;
                    case '.': tokens.Add(new Token(TipoToken.TokenDOT, ".", _linha)); break;
                    case '(': tokens.Add(new Token(TipoToken.TokenLPAREN, "(", _linha)); break;
                    case ')': tokens.Add(new Token(TipoToken.TokenRPAREN, ")", _linha)); break;
                    case '{': tokens.Add(new Token(TipoToken.TokenLBRACE, "{", _linha)); break;
                    case '}': tokens.Add(new Token(TipoToken.TokenRBRACE, "}", _linha)); break;
                    case '[': tokens.Add(new Token(TipoToken.TokenLBRACKET, "[", _linha)); break;
                    case ']': tokens.Add(new Token(TipoToken.TokenRBRACKET, "]", _linha)); break;
                    case '\'': tokens.Add(new Token(TipoToken.TokenAPOSTROPHE, "'", _linha)); break;
                    default:
                        tokens.Add(new Token(TipoToken.TokenUNKNOWN, Atual.ToString(), _linha));
                        break;
                }

                Avancar();
            }

            tokens.Add(new Token(TipoToken.TokenEOF, "", _linha));
            return tokens;
        }

        private void Avancar() => _pos++;

        private string LerEnquanto(Func<char, bool> condicao)
        {
            int inicio = _pos;
            while (condicao(Atual) && Atual != '\0') Avancar();
            return _texto.Substring(inicio, _pos - inicio);
        }
    }

}
