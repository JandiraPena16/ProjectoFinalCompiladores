using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPenaCompiler
{
    // ========================================
    // CLASSE BASE PARA TODOS OS NÓS DA AST
    // ========================================

    public abstract class ASTNode
    {
        public int Linha { get; set; }
        public abstract void Aceitar(IVisitorAST visitor);
    }

    // ========================================
    // INTERFACE VISITOR PARA ANÁLISE SEMÂNTICA
    // ========================================

    public interface IVisitorAST
    {
        void Visitar(ProgramaNode node);
        void Visitar(NamespaceNode node);
        void Visitar(ClasseNode node);
        void Visitar(MetodoNode node);
        void Visitar(DeclaracaoVariavelNode node);
        void Visitar(ComandoIfNode node);
        void Visitar(ComandoWhileNode node);
        void Visitar(ComandoForNode node);
        void Visitar(ComandoDoWhileNode node);
        void Visitar(ComandoForeachNode node);
        void Visitar(ComandoSwitchNode node);
        void Visitar(ComandoReturnNode node);
        void Visitar(ComandoBreakNode node);
        void Visitar(BlocoComandosNode node);
        void Visitar(AtribuicaoNode node);
        void Visitar(ChamadaMetodoNode node);
        void Visitar(ExpressaoBinariaNode node);
        void Visitar(ExpressaoUnariaNode node);
        void Visitar(LiteralNode node);
        void Visitar(IdentificadorNode node);
    }

    // ========================================
    // NÓS DE PROGRAMA E ESTRUTURA
    // ========================================

    public class ProgramaNode : ASTNode
    {
        public List<string> Usings { get; set; } = new List<string>();
        public NamespaceNode Namespace { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class NamespaceNode : ASTNode
    {
        public string Nome { get; set; }
        public List<ClasseNode> Classes { get; set; } = new List<ClasseNode>();

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ClasseNode : ASTNode
    {
        public string Nome { get; set; }
        public string ModificadorAcesso { get; set; } = "internal"; // default
        public List<DeclaracaoVariavelNode> Campos { get; set; } = new List<DeclaracaoVariavelNode>();
        public List<MetodoNode> Metodos { get; set; } = new List<MetodoNode>();

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    // ========================================
    // NÓS DE DECLARAÇÕES
    // ========================================

    public class MetodoNode : ASTNode
    {
        public string Nome { get; set; }
        public string TipoRetorno { get; set; }
        public string ModificadorAcesso { get; set; }
        public List<ParametroNode> Parametros { get; set; } = new List<ParametroNode>();
        public BlocoComandosNode Corpo { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ParametroNode : ASTNode
    {
        public string Tipo { get; set; }
        public string Nome { get; set; }

        public override void Aceitar(IVisitorAST visitor) { /* Parâmetros são visitados pelo método pai */ }
    }

    public class DeclaracaoVariavelNode : ASTNode
    {
        public string Tipo { get; set; }
        public string Nome { get; set; }
        public ASTNode Inicializador { get; set; } // Pode ser null
        public bool EhCampo { get; set; } = false; // Para distinguir campo de variável local

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    // ========================================
    // NÓS DE COMANDOS
    // ========================================

    public class BlocoComandosNode : ASTNode
    {
        public List<ASTNode> Comandos { get; set; } = new List<ASTNode>();

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoIfNode : ASTNode
    {
        public ASTNode Condicao { get; set; }
        public ASTNode ComandoEntao { get; set; }
        public ASTNode ComandoSenao { get; set; } // Pode ser null

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoWhileNode : ASTNode
    {
        public ASTNode Condicao { get; set; }
        public ASTNode Comando { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoForNode : ASTNode
    {
        public ASTNode Inicializacao { get; set; } // Pode ser DeclaracaoVariavel ou Atribuicao
        public ASTNode Condicao { get; set; }
        public ASTNode Incremento { get; set; }
        public ASTNode Comando { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoDoWhileNode : ASTNode
    {
        public BlocoComandosNode Bloco { get; set; }
        public ASTNode Condicao { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoForeachNode : ASTNode
    {
        public string TipoVariavel { get; set; }
        public string NomeVariavel { get; set; }
        public ASTNode Colecao { get; set; }
        public ASTNode Comando { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoSwitchNode : ASTNode
    {
        public ASTNode Expressao { get; set; }
        public List<CaseSwitchNode> Cases { get; set; } = new List<CaseSwitchNode>();
        public List<ASTNode> DefaultComandos { get; set; } = new List<ASTNode>(); // Pode estar vazio

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class CaseSwitchNode : ASTNode
    {
        public ASTNode Valor { get; set; } // Literal ou constante
        public List<ASTNode> Comandos { get; set; } = new List<ASTNode>();

        public override void Aceitar(IVisitorAST visitor) { /* Visitado pelo switch pai */ }
    }

    public class ComandoReturnNode : ASTNode
    {
        public ASTNode Expressao { get; set; } // Pode ser null para void

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ComandoBreakNode : ASTNode
    {
        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class AtribuicaoNode : ASTNode
    {
        public string NomeVariavel { get; set; }
        public ASTNode Expressao { get; set; }
        public bool EhArrayAccess { get; set; } = false;
        public ASTNode IndiceArray { get; set; } // Para array[indice] = valor

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ChamadaMetodoNode : ASTNode
    {
        public string NomeObjeto { get; set; } // Para obj.metodo() ou null para metodo()
        public string NomeMetodo { get; set; }
        public List<ASTNode> Argumentos { get; set; } = new List<ASTNode>();

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    // ========================================
    // NÓS DE EXPRESSÕES
    // ========================================

    public class ExpressaoBinariaNode : ASTNode
    {
        public ASTNode Esquerda { get; set; }
        public string Operador { get; set; } // +, -, *, /, ==, !=, <, >, etc.
        public ASTNode Direita { get; set; }

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class ExpressaoUnariaNode : ASTNode
    {
        public string Operador { get; set; } // -, !, ++, --
        public ASTNode Operando { get; set; }
        public bool EhPrefixo { get; set; } = true; // true para ++x, false para x++

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class LiteralNode : ASTNode
    {
        public string Valor { get; set; }
        public string Tipo { get; set; } // "int", "float", "string", "bool"

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    public class IdentificadorNode : ASTNode
    {
        public string Nome { get; set; }
        public bool EhArrayAccess { get; set; } = false;
        public ASTNode IndiceArray { get; set; } // Para array[indice]

        public override void Aceitar(IVisitorAST visitor) => visitor.Visitar(this);
    }

    // ========================================
    // ENUMS AUXILIARES
    // ========================================

    public enum TipoSimbolo
    {
        Variavel,
        Metodo,
        Classe,
        Parametro,
        Campo
    }

    public enum ModificadorAcesso
    {
        Public,
        Private,
        Protected,
        Internal
    }
}
