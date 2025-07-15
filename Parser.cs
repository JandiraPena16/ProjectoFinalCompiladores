using System;
using System.Collections.Generic;
using JPenaCompiler;

namespace JPenaCompiler
{
    public class Parser
    {
        private List<Token> tokens;
        private int pos = 0;

        public List<Erro> Erros { get; private set; } = new List<Erro>();
        public ProgramaNode AST { get; private set; }

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token Proximo()
        {
            if (pos < tokens.Count)
                return tokens[pos++];
            return new Token(TipoToken.TokenEOF, "", -1);
        }

        private Token VerificarProximo()
        {
            if (pos < tokens.Count)
                return tokens[pos];
            return new Token(TipoToken.TokenEOF, "", -1);
        }

        private bool Match(TipoToken tipo)
        {
            if (VerificarProximo().Tipo == tipo)
            {
                Proximo();
                return true;
            }
            return false;
        }

        private Token Consumir(TipoToken tipo, string mensagemErro)
        {
            if (VerificarProximo().Tipo == tipo)
            {
                return Proximo();
            }
            else
            {
                Erros.Add(new Erro(mensagemErro, VerificarProximo().Linha));
                return new Token(tipo, "", VerificarProximo().Linha); // Token de erro
            }
        }

        // ========================================
        // MÉTODO PRINCIPAL PARA GERAR AST
        // ========================================

        public ProgramaNode AnalisarPrograma()
        {
            try
            {
                AST = new ProgramaNode { Linha = 1 };

                // Reconhecer todos os usings no início
                while (VerificarProximo().Tipo == TipoToken.TokenUSING)
                {
                    string usingNamespace = AnalisarUsing();
                    if (!string.IsNullOrEmpty(usingNamespace))
                    {
                        AST.Usings.Add(usingNamespace);
                    }
                }

                // Espera o namespace depois dos usings
                if (VerificarProximo().Tipo == TipoToken.TokenNAMESPACE)
                {
                    AST.Namespace = AnalisarNamespace();
                }
                else if (VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Erros.Add(new Erro("Esperado 'namespace' após os usings", VerificarProximo().Linha));
                }

                return AST;
            }
            catch (Exception ex)
            {
                Erros.Add(new Erro($"Erro crítico no parser: {ex.Message}", VerificarProximo().Linha));
                return AST ?? new ProgramaNode { Linha = 1 };
            }
        }

        // ========================================
        // ANÁLISE DE USING
        // ========================================

        private string AnalisarUsing()
        {
            Consumir(TipoToken.TokenUSING, "Esperado 'using'");
            var identificador = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após 'using'");

            string nomeCompleto = identificador.Lexema;

            while (Match(TipoToken.TokenDOT))
            {
                var parte = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após '.' em using");
                nomeCompleto += "." + parte.Lexema;
            }

            Consumir(TipoToken.TokenSEMICOLON, "Esperado ';' após declaração using");
            return nomeCompleto;
        }

        // ========================================
        // ANÁLISE DE NAMESPACE
        // ========================================

        private NamespaceNode AnalisarNamespace()
        {
            var tokenNamespace = Consumir(TipoToken.TokenNAMESPACE, "Esperado 'namespace'");
            var nomeNamespace = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após 'namespace'");

            string nomeCompleto = nomeNamespace.Lexema;

            while (Match(TipoToken.TokenDOT))
            {
                var parte = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após '.' em namespace");
                nomeCompleto += "." + parte.Lexema;
            }

            var namespaceNode = new NamespaceNode
            {
                Nome = nomeCompleto,
                Linha = tokenNamespace.Linha
            };

            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração do namespace");

            // Analisar conteúdo do namespace
            while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                if (VerificarProximo().Tipo == TipoToken.TokenCLASS)
                {
                    var classe = AnalisarClasse();
                    if (classe != null)
                        namespaceNode.Classes.Add(classe);
                }
                else
                {
                    // Pular tokens inválidos
                    Erros.Add(new Erro($"Declaração inválida no namespace: '{VerificarProximo().Lexema}'", VerificarProximo().Linha));
                    Proximo();
                }
            }

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar namespace");
            return namespaceNode;
        }

        // ========================================
        // ANÁLISE DE CLASSE
        // ========================================

