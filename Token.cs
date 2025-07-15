using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPenaCompiler
{
    public class Token
    {
        public TipoToken Tipo { get; }
        public string Lexema { get; }
        public int Linha { get; }

        public Token(TipoToken tipo, string lexema, int linha)
        {
            Tipo = tipo;
            Lexema = lexema;
            Linha = linha;
        }

        public override string ToString()
        {
            return $"[{Tipo}] '{Lexema}' (linha {Linha})";
        }
    }

}
