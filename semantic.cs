using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPenaCompiler
{
    // ========================================
    // CLASSE PRINCIPAL DO ANALISADOR SEMÂNTICO
    // ========================================

    public class Semantic : IVisitorAST
    {
        private TabelaSimbolos tabelaSimbolos;
        private List<ErroSemantico> erros;
        private string escopoAtual;
        private string classeAtual;
        private string metodoAtual;
        private bool dentroDeLoop = false;

        public List<ErroSemantico> Erros => erros;

        public Semantic()
        {
            tabelaSimbolos = new TabelaSimbolos();
            erros = new List<ErroSemantico>();
            escopoAtual = "global";
        }

        public void Analisar(ProgramaNode programa)
        {
            try
            {
                programa.Aceitar(this);
            }
            catch (Exception ex)
            {
                erros.Add(new ErroSemantico($"Erro crítico na análise semântica: {ex.Message}", 0, escopoAtual));
            }
        }

        // ========================================
        // IMPLEMENTAÇÃO DO VISITOR PATTERN
        // ========================================

        public void Visitar(ProgramaNode node)
        {
            if (node.Namespace != null)
            {
                node.Namespace.Aceitar(this);
            }
        }

        public void Visitar(NamespaceNode node)
        {
            escopoAtual = node.Nome;

            foreach (var classe in node.Classes)
            {
                classe.Aceitar(this);
            }
        }

        public void Visitar(ClasseNode node)
        {
            classeAtual = node.Nome;
            string escopoClasse = $"{escopoAtual}.{node.Nome}";

            // Verificar se classe já foi declarada
            if (tabelaSimbolos.ExisteNoEscopoAtual(node.Nome))
            {
                erros.Add(new ErroSemantico($"Classe '{node.Nome}' já foi declarada", node.Linha, escopoAtual));
                return;
            }

            // Adicionar classe à tabela de símbolos
            var simboloClasse = new Simbolo
            {
                Nome = node.Nome,
                Tipo = TipoSimbolo.Classe,
                TipoDado = "class",
                Linha = node.Linha,
                Escopo = escopoAtual,
                ModificadorAcesso = node.ModificadorAcesso
            };
            tabelaSimbolos.AdicionarSimbolo(simboloClasse);

            // Entrar no escopo da classe
            tabelaSimbolos.EntrarEscopo(escopoClasse);

            try
            {
                // Analisar campos da classe
                foreach (var campo in node.Campos)
                {
                    campo.EhCampo = true;
                    campo.Aceitar(this);
                }

                // Analisar métodos da classe
                foreach (var metodo in node.Metodos)
                {
                    metodo.Aceitar(this);
                }
            }
            finally
            {
                // Sair do escopo da classe
                tabelaSimbolos.SairEscopo();
                classeAtual = null;
            }
        }

        public void Visitar(MetodoNode node)
        {
            metodoAtual = node.Nome;
            string escopoMetodo = $"{tabelaSimbolos.EscopoAtual}.{node.Nome}";

            // Verificar se método já foi declarado na classe
            if (tabelaSimbolos.ExisteNoEscopoAtual(node.Nome))
            {
                erros.Add(new ErroSemantico($"Método '{node.Nome}' já foi declarado nesta classe", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Verificar se tipo de retorno é válido
            if (!TipoValido(node.TipoRetorno))
            {
                erros.Add(new ErroSemantico($"Tipo de retorno '{node.TipoRetorno}' inválido", node.Linha, tabelaSimbolos.EscopoAtual));
            }

            // Criar símbolo do método
            var simboloMetodo = new Simbolo
            {
                Nome = node.Nome,
                Tipo = TipoSimbolo.Metodo,
                TipoDado = node.TipoRetorno,
                Linha = node.Linha,
                Escopo = tabelaSimbolos.EscopoAtual,
                ModificadorAcesso = node.ModificadorAcesso,
                Parametros = node.Parametros.Select(p => $"{p.Tipo} {p.Nome}").ToList()
            };
            tabelaSimbolos.AdicionarSimbolo(simboloMetodo);

            // Entrar no escopo do método
            tabelaSimbolos.EntrarEscopo(escopoMetodo);

            try
            {
                // Adicionar parâmetros ao escopo do método
                foreach (var parametro in node.Parametros)
                {
                    if (!TipoValido(parametro.Tipo))
                    {
                        erros.Add(new ErroSemantico($"Tipo de parâmetro '{parametro.Tipo}' inválido", parametro.Linha, escopoMetodo));
                        continue;
                    }

                    if (tabelaSimbolos.ExisteNoEscopoAtual(parametro.Nome))
                    {
                        erros.Add(new ErroSemantico($"Parâmetro '{parametro.Nome}' já foi declarado", parametro.Linha, escopoMetodo));
                        continue;
                    }

                    var simboloParametro = new Simbolo
                    {
                        Nome = parametro.Nome,
                        Tipo = TipoSimbolo.Parametro,
                        TipoDado = parametro.Tipo,
                        Linha = parametro.Linha,
                        Escopo = escopoMetodo,
                        Inicializada = true // Parâmetros são sempre inicializados
                    };
                    tabelaSimbolos.AdicionarSimbolo(simboloParametro);
                }

                // Analisar corpo do método
                if (node.Corpo != null)
                {
                    node.Corpo.Aceitar(this);
                }
            }
            finally
            {
                // Sair do escopo do método
                tabelaSimbolos.SairEscopo();
                metodoAtual = null;
            }
        }

        public void Visitar(DeclaracaoVariavelNode node)
        {
            // Analisar tipo e dimensão
            var tipoInfo = AnalisarTipoEDimensao(node.Tipo);

            // Verificar se tipo base é válido
            if (!TipoValido(tipoInfo.TipoBase))
            {
                erros.Add(new ErroSemantico($"Tipo '{tipoInfo.TipoBase}' inválido", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Verificar se variável já foi declarada no escopo atual
            if (tabelaSimbolos.ExisteNoEscopoAtual(node.Nome))
            {
                erros.Add(new ErroSemantico($"Variável '{node.Nome}' já foi declarada neste escopo", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Analisar inicializador se existir
            string tipoInicializador = null;
            if (node.Inicializador != null)
            {
                tipoInicializador = ObterTipoExpressao(node.Inicializador);

                // Verificar compatibilidade de tipos e dimensões
                if (tipoInicializador != null && !TiposEDimensoesCompativeis(tipoInfo.TipoCompleto, tipoInicializador, tipoInfo.Dimensao))
                {
                    erros.Add(new ErroSemantico($"Não é possível converter '{tipoInicializador}' para '{tipoInfo.TipoCompleto}'", node.Linha, tabelaSimbolos.EscopoAtual));
                }
            }

            // Adicionar variável à tabela de símbolos
            var simbolo = new Simbolo
            {
                Nome = node.Nome,
                Tipo = node.EhCampo ? TipoSimbolo.Campo : TipoSimbolo.Variavel,
                TipoDado = tipoInfo.TipoBase,
                Dimensao = tipoInfo.Dimensao,
                TamanhoArray = tipoInfo.TamanhoArray,
                Linha = node.Linha,
                Escopo = tabelaSimbolos.EscopoAtual,
                Inicializada = node.Inicializador != null
            };
            tabelaSimbolos.AdicionarSimbolo(simbolo);

            // Visitar inicializador
            if (node.Inicializador != null)
            {
                node.Inicializador.Aceitar(this);
            }
        }

        public void Visitar(ComandoIfNode node)
        {
            // Verificar tipo da condição
            string tipoCondicao = ObterTipoExpressao(node.Condicao);
            if (tipoCondicao != null && tipoCondicao != "bool")
            {
                erros.Add(new ErroSemantico($"Condição do 'if' deve ser do tipo 'bool', mas foi encontrado '{tipoCondicao}'", node.Linha, tabelaSimbolos.EscopoAtual));
            }

            // Visitar nós filhos
            node.Condicao.Aceitar(this);

            if (node.ComandoEntao != null)
            {
                node.ComandoEntao.Aceitar(this);
            }

            if (node.ComandoSenao != null)
            {
                node.ComandoSenao.Aceitar(this);
            }
        }

        public void Visitar(ComandoWhileNode node)
        {
            // Verificar tipo da condição
            string tipoCondicao = ObterTipoExpressao(node.Condicao);
            if (tipoCondicao != null && tipoCondicao != "bool")
            {
                erros.Add(new ErroSemantico($"Condição do 'while' deve ser do tipo 'bool', mas foi encontrado '{tipoCondicao}'", node.Linha, tabelaSimbolos.EscopoAtual));
            }

            // Marcar que estamos dentro de um loop
            bool dentroDeLoopAnterior = dentroDeLoop;
            dentroDeLoop = true;

            try
            {
                // Visitar nós filhos
                node.Condicao.Aceitar(this);
                if (node.Comando != null)
                {
                    node.Comando.Aceitar(this);
                }
            }
            finally
            {
                dentroDeLoop = dentroDeLoopAnterior;
            }
        }

        public void Visitar(ComandoForNode node)
        {
            // Criar novo escopo para o for
            tabelaSimbolos.EntrarEscopo($"{tabelaSimbolos.EscopoAtual}.for{node.Linha}");

            bool dentroDeLoopAnterior = dentroDeLoop;
            dentroDeLoop = true;

            try
            {
                // Visitar inicialização
                if (node.Inicializacao != null)
                {
                    node.Inicializacao.Aceitar(this);
                }

                // Verificar condição
                if (node.Condicao != null)
                {
                    string tipoCondicao = ObterTipoExpressao(node.Condicao);
                    if (tipoCondicao != null && tipoCondicao != "bool")
                    {
                        erros.Add(new ErroSemantico($"Condição do 'for' deve ser do tipo 'bool', mas foi encontrado '{tipoCondicao}'", node.Linha, tabelaSimbolos.EscopoAtual));
                    }
                    node.Condicao.Aceitar(this);
                }

                // Visitar incremento
                if (node.Incremento != null)
                {
                    node.Incremento.Aceitar(this);
                }

                // Visitar comando
                if (node.Comando != null)
                {
                    node.Comando.Aceitar(this);
                }
            }
            finally
            {
                dentroDeLoop = dentroDeLoopAnterior;
                tabelaSimbolos.SairEscopo();
            }
        }

        public void Visitar(ComandoDoWhileNode node)
        {
            bool dentroDeLoopAnterior = dentroDeLoop;
            dentroDeLoop = true;

            try
            {
                // Visitar bloco
                if (node.Bloco != null)
                {
                    node.Bloco.Aceitar(this);
                }

                // Verificar condição
                if (node.Condicao != null)
                {
                    string tipoCondicao = ObterTipoExpressao(node.Condicao);
                    if (tipoCondicao != null && tipoCondicao != "bool")
                    {
                        erros.Add(new ErroSemantico($"Condição do 'do-while' deve ser do tipo 'bool', mas foi encontrado '{tipoCondicao}'", node.Linha, tabelaSimbolos.EscopoAtual));
                    }
                    node.Condicao.Aceitar(this);
                }
            }
            finally
            {
                dentroDeLoop = dentroDeLoopAnterior;
            }
        }

        public void Visitar(ComandoForeachNode node)
        {
            // Verificar se tipo da variável é válido
            if (!TipoValido(node.TipoVariavel))
            {
                erros.Add(new ErroSemantico($"Tipo '{node.TipoVariavel}' inválido para variável do foreach", node.Linha, tabelaSimbolos.EscopoAtual));
            }

            // Criar novo escopo para o foreach
            tabelaSimbolos.EntrarEscopo($"{tabelaSimbolos.EscopoAtual}.foreach{node.Linha}");

            bool dentroDeLoopAnterior = dentroDeLoop;
            dentroDeLoop = true;

            try
            {
                // Adicionar variável do foreach ao escopo
                var simboloVariavel = new Simbolo
                {
                    Nome = node.NomeVariavel,
                    Tipo = TipoSimbolo.Variavel,
                    TipoDado = node.TipoVariavel,
                    Linha = node.Linha,
                    Escopo = tabelaSimbolos.EscopoAtual,
                    Inicializada = true // Variável do foreach é sempre inicializada
                };
                tabelaSimbolos.AdicionarSimbolo(simboloVariavel);

                // Visitar coleção
                if (node.Colecao != null)
                {
                    node.Colecao.Aceitar(this);
                }

                // Visitar comando
                if (node.Comando != null)
                {
                    node.Comando.Aceitar(this);
                }
            }
            finally
            {
                dentroDeLoop = dentroDeLoopAnterior;
                tabelaSimbolos.SairEscopo();
            }
        }

        public void Visitar(ComandoSwitchNode node)
        {
            // Verificar expressão do switch
            string tipoExpressao = null;
            if (node.Expressao != null)
            {
                tipoExpressao = ObterTipoExpressao(node.Expressao);
                node.Expressao.Aceitar(this);
            }

            // Verificar cases
            foreach (var caseNode in node.Cases)
            {
                if (caseNode.Valor != null)
                {
                    string tipoCase = ObterTipoExpressao(caseNode.Valor);
                    if (tipoExpressao != null && tipoCase != null && !TiposCompativeis(tipoExpressao, tipoCase))
                    {
                        erros.Add(new ErroSemantico($"Tipo do case '{tipoCase}' incompatível com tipo da expressão switch '{tipoExpressao}'", caseNode.Linha, tabelaSimbolos.EscopoAtual));
                    }
                    caseNode.Valor.Aceitar(this);
                }

                // Visitar comandos do case
                foreach (var comando in caseNode.Comandos)
                {
                    comando.Aceitar(this);
                }
            }

            // Visitar comandos do default
            foreach (var comando in node.DefaultComandos)
            {
                comando.Aceitar(this);
            }
        }

        public void Visitar(ComandoReturnNode node)
        {
            if (metodoAtual == null)
            {
                erros.Add(new ErroSemantico("'return' só pode ser usado dentro de métodos", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Buscar tipo de retorno do método atual
            var simboloMetodo = tabelaSimbolos.BuscarSimbolo(metodoAtual);
            if (simboloMetodo == null)
            {
                return; // Erro já reportado antes
            }

            string tipoRetornoEsperado = simboloMetodo.TipoDado;

            if (node.Expressao == null)
            {
                // Return sem expressão - só válido para void
                if (tipoRetornoEsperado != "void")
                {
                    erros.Add(new ErroSemantico($"Método '{metodoAtual}' deve retornar um valor do tipo '{tipoRetornoEsperado}'", node.Linha, tabelaSimbolos.EscopoAtual));
                }
            }
            else
            {
                // Return com expressão
                if (tipoRetornoEsperado == "void")
                {
                    erros.Add(new ErroSemantico($"Método '{metodoAtual}' não deve retornar um valor", node.Linha, tabelaSimbolos.EscopoAtual));
                }
                else
                {
                    string tipoExpressao = ObterTipoExpressao(node.Expressao);
                    if (tipoExpressao != null && !TiposCompativeis(tipoRetornoEsperado, tipoExpressao))
                    {
                        erros.Add(new ErroSemantico($"Não é possível converter '{tipoExpressao}' para '{tipoRetornoEsperado}'", node.Linha, tabelaSimbolos.EscopoAtual));
                    }
                }

                node.Expressao.Aceitar(this);
            }
        }

        public void Visitar(ComandoBreakNode node)
        {
            if (!dentroDeLoop)
            {
                erros.Add(new ErroSemantico("'break' só pode ser usado dentro de loops ou switch", node.Linha, tabelaSimbolos.EscopoAtual));
            }
        }

        public void Visitar(BlocoComandosNode node)
        {
            // Criar novo escopo para o bloco
            tabelaSimbolos.EntrarEscopo($"{tabelaSimbolos.EscopoAtual}.bloco{node.Linha}");

            try
            {
                foreach (var comando in node.Comandos)
                {
                    comando.Aceitar(this);
                }
            }
            finally
            {
                tabelaSimbolos.SairEscopo();
            }
        }

        public void Visitar(AtribuicaoNode node)
        {
            // Verificar se variável foi declarada
            var simbolo = tabelaSimbolos.BuscarSimbolo(node.NomeVariavel);
            if (simbolo == null)
            {
                erros.Add(new ErroSemantico($"Variável '{node.NomeVariavel}' não foi declarada", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Se for acesso a array, verificar se é válido
            if (node.EhArrayAccess)
            {
                if (simbolo.Dimensao != DimensaoTipo.Composta)
                {
                    erros.Add(new ErroSemantico($"'{node.NomeVariavel}' não é um array", node.Linha, tabelaSimbolos.EscopoAtual));
                    return;
                }

                if (node.IndiceArray != null)
                {
                    string tipoIndice = ObterTipoExpressao(node.IndiceArray);
                    if (tipoIndice != null && tipoIndice != "int")
                    {
                        erros.Add(new ErroSemantico($"Índice de array deve ser do tipo 'int', mas foi encontrado '{tipoIndice}'", node.Linha, tabelaSimbolos.EscopoAtual));
                    }
                    node.IndiceArray.Aceitar(this);
                }
            }

            // Determinar tipo de destino para verificação
            string tipoDestino;
            if (node.EhArrayAccess)
            {
                // Para array[i] = valor, tipo destino é o tipo base do array
                tipoDestino = simbolo.TipoDado; // tipo base (int, float, etc.)
            }
            else
            {
                // Para variavel = valor, tipo destino é o tipo completo
                tipoDestino = ObterTipoCompletoSimbolo(simbolo);
            }

            // Verificar compatibilidade de tipos
            string tipoExpressao = ObterTipoExpressao(node.Expressao);
            if (tipoExpressao != null)
            {
                var infoDestino = AnalisarTipoEDimensao(tipoDestino);
                if (!TiposEDimensoesCompativeis(tipoDestino, tipoExpressao, infoDestino.Dimensao))
                {
                    erros.Add(new ErroSemantico($"Não é possível converter '{tipoExpressao}' para '{tipoDestino}'", node.Linha, tabelaSimbolos.EscopoAtual));
                }
            }

            // Marcar variável como inicializada
            simbolo.Inicializada = true;

            // Visitar expressão
            node.Expressao.Aceitar(this);
        }

        public void Visitar(ChamadaMetodoNode node)
        {
            // Se for chamada com objeto (obj.metodo())
            if (!string.IsNullOrEmpty(node.NomeObjeto))
            {
                // Verificar se objeto existe
                var simboloObjeto = tabelaSimbolos.BuscarSimbolo(node.NomeObjeto);
                if (simboloObjeto == null)
                {
                    erros.Add(new ErroSemantico($"Objeto '{node.NomeObjeto}' não foi declarado", node.Linha, tabelaSimbolos.EscopoAtual));
                    return;
                }

                // Para simplificar, vamos permitir só Console.WriteLine por agora
                if (node.NomeObjeto == "Console" && node.NomeMetodo == "WriteLine")
                {
                    // Verificar argumentos do WriteLine
                    foreach (var argumento in node.Argumentos)
                    {
                        argumento.Aceitar(this);
                    }
                    return;
                }
            }

            // Buscar método na classe atual ou escopos superiores
            var simboloMetodo = tabelaSimbolos.BuscarSimbolo(node.NomeMetodo);
            if (simboloMetodo == null || simboloMetodo.Tipo != TipoSimbolo.Metodo)
            {
                erros.Add(new ErroSemantico($"Método '{node.NomeMetodo}' não foi declarado", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Verificar número de argumentos (simplificado)
            if (simboloMetodo.Parametros != null && node.Argumentos.Count != simboloMetodo.Parametros.Count)
            {
                erros.Add(new ErroSemantico($"Método '{node.NomeMetodo}' espera {simboloMetodo.Parametros.Count} argumentos, mas {node.Argumentos.Count} foram fornecidos", node.Linha, tabelaSimbolos.EscopoAtual));
            }

            // Visitar argumentos
            foreach (var argumento in node.Argumentos)
            {
                argumento.Aceitar(this);
            }
        }

        public void Visitar(ExpressaoBinariaNode node)
        {
            // Visitar operandos
            node.Esquerda.Aceitar(this);
            node.Direita.Aceitar(this);

            // Verificar compatibilidade de tipos
            string tipoEsquerda = ObterTipoExpressao(node.Esquerda);
            string tipoDireita = ObterTipoExpressao(node.Direita);

            if (tipoEsquerda != null && tipoDireita != null)
            {
                if (!OperacaoValida(node.Operador, tipoEsquerda, tipoDireita))
                {
                    erros.Add(new ErroSemantico($"Operação '{node.Operador}' não é válida entre '{tipoEsquerda}' e '{tipoDireita}'", node.Linha, tabelaSimbolos.EscopoAtual));
                }
            }
        }

        public void Visitar(ExpressaoUnariaNode node)
        {
            node.Operando.Aceitar(this);

            string tipoOperando = ObterTipoExpressao(node.Operando);
            if (tipoOperando != null)
            {
                if (!OperacaoUnariaValida(node.Operador, tipoOperando))
                {
                    erros.Add(new ErroSemantico($"Operação unária '{node.Operador}' não é válida para tipo '{tipoOperando}'", node.Linha, tabelaSimbolos.EscopoAtual));
                }
            }
        }

        public void Visitar(LiteralNode node)
        {
            // Literais são sempre válidos, só visitamos para completude
        }

        public void Visitar(IdentificadorNode node)
        {
            // Verificar se identificador foi declarado
            var simbolo = tabelaSimbolos.BuscarSimbolo(node.Nome);
            if (simbolo == null)
            {
                erros.Add(new ErroSemantico($"Identificador '{node.Nome}' não foi declarado", node.Linha, tabelaSimbolos.EscopoAtual));
                return;
            }

            // Verificar se variável foi inicializada (apenas para variáveis locais)
            if (simbolo.Tipo == TipoSimbolo.Variavel && !simbolo.Inicializada)
            {
                erros.Add(new ErroSemantico($"Variável '{node.Nome}' está sendo usada sem ter sido inicializada", node.Linha, tabelaSimbolos.EscopoAtual));
            }

            // Se for acesso a array, verificar se é válido
            if (node.EhArrayAccess)
            {
                if (simbolo.Dimensao != DimensaoTipo.Composta)
                {
                    erros.Add(new ErroSemantico($"'{node.Nome}' não é um array", node.Linha, tabelaSimbolos.EscopoAtual));
                    return;
                }

                if (node.IndiceArray != null)
                {
                    string tipoIndice = ObterTipoExpressao(node.IndiceArray);
                    if (tipoIndice != null && tipoIndice != "int")
                    {
                        erros.Add(new ErroSemantico($"Índice de array deve ser do tipo 'int', mas foi encontrado '{tipoIndice}'", node.Linha, tabelaSimbolos.EscopoAtual));
                    }
                    node.IndiceArray.Aceitar(this);
                }
            }
            else
            {
                // Acesso normal à variável - verificar se não está tentando usar array como valor simples
                if (simbolo.Dimensao == DimensaoTipo.Composta)
                {
                    erros.Add(new ErroSemantico($"'{node.Nome}' é um array. Use '{node.Nome}[índice]' para acessar elementos", node.Linha, tabelaSimbolos.EscopoAtual));
                }
            }
        }

        // ========================================
        // MÉTODOS AUXILIARES PARA TIPOS E DIMENSÕES
        // ========================================

        private (string TipoBase, string TipoCompleto, DimensaoTipo Dimensao, int TamanhoArray) AnalisarTipoEDimensao(string tipoDeclarado)
        {
            // Verificar se é array: int[], float[], string[]
            if (tipoDeclarado.EndsWith("[]"))
            {
                string tipoBase = tipoDeclarado.Substring(0, tipoDeclarado.Length - 2);
                return (tipoBase, tipoDeclarado, DimensaoTipo.Composta, -1); // -1 = tamanho não especificado
            }

            // Verificar se é array com tamanho: int[5], float[10]
            if (tipoDeclarado.Contains("[") && tipoDeclarado.EndsWith("]"))
            {
                int indiceBracket = tipoDeclarado.IndexOf('[');
                string tipoBase = tipoDeclarado.Substring(0, indiceBracket);
                string tamanhoStr = tipoDeclarado.Substring(indiceBracket + 1, tipoDeclarado.Length - indiceBracket - 2);

                if (int.TryParse(tamanhoStr, out int tamanho) && tamanho > 0)
                {
                    return (tipoBase, tipoDeclarado, DimensaoTipo.Composta, tamanho);
                }
                else
                {
                    // Tamanho inválido, tratar como erro
                    return (tipoBase, tipoDeclarado, DimensaoTipo.Composta, 0);
                }
            }

            // Tipo simples
            return (tipoDeclarado, tipoDeclarado, DimensaoTipo.Simples, 0);
        }

        private bool TiposEDimensoesCompativeis(string tipoDestino, string tipoOrigem, DimensaoTipo dimensaoDestino)
        {
            var infoDestino = AnalisarTipoEDimensao(tipoDestino);
            var infoOrigem = AnalisarTipoEDimensao(tipoOrigem);

            // Dimensões devem ser compatíveis
            if (infoDestino.Dimensao != infoOrigem.Dimensao)
            {
                return false;
            }

            // Para tipos simples, usar verificação normal
            if (infoDestino.Dimensao == DimensaoTipo.Simples)
            {
                return TiposCompativeis(infoDestino.TipoBase, infoOrigem.TipoBase);
            }

            // Para arrays, tipos base devem ser compatíveis
            if (infoDestino.Dimensao == DimensaoTipo.Composta)
            {
                return TiposCompativeis(infoDestino.TipoBase, infoOrigem.TipoBase);
            }

            return false;
        }

        private string ObterTipoCompletoSimbolo(Simbolo simbolo)
        {
            if (simbolo.Dimensao == DimensaoTipo.Simples)
            {
                return simbolo.TipoDado;
            }
            else
            {
                if (simbolo.TamanhoArray > 0)
                {
                    return $"{simbolo.TipoDado}[{simbolo.TamanhoArray}]";
                }
                else
                {
                    return $"{simbolo.TipoDado}[]";
                }
            }
        }

        private bool EhTipoArray(string tipo)
        {
            return tipo.Contains("[") && tipo.Contains("]");
        }

        private bool EhAcessoArrayValido(Simbolo simbolo, ASTNode indice)
        {
            // Só pode acessar array se símbolo for de dimensão composta
            if (simbolo.Dimensao != DimensaoTipo.Composta)
            {
                return false;
            }

            // Índice deve ser do tipo int
            string tipoIndice = ObterTipoExpressao(indice);
            return tipoIndice == "int";
        }

        private string ObterTipoExpressao(ASTNode node)
        {
            switch (node)
            {
                case LiteralNode literal:
                    return literal.Tipo;

                case IdentificadorNode identificador:
                    var simbolo = tabelaSimbolos.BuscarSimbolo(identificador.Nome);
                    if (simbolo == null) return null;

                    // Se for acesso a array, retorna tipo base
                    if (identificador.EhArrayAccess)
                    {
                        return simbolo.TipoDado; // tipo base (int, float, etc.)
                    }
                    else
                    {
                        // Se for array sem acesso, retorna tipo completo
                        return ObterTipoCompletoSimbolo(simbolo);
                    }

                case ExpressaoBinariaNode binaria:
                    return ObterTipoOperacaoBinaria(binaria.Operador, binaria.Esquerda, binaria.Direita);

                case ExpressaoUnariaNode unaria:
                    return ObterTipoOperacaoUnaria(unaria.Operador, unaria.Operando);

                case ChamadaMetodoNode chamada:
                    var simboloMetodo = tabelaSimbolos.BuscarSimbolo(chamada.NomeMetodo);
                    return simboloMetodo?.TipoDado;

                default:
                    return null;
            }
        }

        private string ObterTipoOperacaoBinaria(string operador, ASTNode esquerda, ASTNode direita)
        {
            string tipoEsquerda = ObterTipoExpressao(esquerda);
            string tipoDireita = ObterTipoExpressao(direita);

            if (tipoEsquerda == null || tipoDireita == null)
                return null;

            // Operadores de comparação sempre retornam bool
            if (operador == "==" || operador == "!=" || operador == "<" || operador == ">" ||
                operador == "<=" || operador == ">=" || operador == "&&" || operador == "||")
            {
                return "bool";
            }

            // Operadores aritméticos
            if (operador == "+" || operador == "-" || operador == "*" || operador == "/" || operador == "%")
            {
                // Se algum dos tipos for float, resultado é float
                if (tipoEsquerda == "float" || tipoDireita == "float")
                    return "float";

                // Se ambos forem int, resultado é int
                if (tipoEsquerda == "int" && tipoDireita == "int")
                    return "int";

                // Concatenação de strings
                if (operador == "+" && (tipoEsquerda == "string" || tipoDireita == "string"))
                    return "string";
            }

            return tipoEsquerda; // Default
        }

        private string ObterTipoOperacaoUnaria(string operador, ASTNode operando)
        {
            string tipoOperando = ObterTipoExpressao(operando);

            if (tipoOperando == null)
                return null;

            switch (operador)
            {
                case "!":
                    return "bool";
                case "-":
                case "+":
                case "++":
                case "--":
                    return tipoOperando;
                default:
                    return tipoOperando;
            }
        }

        private bool TipoValido(string tipo)
        {
            return tipo == "int" || tipo == "float" || tipo == "string" || tipo == "bool" || tipo == "void";
        }

        private bool TiposCompativeis(string tipoDestino, string tipoOrigem)
        {
            if (tipoDestino == tipoOrigem)
                return true;

            // Conversões implícitas permitidas
            if (tipoDestino == "float" && tipoOrigem == "int")
                return true;

            if (tipoDestino == "string" && (tipoOrigem == "int" || tipoOrigem == "float" || tipoOrigem == "bool"))
                return true;

            return false;
        }

        private bool OperacaoValida(string operador, string tipoEsquerda, string tipoDireita)
        {
            // Operadores de comparação
            if (operador == "==" || operador == "!=")
                return TiposCompativeis(tipoEsquerda, tipoDireita) || TiposCompativeis(tipoDireita, tipoEsquerda);

            if (operador == "<" || operador == ">" || operador == "<=" || operador == ">=")
                return (tipoEsquerda == "int" || tipoEsquerda == "float") && (tipoDireita == "int" || tipoDireita == "float");

            // Operadores lógicos
            if (operador == "&&" || operador == "||")
                return tipoEsquerda == "bool" && tipoDireita == "bool";

            // Operadores aritméticos
            if (operador == "+" || operador == "-" || operador == "*" || operador == "/" || operador == "%")
            {
                // Números
                if ((tipoEsquerda == "int" || tipoEsquerda == "float") && (tipoDireita == "int" || tipoDireita == "float"))
                    return true;

                // Concatenação de strings (apenas +)
                if (operador == "+" && (tipoEsquerda == "string" || tipoDireita == "string"))
                    return true;
            }

            return false;
        }

        private bool OperacaoUnariaValida(string operador, string tipo)
        {
            switch (operador)
            {
                case "!":
                    return tipo == "bool";
                case "-":
                case "+":
                    return tipo == "int" || tipo == "float";
                case "++":
                case "--":
                    return tipo == "int" || tipo == "float";
                default:
                    return false;
            }
        }
    }

    // ========================================
    // CLASSE PARA GERENCIAR TABELA DE SÍMBOLOS
    // ========================================

    public class TabelaSimbolos
    {
        private Stack<Dictionary<string, Simbolo>> escopos;
        private Stack<string> nomesEscopos;

        public string EscopoAtual => nomesEscopos.Count > 0 ? nomesEscopos.Peek() : "global";

        public TabelaSimbolos()
        {
            escopos = new Stack<Dictionary<string, Simbolo>>();
            nomesEscopos = new Stack<string>();

            // Escopo global inicial
            escopos.Push(new Dictionary<string, Simbolo>());
            nomesEscopos.Push("global");
        }

        public void EntrarEscopo(string nomeEscopo)
        {
            escopos.Push(new Dictionary<string, Simbolo>());
            nomesEscopos.Push(nomeEscopo);
        }

        public void SairEscopo()
        {
            if (escopos.Count > 1) // Nunca remover escopo global
            {
                escopos.Pop();
                nomesEscopos.Pop();
            }
        }

        public void AdicionarSimbolo(Simbolo simbolo)
        {
            if (escopos.Count > 0)
            {
                var escopoAtual = escopos.Peek();
                escopoAtual[simbolo.Nome] = simbolo;
            }
        }

        public Simbolo BuscarSimbolo(string nome)
        {
            // Buscar do escopo mais interno para o mais externo
            foreach (var escopo in escopos)
            {
                if (escopo.ContainsKey(nome))
                {
                    return escopo[nome];
                }
            }
            return null;
        }

        public bool ExisteNoEscopoAtual(string nome)
        {
            if (escopos.Count > 0)
            {
                return escopos.Peek().ContainsKey(nome);
            }
            return false;
        }

        public List<Simbolo> ObterSimbolosEscopoAtual()
        {
            if (escopos.Count > 0)
            {
                return escopos.Peek().Values.ToList();
            }
            return new List<Simbolo>();
        }
    }

    // ========================================
    // CLASSE PARA REPRESENTAR SÍMBOLOS
    // ========================================

    public class Simbolo
    {
        public string Nome { get; set; }
        public TipoSimbolo Tipo { get; set; }
        public string TipoDado { get; set; }
        public int Linha { get; set; }
        public string Escopo { get; set; }
        public bool Inicializada { get; set; } = false;

        // NOVA PROPRIEDADE: Dimensão para auxiliar análise semântica
        public DimensaoTipo Dimensao { get; set; } = DimensaoTipo.Simples;
        public int TamanhoArray { get; set; } = 0; // Para arrays com tamanho definido

        // Para métodos
        public List<string> Parametros { get; set; } = new List<string>();
        public string ModificadorAcesso { get; set; } = "private";

        // Para classes
        public string TipoBase { get; set; }

        public override string ToString()
        {
            string dimensaoStr = Dimensao == DimensaoTipo.Composta ? $"[{TamanhoArray}]" : "";
            return $"{Tipo} {Nome} : {TipoDado}{dimensaoStr} (Linha {Linha}, Escopo: {Escopo}, Dimensão: {Dimensao})";
        }
    }

    // ========================================
    // ENUM PARA DIMENSÃO DE TIPOS
    // ========================================

    public enum DimensaoTipo
    {
        Simples,   // int, float, string, bool - tipos primitivos
        Composta   // int[], float[], string[] - arrays/coleções
    }

    // ========================================
    // CLASSE PARA ERROS SEMÂNTICOS
    // ========================================

    public class ErroSemantico
    {
        public string Mensagem { get; }
        public int Linha { get; }
        public string Escopo { get; }

        public ErroSemantico(string mensagem, int linha, string escopo)
        {
            Mensagem = mensagem;
            Linha = linha;
            Escopo = escopo;
        }

        public override string ToString()
        {
            return $"Erro Semântico [Linha {Linha}] ({Escopo}): {Mensagem}";
        }
    }
}