        private ClasseNode AnalisarClasse()
        {
            var tokenClasse = Consumir(TipoToken.TokenCLASS, "Esperado 'class'");
            var nomeClasse = Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome da classe");

            var classeNode = new ClasseNode
            {
                Nome = nomeClasse.Lexema,
                ModificadorAcesso = "internal", // default
                Linha = tokenClasse.Linha
            };

            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração de classe");

            // Analisar membros da classe
            while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                try
                {
                    // Verificar modificadores de acesso
                    if (VerificarProximo().Tipo == TipoToken.TokenPUBLIC ||
                        VerificarProximo().Tipo == TipoToken.TokenPRIVATE ||
                        VerificarProximo().Tipo == TipoToken.TokenPROTECTED)
                    {
                        var metodo = AnalisarMetodo();
                        if (metodo != null)
                            classeNode.Metodos.Add(metodo);
                    }
                    // Declarações de campos/variáveis
                    else if (VerificarProximo().Tipo == TipoToken.TokenINT ||
                             VerificarProximo().Tipo == TipoToken.TokenFLOAT ||
                             VerificarProximo().Tipo == TipoToken.TokenSTRING ||
                             VerificarProximo().Tipo == TipoToken.TokenBOOL)
                    {
                        var campo = AnalisarDeclaracaoVariavel(true); // true = é campo
                        if (campo != null)
                            classeNode.Campos.Add(campo);
                    }
                    else
                    {
                        // Comandos soltos na classe (erro semântico, mas vamos processar)
                        Erros.Add(new Erro($"Comando '{VerificarProximo().Lexema}' não pode estar solto na classe", VerificarProximo().Linha));
                        PularAteProximaDeclaracao();
                    }
                }
                catch (Exception)
                {
                    PularAteProximaDeclaracao();
                }
            }

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' ao final da classe");
            return classeNode;
        }

        // ========================================
        // ANÁLISE DE MÉTODO
        // ========================================

        private MetodoNode AnalisarMetodo()
        {
            // Modificador de acesso
            var modificador = Proximo();
            string modificadorAcesso = modificador.Lexema;

            // Tipo de retorno
            var tipoRetorno = Consumir(TipoToken.TokenINT, "Esperado tipo de retorno");
            if (tipoRetorno.Tipo != TipoToken.TokenINT &&
                tipoRetorno.Tipo != TipoToken.TokenFLOAT &&
                tipoRetorno.Tipo != TipoToken.TokenSTRING &&
                tipoRetorno.Tipo != TipoToken.TokenBOOL &&
                tipoRetorno.Tipo != TipoToken.TokenVOID)
            {
                // Tentar outros tipos
                pos--; // Volta posição
                tipoRetorno = Proximo(); // Pega qualquer tipo
            }

            // Nome do método
            var nomeMetodo = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após tipo de retorno do método");

            var metodoNode = new MetodoNode
            {
                Nome = nomeMetodo.Lexema,
                TipoRetorno = tipoRetorno.Lexema,
                ModificadorAcesso = modificadorAcesso,
                Linha = modificador.Linha
            };

            // Parâmetros
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após nome do método");

            if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
            {
                do
                {
                    var parametro = AnalisarParametro();
                    if (parametro != null)
                        metodoNode.Parametros.Add(parametro);
                } while (Match(TipoToken.TokenCOMMA));
            }

            Consumir(TipoToken.TokenRPAREN, "Esperado ')' após parâmetros do método");

            // Corpo do método
            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração de método");
            metodoNode.Corpo = AnalisarBlocoComandos();
            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar bloco do método");

            return metodoNode;
        }

        // ========================================
        // ANÁLISE DE PARÂMETRO
        // ========================================

        private ParametroNode AnalisarParametro()
        {
            var tipo = Proximo();
            if (tipo.Tipo != TipoToken.TokenINT &&
                tipo.Tipo != TipoToken.TokenFLOAT &&
                tipo.Tipo != TipoToken.TokenSTRING &&
                tipo.Tipo != TipoToken.TokenBOOL)
            {
                Erros.Add(new Erro("Tipo de parâmetro inválido", tipo.Linha));
                return null;
            }

            var nome = Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome do parâmetro");

            return new ParametroNode
            {
                Tipo = tipo.Lexema,
                Nome = nome.Lexema,
                Linha = tipo.Linha
            };
        }

        // ========================================
        // ANÁLISE DE DECLARAÇÃO DE VARIÁVEL
        // ========================================

        private DeclaracaoVariavelNode AnalisarDeclaracaoVariavel(bool ehCampo = false)
        {
            var tipo = Proximo();
            var nome = Consumir(TipoToken.TokenIDENTIFIER, "Identificador esperado");

            var declaracao = new DeclaracaoVariavelNode
            {
                Tipo = tipo.Lexema,
                Nome = nome.Lexema,
                EhCampo = ehCampo,
                Linha = tipo.Linha
            };

            // Inicializador opcional
            if (Match(TipoToken.TokenASSIGN))
            {
                declaracao.Inicializador = AnalisarExpressao();
            }

            Consumir(TipoToken.TokenSEMICOLON, "; esperado após declaração");
            return declaracao;
        }

        // ========================================
        // ANÁLISE DE BLOCO DE COMANDOS
        // ========================================

        private BlocoComandosNode AnalisarBlocoComandos()
        {
            var bloco = new BlocoComandosNode { Linha = VerificarProximo().Linha };

            while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                var comando = AnalisarComando();
                if (comando != null)
                    bloco.Comandos.Add(comando);
            }

            return bloco;
        }

        // ========================================
        // ANÁLISE DE COMANDO
        // ========================================

        private ASTNode AnalisarComando()
        {
            try
            {
                var tokenAtual = VerificarProximo();

                switch (tokenAtual.Tipo)
                {
                    case TipoToken.TokenIF:
                        return AnalisarComandoIf();

                    case TipoToken.TokenWHILE:
                        return AnalisarComandoWhile();

                    case TipoToken.TokenFOR:
                        return AnalisarComandoFor();

                    case TipoToken.TokenDO:
                        return AnalisarComandoDoWhile();

                    case TipoToken.TokenFOREACH:
                        return AnalisarComandoForeach();

                    case TipoToken.TokenSWITCH:
                        return AnalisarComandoSwitch();

                    case TipoToken.TokenRETURN:
                        return AnalisarComandoReturn();

                    case TipoToken.TokenBREAK:
                        Proximo();
                        Consumir(TipoToken.TokenSEMICOLON, "; esperado após 'break'");
                        return new ComandoBreakNode { Linha = tokenAtual.Linha };

                    case TipoToken.TokenINT:
                    case TipoToken.TokenFLOAT:
                    case TipoToken.TokenSTRING:
                    case TipoToken.TokenBOOL:
                        return AnalisarDeclaracaoVariavel();

                    case TipoToken.TokenIDENTIFIER:
                        return AnalisarAtribuicaoOuChamada();

                    case TipoToken.TokenLBRACE:
                        Proximo(); // Consome '{'
                        var bloco = AnalisarBlocoComandos();
                        Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar o bloco");
                        return bloco;

                    default:
                        Erros.Add(new Erro("Comando inválido", tokenAtual.Linha));
                        Proximo(); // Pula token inválido
                        return null;
                }
            }
            catch (Exception)
            {
                PularAteProximaDeclaracao();
                return null;
            }
        }

        // ========================================
        // ANÁLISE DE COMANDO IF
        // ========================================

        private ComandoIfNode AnalisarComandoIf()
        {
            var tokenIf = Proximo(); // Consome 'if'
            Consumir(TipoToken.TokenLPAREN, "Esperado (");
            var condicao = AnalisarExpressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado )");
            var comandoEntao = AnalisarComando();

            var ifNode = new ComandoIfNode
            {
                Condicao = condicao,
                ComandoEntao = comandoEntao,
                Linha = tokenIf.Linha
            };

            if (Match(TipoToken.TokenELSE))
            {
                ifNode.ComandoSenao = AnalisarComando();
            }

            return ifNode;
        }

        // ========================================
        // ANÁLISE DE COMANDO WHILE
        // ========================================

        private ComandoWhileNode AnalisarComandoWhile()
        {
            var tokenWhile = Proximo(); // Consome 'while'
            Consumir(TipoToken.TokenLPAREN, "Esperado (");
            var condicao = AnalisarExpressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado )");
            var comando = AnalisarComando();

            return new ComandoWhileNode
            {
                Condicao = condicao,
                Comando = comando,
                Linha = tokenWhile.Linha
            };
        }

        // ========================================
        // ANÁLISE DE COMANDO FOR
        // ========================================

        private ComandoForNode AnalisarComandoFor()
        {
            var tokenFor = Proximo(); // Consome 'for'
            Consumir(TipoToken.TokenLPAREN, "Esperado (");

            // Inicialização
            ASTNode inicializacao = null;
            if (VerificarProximo().Tipo == TipoToken.TokenINT ||
                VerificarProximo().Tipo == TipoToken.TokenFLOAT ||
                VerificarProximo().Tipo == TipoToken.TokenSTRING ||
                VerificarProximo().Tipo == TipoToken.TokenBOOL)
            {
                inicializacao = AnalisarDeclaracaoVariavel();
            }
            else if (VerificarProximo().Tipo == TipoToken.TokenIDENTIFIER)
            {
                inicializacao = AnalisarAtribuicaoOuChamada();
                Consumir(TipoToken.TokenSEMICOLON, "; esperado após inicialização do for");
            }

            // Condição
            var condicao = AnalisarExpressao();
            Consumir(TipoToken.TokenSEMICOLON, "; esperado após condição do for");

            // Incremento
            ASTNode incremento = null;
            if (VerificarProximo().Tipo == TipoToken.TokenIDENTIFIER)
            {
                incremento = AnalisarAtribuicaoOuChamada();
            }

            Consumir(TipoToken.TokenRPAREN, ") esperado após cláusulas do for");
            var comando = AnalisarComando();

            return new ComandoForNode
            {
                Inicializacao = inicializacao,
                Condicao = condicao,
                Incremento = incremento,
                Comando = comando,
                Linha = tokenFor.Linha
            };
        }

        // ========================================
        // ANÁLISE DE COMANDO DO-WHILE
        // ========================================

        private ComandoDoWhileNode AnalisarComandoDoWhile()
        {
            var tokenDo = Proximo(); // Consome 'do'
            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após do");

            var bloco = AnalisarBlocoComandos();

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar o bloco do-while");
            Consumir(TipoToken.TokenWHILE, "Esperado 'while' após bloco do-while");
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'while'");
            var condicao = AnalisarExpressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado ')' após condição");
            Consumir(TipoToken.TokenSEMICOLON, "Esperado ';' após ')' do while");

            return new ComandoDoWhileNode
            {
                Bloco = bloco,
                Condicao = condicao,
                Linha = tokenDo.Linha
            };
        }

        // ========================================
        // ANÁLISE DE COMANDO FOREACH
        // ========================================

        private ComandoForeachNode AnalisarComandoForeach()
        {
            var tokenForeach = Proximo(); // Consome 'foreach'
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'foreach'");

            var tipo = Proximo();
            var nome = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após tipo no foreach");
            Consumir(TipoToken.TokenIN, "Esperado 'in' após identificador no foreach");
            var colecao = AnalisarExpressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado ')' após cláusula do foreach");

            var comando = AnalisarComando();

            return new ComandoForeachNode
            {
                TipoVariavel = tipo.Lexema,
                NomeVariavel = nome.Lexema,
                Colecao = colecao,
                Comando = comando,
                Linha = tokenForeach.Linha
            };
        }

        // ========================================
        // ANÁLISE DE COMANDO SWITCH
        // ========================================

        private ComandoSwitchNode AnalisarComandoSwitch()
        {
            var tokenSwitch = Proximo(); // Consome 'switch'
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'switch'");
            var expressao = AnalisarExpressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado ')' após expressão do switch");
            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após switch");

            var switchNode = new ComandoSwitchNode
            {
                Expressao = expressao,
                Linha = tokenSwitch.Linha
            };

            while (VerificarProximo().Tipo == TipoToken.TokenCASE || VerificarProximo().Tipo == TipoToken.TokenDEFAULT)
            {
                if (VerificarProximo().Tipo == TipoToken.TokenCASE)
                {
                    Proximo(); // Consome 'case'
                    var valor = AnalisarExpressao();
                    Consumir(TipoToken.TokenCOLON, "Esperado ':' após valor do case");

                    var caseNode = new CaseSwitchNode
                    {
                        Valor = valor,
                        Linha = VerificarProximo().Linha
                    };

                    // Comandos do case
                    while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                           VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                           VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                           VerificarProximo().Tipo != TipoToken.TokenEOF)
                    {
                        var comandoCase = AnalisarComando();
                        if (comandoCase != null)
                            caseNode.Comandos.Add(comandoCase);
                    }

                    switchNode.Cases.Add(caseNode);
                }
                else if (VerificarProximo().Tipo == TipoToken.TokenDEFAULT)
                {
                    Proximo(); // Consome 'default'
                    Consumir(TipoToken.TokenCOLON, "Esperado ':' após 'default'");

                    // Comandos do default
                    while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                           VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                           VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                           VerificarProximo().Tipo != TipoToken.TokenEOF)
                    {
                        var comandoDefault = AnalisarComando();
                        if (comandoDefault != null)
                            switchNode.DefaultComandos.Add(comandoDefault);
                    }
                }
            }

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar bloco do switch");
            return switchNode;
        }

        // ========================================
        // ANÁLISE DE COMANDO RETURN
        // ========================================

        private ComandoReturnNode AnalisarComandoReturn()
        {
            var tokenReturn = Proximo(); // Consome 'return'

            ASTNode expressao = null;
            if (VerificarProximo().Tipo != TipoToken.TokenSEMICOLON)
            {
                expressao = AnalisarExpressao();
            }

            Consumir(TipoToken.TokenSEMICOLON, "; esperado após return");

            return new ComandoReturnNode
            {
                Expressao = expressao,
                Linha = tokenReturn.Linha
            };
        }

        // ========================================
        // ANÁLISE DE ATRIBUIÇÃO OU CHAMADA
        // ========================================

        private ASTNode AnalisarAtribuicaoOuChamada()
        {
            var nome = Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador");

            // Chamada de método (obj.metodo())
            if (Match(TipoToken.TokenDOT))
            {
                var nomeMetodo = Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome do método após '.'");
                Consumir(TipoToken.TokenLPAREN, "Esperado '(' após nome do método");

                var chamada = new ChamadaMetodoNode
                {
                    NomeObjeto = nome.Lexema,
                    NomeMetodo = nomeMetodo.Lexema,
                    Linha = nome.Linha
                };

                // Argumentos
                if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
                {
                    do
                    {
                        var argumento = AnalisarExpressao();
                        if (argumento != null)
                            chamada.Argumentos.Add(argumento);
                    } while (Match(TipoToken.TokenCOMMA));
                }

                Consumir(TipoToken.TokenRPAREN, ") esperado após argumentos");
                return chamada;
            }
            // Acesso a array ou atribuição
            else if (VerificarProximo().Tipo == TipoToken.TokenLBRACKET)
            {
                Match(TipoToken.TokenLBRACKET);
                var indice = AnalisarExpressao();
                Consumir(TipoToken.TokenRBRACKET, "] esperado");
                Consumir(TipoToken.TokenASSIGN, "= esperado após índice de array");
                var expressao = AnalisarExpressao();

                return new AtribuicaoNode
                {
                    NomeVariavel = nome.Lexema,
                    Expressao = expressao,
                    EhArrayAccess = true,
                    IndiceArray = indice,
                    Linha = nome.Linha
                };
            }
            // Atribuição simples
            else if (Match(TipoToken.TokenASSIGN))
            {
                var expressao = AnalisarExpressao();

                return new AtribuicaoNode
                {
                    NomeVariavel = nome.Lexema,
                    Expressao = expressao,
                    Linha = nome.Linha
                };
            }
            // Incremento/Decremento
            else if (Match(TipoToken.TokenINCREMENT) || Match(TipoToken.TokenDECREMENT))
            {
                string operador = tokens[pos - 1].Lexema;
                return new ExpressaoUnariaNode
                {
                    Operador = operador,
                    Operando = new IdentificadorNode { Nome = nome.Lexema, Linha = nome.Linha },
                    EhPrefixo = false,
                    Linha = nome.Linha
                };
            }
            else
            {
                Erros.Add(new Erro("Atribuição ou chamada inválida", nome.Linha));
                return null;
            }
        }

        // ========================================
        // ANÁLISE DE EXPRESSÕES
        // ========================================

        private ASTNode AnalisarExpressao()
        {
            return AnalisarExpressaoOu();
        }

        private ASTNode AnalisarExpressaoOu()
        {
            var esquerda = AnalisarExpressaoE();

            while (Match(TipoToken.TokenOR))
            {
                string operador = tokens[pos - 1].Lexema;
                var direita = AnalisarExpressaoE();
                esquerda = new ExpressaoBinariaNode
                {
                    Esquerda = esquerda,
                    Operador = operador,
                    Direita = direita,
                    Linha = VerificarProximo().Linha
                };
            }

            return esquerda;
        }

        private ASTNode AnalisarExpressaoE()
        {
            var esquerda = AnalisarExpressaoIgualdade();

            while (Match(TipoToken.TokenAND))
            {
                string operador = tokens[pos - 1].Lexema;
                var direita = AnalisarExpressaoIgualdade();
                esquerda = new ExpressaoBinariaNode
                {
                    Esquerda = esquerda,
                    Operador = operador,
                    Direita = direita,
                    Linha = VerificarProximo().Linha
                };
            }

            return esquerda;
        }

        private ASTNode AnalisarExpressaoIgualdade()
        {
            var esquerda = AnalisarExpressaoRelacional();

            while (Match(TipoToken.TokenEQUALS) || Match(TipoToken.TokenNOT_EQUALS))
            {
                string operador = tokens[pos - 1].Lexema;
                var direita = AnalisarExpressaoRelacional();
                esquerda = new ExpressaoBinariaNode
                {
                    Esquerda = esquerda,
                    Operador = operador,
                    Direita = direita,
                    Linha = VerificarProximo().Linha
                };
            }

            return esquerda;
        }

        private ASTNode AnalisarExpressaoRelacional()
        {
            var esquerda = AnalisarExpressaoAritmetica();

            while (Match(TipoToken.TokenLESS) || Match(TipoToken.TokenLESS_EQUAL) ||
                   Match(TipoToken.TokenGREATER) || Match(TipoToken.TokenGREATER_EQUAL))
            {
                string operador = tokens[pos - 1].Lexema;
                var direita = AnalisarExpressaoAritmetica();
                esquerda = new ExpressaoBinariaNode
                {
                    Esquerda = esquerda,
                    Operador = operador,
                    Direita = direita,
                    Linha = VerificarProximo().Linha
                };
            }

            return esquerda;
        }

        private ASTNode AnalisarExpressaoAritmetica()
        {
            var esquerda = AnalisarTermo();

            while (Match(TipoToken.TokenPLUS) || Match(TipoToken.TokenMINUS))
            {
                string operador = tokens[pos - 1].Lexema;
                var direita = AnalisarTermo();
                esquerda = new ExpressaoBinariaNode
                {
                    Esquerda = esquerda,
                    Operador = operador,
                    Direita = direita,
                    Linha = VerificarProximo().Linha
                };
            }

            return esquerda;
        }

        private ASTNode AnalisarTermo()
        {
            var esquerda = AnalisarFator();

            while (Match(TipoToken.TokenMULTIPLY) || Match(TipoToken.TokenDIVIDE) || Match(TipoToken.TokenMODULO))
            {
                string operador = tokens[pos - 1].Lexema;
                var direita = AnalisarFator();
                esquerda = new ExpressaoBinariaNode
                {
                    Esquerda = esquerda,
                    Operador = operador,
                    Direita = direita,
                    Linha = VerificarProximo().Linha
                };
            }

            return esquerda;
        }

        private ASTNode AnalisarFator()
        {
            var tokenAtual = VerificarProximo();

            // Operadores unários
            if (Match(TipoToken.TokenNOT) || Match(TipoToken.TokenMINUS))
            {
                string operador = tokens[pos - 1].Lexema;
                var operando = AnalisarFator();
                return new ExpressaoUnariaNode
                {
                    Operador = operador,
                    Operando = operando,
                    EhPrefixo = true,
                    Linha = tokenAtual.Linha
                };
            }

            // Incremento/Decremento prefixo
            if (Match(TipoToken.TokenINCREMENT) || Match(TipoToken.TokenDECREMENT))
            {
                string operador = tokens[pos - 1].Lexema;
                var operando = AnalisarFator();
                return new ExpressaoUnariaNode
                {
                    Operador = operador,
                    Operando = operando,
                    EhPrefixo = true,
                    Linha = tokenAtual.Linha
                };
            }

            // Literais
            if (Match(TipoToken.TokenNUMBER))
            {
                return new LiteralNode
                {
                    Valor = tokens[pos - 1].Lexema,
                    Tipo = "int",
                    Linha = tokens[pos - 1].Linha
                };
            }

            if (Match(TipoToken.TokenFLOAT_LITERAL))
            {
                return new LiteralNode
                {
                    Valor = tokens[pos - 1].Lexema,
                    Tipo = "float",
                    Linha = tokens[pos - 1].Linha
                };
            }

            if (Match(TipoToken.TokenSTRING_LITERAL))
            {
                return new LiteralNode
                {
                    Valor = tokens[pos - 1].Lexema,
                    Tipo = "string",
                    Linha = tokens[pos - 1].Linha
                };
            }

            if (Match(TipoToken.TokenTRUE) || Match(TipoToken.TokenFALSE))
            {
                return new LiteralNode
                {
                    Valor = tokens[pos - 1].Lexema,
                    Tipo = "bool",
                    Linha = tokens[pos - 1].Linha
                };
            }

            // Identificador
            if (Match(TipoToken.TokenIDENTIFIER))
            {
                string nome = tokens[pos - 1].Lexema;
                int linha = tokens[pos - 1].Linha;

                // Incremento/Decremento pósfixo
                if (Match(TipoToken.TokenINCREMENT) || Match(TipoToken.TokenDECREMENT))
                {
                    string operador = tokens[pos - 1].Lexema;
                    return new ExpressaoUnariaNode
                    {
                        Operador = operador,
                        Operando = new IdentificadorNode { Nome = nome, Linha = linha },
                        EhPrefixo = false,
                        Linha = linha
                    };
                }

                // Chamada de método
                if (Match(TipoToken.TokenLPAREN))
                {
                    var chamada = new ChamadaMetodoNode
                    {
                        NomeMetodo = nome,
                        Linha = linha
                    };

                    // Argumentos
                    if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
                    {
                        do
                        {
                            var argumento = AnalisarExpressao();
                            if (argumento != null)
                                chamada.Argumentos.Add(argumento);
                        } while (Match(TipoToken.TokenCOMMA));
                    }

                    Consumir(TipoToken.TokenRPAREN, ") esperado");
                    return chamada;
                }

                // Acesso a array
                if (Match(TipoToken.TokenLBRACKET))
                {
                    var indice = AnalisarExpressao();
                    Consumir(TipoToken.TokenRBRACKET, "] esperado");
                    return new IdentificadorNode
                    {
                        Nome = nome,
                        EhArrayAccess = true,
                        IndiceArray = indice,
                        Linha = linha
                    };
                }

                // Identificador simples
                return new IdentificadorNode
                {
                    Nome = nome,
                    Linha = linha
                };
            }

            // Expressão entre parênteses
            if (Match(TipoToken.TokenLPAREN))
            {
                var expressao = AnalisarExpressao();
                Consumir(TipoToken.TokenRPAREN, ") esperado");
                return expressao;
            }

            // Se chegou aqui, é um fator inválido
            Erros.Add(new Erro("Fator inválido", tokenAtual.Linha));
            Proximo(); // Pula token inválido
            return new LiteralNode { Valor = "0", Tipo = "int", Linha = tokenAtual.Linha }; // Valor padrão para recuperação
        }

        // ========================================
        // MÉTODOS AUXILIARES
        // ========================================

        private void PularAteProximaDeclaracao()
        {
            while (VerificarProximo().Tipo != TipoToken.TokenEOF &&
                   VerificarProximo().Tipo != TipoToken.TokenSEMICOLON &&
                   VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                   VerificarProximo().Tipo != TipoToken.TokenPUBLIC &&
                   VerificarProximo().Tipo != TipoToken.TokenPRIVATE &&
                   VerificarProximo().Tipo != TipoToken.TokenPROTECTED &&
                   VerificarProximo().Tipo != TipoToken.TokenINT &&
                   VerificarProximo().Tipo != TipoToken.TokenFLOAT &&
                   VerificarProximo().Tipo != TipoToken.TokenSTRING &&
                   VerificarProximo().Tipo != TipoToken.TokenBOOL &&
                   VerificarProximo().Tipo != TipoToken.TokenVOID)
            {
                Proximo();
            }

            // Se parou em ';', consome
            if (VerificarProximo().Tipo == TipoToken.TokenSEMICOLON)
            {
                Proximo();
            }
        }
    }

    // ========================================
    // CLASSE DE ERRO (REUTILIZADA)
    // ========================================

    public class Erro
    {
        public string Mensagem { get; }
        public int Linha { get; }

        public Erro(string mensagem, int linha)
        {
            Mensagem = mensagem;
            Linha = linha;
        }

        public override string ToString()
        {
            return $"Erro na linha {Linha}: {Mensagem}";
        }
    }
}
