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

        private void Consumir(TipoToken tipo, string mensagemErro)
        {
            if (!Match(tipo))
                Erros.Add(new Erro(mensagemErro, VerificarProximo().Linha));
        }

        public void Analisar()
{
    // Reconhecer todos os usings no início
    while (VerificarProximo().Tipo == TipoToken.TokenUSING)
    {
        DeclaracaoUsing();
    }

    // Espera o namespace depois dos usings
    if (VerificarProximo().Tipo == TipoToken.TokenNAMESPACE)
    {
        DeclaracaoNamespace();
    }
    else
    {
        Erros.Add(new Erro("Esperado 'namespace' após os usings", VerificarProximo().Linha));
    }
}

private void DeclaracaoUsing()
{
    Consumir(TipoToken.TokenUSING, "Esperado 'using'");
    Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após 'using'");

    while (Match(TipoToken.TokenDOT))
    {
        Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após '.' em using");
    }

    Consumir(TipoToken.TokenSEMICOLON, "Esperado ';' após declaração using");
}

private void DeclaracaoNamespace()
{
    Consumir(TipoToken.TokenNAMESPACE, "Esperado 'namespace'");
    Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após 'namespace'");

    while (Match(TipoToken.TokenDOT))
    {
        Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após '.' em namespace");
    }

    Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração do namespace");

    // Agora lê classes, modificadores, comandos dentro do namespace
    while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
    {
        if (VerificarProximo().Tipo == TipoToken.TokenCLASS)
        {
            DeclaracaoClasse();
        }
        else if (VerificarProximo().Tipo == TipoToken.TokenPUBLIC ||
                 VerificarProximo().Tipo == TipoToken.TokenPRIVATE ||
                 VerificarProximo().Tipo == TipoToken.TokenPROTECTED)
        {
            DeclaracaoMetodo();
        }
        else
        {
            Comando();
        }
    }

    Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar namespace");
}



        private void Comando()
        {
            if (Match(TipoToken.TokenIF)) ComandoIf();
            else if (Match(TipoToken.TokenWHILE)) ComandoWhile();
            else if (Match(TipoToken.TokenFOR)) ComandoFor();
            else if (Match(TipoToken.TokenDO)) ComandoDoWhile();
            else if (Match(TipoToken.TokenFOREACH)) ComandoForeach();
            else if (Match(TipoToken.TokenSWITCH)) ComandoSwitch();
            else if (Match(TipoToken.TokenRETURN))
            {
                Expressao();
                Consumir(TipoToken.TokenSEMICOLON, "; esperado após return");
            }
            else if (Match(TipoToken.TokenBREAK))
            {
                Consumir(TipoToken.TokenSEMICOLON, "; esperado após 'break'");
            }
            else if (VerificarProximo().Tipo == TipoToken.TokenINT ||
                     VerificarProximo().Tipo == TipoToken.TokenFLOAT ||
                     VerificarProximo().Tipo == TipoToken.TokenSTRING ||
                     VerificarProximo().Tipo == TipoToken.TokenBOOL)
            {
                DeclaracaoVariavel();
            }
            else if (VerificarProximo().Tipo == TipoToken.TokenIDENTIFIER)
            {
                AtribuicaoOuChamada();
                Consumir(TipoToken.TokenSEMICOLON, "; esperado");
            }
            else if (Match(TipoToken.TokenLBRACE))
            {
                while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Comando();
                }
                Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar o bloco");
            }
            else
            {
                Erros.Add(new Erro("Comando inválido", VerificarProximo().Linha));
                Proximo();
            }
        }
        private void ComandoIf()
        {
            Consumir(TipoToken.TokenLPAREN, "Esperado (");
            Expressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado )");
            Comando();
            if (Match(TipoToken.TokenELSE))
            {
                Comando();
            }
        }

        private void ComandoWhile()
        {
            Consumir(TipoToken.TokenLPAREN, "Esperado (");
            Expressao();
            Consumir(TipoToken.TokenRPAREN, "Esperado )");
            Comando();
        }

        private void ComandoDoWhile()
        {
            // Espera abrir bloco {
            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após do");

            // Enquanto não fechar bloco }
            while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                Comando(); // analisa os comandos dentro do bloco
            }
            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar o bloco do-while");
            // Agora espera a palavra while
            Consumir(TipoToken.TokenWHILE, "Esperado 'while' após bloco do-while");
            // Abre parênteses para condição
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'while'");
            // Analisa a expressão da condição
            Expressao();
            // Fecha parênteses da condição
            Consumir(TipoToken.TokenRPAREN, "Esperado ')' após condição");
            // Espera o ponto-e-vírgula final
            Consumir(TipoToken.TokenSEMICOLON, "Esperado ';' após ')' do while");
        }


        private void ComandoFor()
        {
            Consumir(TipoToken.TokenLPAREN, "Esperado (");
            if (VerificarProximo().Tipo == TipoToken.TokenINT || VerificarProximo().Tipo == TipoToken.TokenFLOAT || VerificarProximo().Tipo == TipoToken.TokenSTRING || VerificarProximo().Tipo == TipoToken.TokenBOOL)
            {
                DeclaracaoVariavel();
            }
            else
            {
                AtribuicaoOuChamada();
                Consumir(TipoToken.TokenSEMICOLON, "; esperado após inicialização do for");
            }

            Expressao();
            Consumir(TipoToken.TokenSEMICOLON, "; esperado após condição do for");

            if (VerificarProximo().Tipo == TipoToken.TokenIDENTIFIER)
            {
                Token ident = Proximo();
                if (Match(TipoToken.TokenINCREMENT) || Match(TipoToken.TokenDECREMENT))
                {
                }
                else if (VerificarProximo().Tipo == TipoToken.TokenASSIGN || VerificarProximo().Tipo == TipoToken.TokenLBRACKET || VerificarProximo().Tipo == TipoToken.TokenLPAREN)
                {
                    pos--;
                    AtribuicaoOuChamada();
                }
                else
                {
                    Erros.Add(new Erro("Incremento do for inválido", ident.Linha));
                }
            }
            else
            {
                Erros.Add(new Erro("Incremento do for inválido", VerificarProximo().Linha));
                Proximo();
            }

            Consumir(TipoToken.TokenRPAREN, ") esperado após cláusulas do for");
            Comando();
        }


        private void ComandoForeach()
        {
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'foreach'");

            // Espera tipo: int, float, string ou bool
            if (Match(TipoToken.TokenINT) || Match(TipoToken.TokenFLOAT) || Match(TipoToken.TokenSTRING) || Match(TipoToken.TokenBOOL))
            {
                // Consumido o tipo, agora espera o identificador (variável do foreach)
                Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após tipo no foreach");

                // Espera o token 'in'
                Consumir(TipoToken.TokenIN, "Esperado 'in' após identificador no foreach");

                // Expressão que representa a coleção/array a iterar
                Expressao();

                Consumir(TipoToken.TokenRPAREN, "Esperado ')' após cláusula do foreach");

                // Agora o bloco de comandos dentro do foreach
                if (Match(TipoToken.TokenLBRACE))
                {
                    while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
                    {
                        Comando();
                    }
                    Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar o bloco do foreach");
                }
                else
                {
                    // Permitir comando único sem chaves (opcional)
                    Comando();
                }
            }
            else
            {
                Erros.Add(new Erro("Esperado tipo (int, float, string ou bool) após '(' no foreach", VerificarProximo().Linha));
            }
        }


        private void ComandoSwitch()
        {
            Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'switch'");
            Expressao(); // expressão de controle do switch
            Consumir(TipoToken.TokenRPAREN, "Esperado ')' após expressão do switch");
            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após switch");

            while (VerificarProximo().Tipo == TipoToken.TokenCASE || VerificarProximo().Tipo == TipoToken.TokenDEFAULT)
            {
                if (Match(TipoToken.TokenCASE))
                {
                    // Suporta valores literais: número, string ou caractere
                    if (Match(TipoToken.TokenNUMBER) || Match(TipoToken.TokenSTRING_LITERAL) || Match(TipoToken.TokenCHAR_LITERAL))
                    {
                        Consumir(TipoToken.TokenCOLON, "Esperado ':' após valor do case");

                        while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                               VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                               VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                               VerificarProximo().Tipo != TipoToken.TokenEOF)
                        {
                            Comando(); // comandos dentro do case
                        }
                    }
                    else
                    {
                        Erros.Add(new Erro("Valor literal esperado após 'case'", VerificarProximo().Linha));
                        Proximo();
                    }
                }
                else if (Match(TipoToken.TokenBREAK))
                {
                    Consumir(TipoToken.TokenSEMICOLON, "; esperado após 'break'");
                }
                else if (Match(TipoToken.TokenDEFAULT))
                {
                    Consumir(TipoToken.TokenCOLON, "Esperado ':' após 'default'");

                    while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                           VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                           VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                           VerificarProximo().Tipo != TipoToken.TokenEOF)
                    {
                        Comando(); // comandos dentro do default
                    }
                }
            }

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar bloco do switch");
        }



        private void DeclaracaoVariavel()
        {
            Proximo();
            Consumir(TipoToken.TokenIDENTIFIER, "Identificador esperado");
            if (Match(TipoToken.TokenASSIGN))
            {
                Expressao();
            }
            Consumir(TipoToken.TokenSEMICOLON, "; esperado após declaração");
        }

        private void DeclaracaoMetodo()
        {
            Proximo(); // modificador de acesso

            if (Match(TipoToken.TokenINT) || Match(TipoToken.TokenFLOAT) || Match(TipoToken.TokenSTRING) || Match(TipoToken.TokenBOOL) || Match(TipoToken.TokenVOID))
            {
                Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após tipo de retorno do método");
                Consumir(TipoToken.TokenLPAREN, "Esperado '(' após nome do método");

                if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
                {
                    do
                    {
                        if (!(Match(TipoToken.TokenINT) || Match(TipoToken.TokenFLOAT) || Match(TipoToken.TokenSTRING) || Match(TipoToken.TokenBOOL)))
                        {
                            Erros.Add(new Erro("Tipo de parâmetro inválido", VerificarProximo().Linha));
                        }
                        Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome do parâmetro");
                    } while (Match(TipoToken.TokenCOMMA));
                }

                Consumir(TipoToken.TokenRPAREN, "Esperado ')' após parâmetros do método");

                Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração de método");
                while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Comando();
                }
                Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar bloco do método");
            }
            else
            {
                Erros.Add(new Erro("Tipo de retorno inválido após modificador de acesso", VerificarProximo().Linha));
            }
        }


        private void AtribuicaoOuChamada()
        {
            Token ident = Proximo();
            if (Match(TipoToken.TokenDOT))
            {
                Token metodo = Proximo();
                if (metodo.Lexema == "WriteLine" && Match(TipoToken.TokenLPAREN))
                {
                    if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
                    {
                        Expressao();
                        while (Match(TipoToken.TokenCOMMA))
                            Expressao();
                    }
                    Consumir(TipoToken.TokenRPAREN, ") esperado após argumentos do WriteLine");
                }
                else
                {
                    Erros.Add(new Erro("Método inválido após ponto", metodo.Linha));
                }
            }
            else if (Match(TipoToken.TokenLPAREN))
            {
                if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
                {
                    Expressao();
                    while (Match(TipoToken.TokenCOMMA))
                        Expressao();
                }
                Consumir(TipoToken.TokenRPAREN, ") esperado");
            }
            else if (Match(TipoToken.TokenASSIGN))
            {
                Expressao();
            }
            else if (Match(TipoToken.TokenLBRACKET))
            {
                Expressao();
                Consumir(TipoToken.TokenRBRACKET, "] esperado");
                Consumir(TipoToken.TokenASSIGN, "= esperado após índice de array");
                Expressao();
            }
            else if (Match(TipoToken.TokenINCREMENT) || Match(TipoToken.TokenDECREMENT))
            {
            }
            else
            {
                Erros.Add(new Erro("Atribuição ou chamada inválida", ident.Linha));
            }
        }

        private void Expressao()
        {
            ExpressaoOu();
        }

        private void ExpressaoOu()
        {
            ExpressaoE();
            while (Match(TipoToken.TokenOR))
                ExpressaoE();
        }

        private void ExpressaoE()
        {
            ExpressaoIgualdade();
            while (Match(TipoToken.TokenAND))
                ExpressaoIgualdade();
        }

        private void ExpressaoIgualdade()
        {
            ExpressaoRelacional();
            while (Match(TipoToken.TokenEQUALS) || Match(TipoToken.TokenNOT_EQUALS))
                ExpressaoRelacional();
        }

        private void ExpressaoRelacional()
        {
            ExpressaoAritmetica();
            while (Match(TipoToken.TokenLESS) || Match(TipoToken.TokenLESS_EQUAL) || Match(TipoToken.TokenGREATER) || Match(TipoToken.TokenGREATER_EQUAL))
                ExpressaoAritmetica();
        }

        private void ExpressaoAritmetica()
        {
            Termo();
            while (Match(TipoToken.TokenPLUS) || Match(TipoToken.TokenMINUS))
                Termo();
        }

        private void Termo()
        {
            Fator();
            while (Match(TipoToken.TokenMULTIPLY) || Match(TipoToken.TokenDIVIDE))
                Fator();
        }

        private void Fator()
        {
            Token token = VerificarProximo();

            if (Match(TipoToken.TokenNOT))
            {
                Fator();
            }
            else if (Match(TipoToken.TokenMINUS))
            {
                Fator();
            }
            else if (Match(TipoToken.TokenNUMBER) || Match(TipoToken.TokenSTRING_LITERAL) || Match(TipoToken.TokenTRUE) || Match(TipoToken.TokenFALSE))
            {
                return;
            }
            else if (Match(TipoToken.TokenIDENTIFIER))
            {
                if (Match(TipoToken.TokenINCREMENT) || Match(TipoToken.TokenDECREMENT))
                {
                    return;
                }
                if (Match(TipoToken.TokenLPAREN))
                {
                    if (VerificarProximo().Tipo != TipoToken.TokenRPAREN)
                    {
                        Expressao();
                        while (Match(TipoToken.TokenCOMMA))
                            Expressao();
                    }
                    Consumir(TipoToken.TokenRPAREN, ") esperado");
                }
                else if (Match(TipoToken.TokenLBRACKET))
                {
                    Expressao();
                    Consumir(TipoToken.TokenRBRACKET, "] esperado");
                }
            }
            else if (Match(TipoToken.TokenLPAREN))
            {
                Expressao();
                Consumir(TipoToken.TokenRPAREN, ") esperado");
            }
            else
            {
                Erros.Add(new Erro("Fator inválido", token.Linha));
                Proximo();
            }
        }


        private void DeclaracaoClasse()
        {
            Consumir(TipoToken.TokenCLASS, "Esperado 'class'");
            Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome da classe");
            Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração de classe");

            while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                if (VerificarProximo().Tipo == TipoToken.TokenPUBLIC ||
                    VerificarProximo().Tipo == TipoToken.TokenPRIVATE ||
                    VerificarProximo().Tipo == TipoToken.TokenPROTECTED)
                {
                    DeclaracaoMetodo();
                }
                else
                {
                    Comando();
                }
            }

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' ao final da classe");
        }

        private void Parametros()
        {
            ConsumirTipoRetorno();
            Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome de parâmetro");

            while (VerificarProximo().Tipo == TipoToken.TokenCOMMA)
            {
                Proximo(); // Consome a vírgula
                ConsumirTipoRetorno();
                Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome de parâmetro");
            }
        }

        private void ConsumirTipoRetorno()
        {
            var tipo = VerificarProximo().Tipo;
            if (tipo == TipoToken.TokenINT || tipo == TipoToken.TokenFLOAT ||
                tipo == TipoToken.TokenSTRING || tipo == TipoToken.TokenBOOL ||
                tipo == TipoToken.TokenVOID)
            {
                Proximo(); // Consome o tipo
            }
            else
            {
                Erros.Add(new Erro("Esperado tipo de retorno ou tipo de parâmetro", VerificarProximo().Linha));
            }
        }

    }





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
