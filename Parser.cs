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
            {
                Erros.Add(new Erro(mensagemErro, VerificarProximo().Linha));
                Sincronizar();
            }
        }

        // MODO PÂNICO: Sincronização para recuperação de erros
        private void Sincronizar()
        {
            while (VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                // Verifica se encontrou um token de sincronização
                if (EhTokenSincronizacao(VerificarProximo().Tipo))
                {
                    return; // Para aqui, encontrou ponto de sincronização
                }
                Proximo(); // Pula o token atual
            }
        }

        private bool EhTokenSincronizacao(TipoToken tipo)
        {
            return tipo == TipoToken.TokenSEMICOLON ||
                   tipo == TipoToken.TokenRBRACE ||
                   tipo == TipoToken.TokenLBRACE ||
                   tipo == TipoToken.TokenIF ||
                   tipo == TipoToken.TokenWHILE ||
                   tipo == TipoToken.TokenFOR ||
                   tipo == TipoToken.TokenDO ||
                   tipo == TipoToken.TokenFOREACH ||
                   tipo == TipoToken.TokenSWITCH ||
                   tipo == TipoToken.TokenRETURN ||
                   tipo == TipoToken.TokenBREAK ||
                   tipo == TipoToken.TokenCONTINUE ||
                   tipo == TipoToken.TokenCLASS ||
                   tipo == TipoToken.TokenNAMESPACE ||
                   tipo == TipoToken.TokenUSING ||
                   tipo == TipoToken.TokenPUBLIC ||
                   tipo == TipoToken.TokenPRIVATE ||
                   tipo == TipoToken.TokenPROTECTED ||
                   tipo == TipoToken.TokenINTERNAL ||
                   tipo == TipoToken.TokenINT ||
                   tipo == TipoToken.TokenFLOAT ||
                   tipo == TipoToken.TokenSTRING ||
                   tipo == TipoToken.TokenBOOL ||
                   tipo == TipoToken.TokenVOID;
        }

        private void SincronizarAteProximoComando()
        {
            while (VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                if (VerificarProximo().Tipo == TipoToken.TokenSEMICOLON)
                {
                    Proximo(); // Consome o ';'
                    return;
                }
                if (VerificarProximo().Tipo == TipoToken.TokenRBRACE ||
                    VerificarProximo().Tipo == TipoToken.TokenLBRACE ||
                    EhTokenSincronizacao(VerificarProximo().Tipo))
                {
                    return;
                }
                Proximo();
            }
        }

        private void SincronizarAteProximoBloco()
        {
            while (VerificarProximo().Tipo != TipoToken.TokenEOF)
            {
                if (VerificarProximo().Tipo == TipoToken.TokenRBRACE ||
                    VerificarProximo().Tipo == TipoToken.TokenLBRACE)
                {
                    return;
                }
                Proximo();
            }
        }

        public void Analisar()
        {
            try
            {
                // Reconhecer todos os usings no início
                while (VerificarProximo().Tipo == TipoToken.TokenUSING)
                {
                    try
                    {
                        DeclaracaoUsing();
                    }
                    catch (Exception)
                    {
                        Sincronizar();
                    }
                }

                // Espera o namespace depois dos usings
                if (VerificarProximo().Tipo == TipoToken.TokenNAMESPACE)
                {
                    try
                    {
                        DeclaracaoNamespace();
                    }
                    catch (Exception)
                    {
                        Sincronizar();
                    }
                }
                else
                {
                    Erros.Add(new Erro("Esperado 'namespace' após os usings", VerificarProximo().Linha));
                    Sincronizar();
                }
            }
            catch (Exception ex)
            {
                Erros.Add(new Erro($"Erro crítico na análise: {ex.Message}", VerificarProximo().Linha));
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
                try
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
                catch (Exception)
                {
                    Sincronizar();
                }
            }

            Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar namespace");
        }

        private void Comando()
        {
            try
            {
                if (Match(TipoToken.TokenIF)) ComandoIf();
                else if (Match(TipoToken.TokenWHILE)) ComandoWhile();
                else if (Match(TipoToken.TokenFOR)) ComandoFor();
                else if (Match(TipoToken.TokenDO)) ComandoDoWhile();
                else if (Match(TipoToken.TokenFOREACH)) ComandoForeach();
                else if (Match(TipoToken.TokenSWITCH)) ComandoSwitch();
                else if (Match(TipoToken.TokenRETURN))
                {
                    if (VerificarProximo().Tipo != TipoToken.TokenSEMICOLON)
                    {
                        Expressao();
                    }
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
                    SincronizarAteProximoComando();
                }
            }
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void ComandoIf()
        {
            try
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
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void ComandoWhile()
        {
            try
            {
                Consumir(TipoToken.TokenLPAREN, "Esperado (");
                Expressao();
                Consumir(TipoToken.TokenRPAREN, "Esperado )");
                Comando();
            }
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void ComandoDoWhile()
        {
            try
            {
                Consumir(TipoToken.TokenLBRACE, "Esperado '{' após do");

                while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Comando();
                }
                Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar o bloco do-while");
                Consumir(TipoToken.TokenWHILE, "Esperado 'while' após bloco do-while");
                Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'while'");
                Expressao();
                Consumir(TipoToken.TokenRPAREN, "Esperado ')' após condição");
                Consumir(TipoToken.TokenSEMICOLON, "Esperado ';' após ')' do while");
            }
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void ComandoFor()
        {
            try
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
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void ComandoForeach()
        {
            try
            {
                Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'foreach'");

                if (Match(TipoToken.TokenINT) || Match(TipoToken.TokenFLOAT) || Match(TipoToken.TokenSTRING) || Match(TipoToken.TokenBOOL))
                {
                    Consumir(TipoToken.TokenIDENTIFIER, "Esperado identificador após tipo no foreach");
                    Consumir(TipoToken.TokenIN, "Esperado 'in' após identificador no foreach");
                    Expressao();
                    Consumir(TipoToken.TokenRPAREN, "Esperado ')' após cláusula do foreach");

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
                        Comando();
                    }
                }
                else
                {
                    Erros.Add(new Erro("Esperado tipo (int, float, string ou bool) após '(' no foreach", VerificarProximo().Linha));
                    SincronizarAteProximoComando();
                }
            }
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void ComandoSwitch()
        {
            try
            {
                Consumir(TipoToken.TokenLPAREN, "Esperado '(' após 'switch'");
                Expressao();
                Consumir(TipoToken.TokenRPAREN, "Esperado ')' após expressão do switch");
                Consumir(TipoToken.TokenLBRACE, "Esperado '{' após switch");

                while (VerificarProximo().Tipo == TipoToken.TokenCASE || VerificarProximo().Tipo == TipoToken.TokenDEFAULT)
                {
                    try
                    {
                        if (Match(TipoToken.TokenCASE))
                        {
                            if (Match(TipoToken.TokenNUMBER) || Match(TipoToken.TokenSTRING_LITERAL) || Match(TipoToken.TokenCHAR_LITERAL))
                            {
                                Consumir(TipoToken.TokenCOLON, "Esperado ':' após valor do case");

                                while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                                       VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                                       VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                                       VerificarProximo().Tipo != TipoToken.TokenEOF)
                                {
                                    Comando();
                                }
                            }
                            else
                            {
                                Erros.Add(new Erro("Valor literal esperado após 'case'", VerificarProximo().Linha));
                                SincronizarAteProximoComando();
                            }
                        }
                        else if (Match(TipoToken.TokenDEFAULT))
                        {
                            Consumir(TipoToken.TokenCOLON, "Esperado ':' após 'default'");

                            while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                                   VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                                   VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                                   VerificarProximo().Tipo != TipoToken.TokenEOF)
                            {
                                Comando();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Sincronizar para próximo case, default ou fim do switch
                        while (VerificarProximo().Tipo != TipoToken.TokenCASE &&
                               VerificarProximo().Tipo != TipoToken.TokenDEFAULT &&
                               VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                               VerificarProximo().Tipo != TipoToken.TokenEOF)
                        {
                            Proximo();
                        }
                    }
                }

                Consumir(TipoToken.TokenRBRACE, "Esperado '}' para fechar bloco do switch");
            }
            catch (Exception)
            {
                SincronizarAteProximoBloco();
            }
        }

        private void DeclaracaoVariavel()
        {
            try
            {
                Proximo();
                Consumir(TipoToken.TokenIDENTIFIER, "Identificador esperado");
                if (Match(TipoToken.TokenASSIGN))
                {
                    Expressao();
                }
                Consumir(TipoToken.TokenSEMICOLON, "; esperado após declaração");
            }
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void DeclaracaoMetodo()
        {
            try
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
                    SincronizarAteProximoBloco();
                }
            }
            catch (Exception)
            {
                SincronizarAteProximoBloco();
            }
        }

        private void AtribuicaoOuChamada()
        {
            try
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
            catch (Exception)
            {
                SincronizarAteProximoComando();
            }
        }

        private void Expressao()
        {
            try
            {
                ExpressaoOu();
            }
            catch (Exception)
            {
                // Sincronizar até próximo delimitador de expressão
                while (VerificarProximo().Tipo != TipoToken.TokenSEMICOLON &&
                       VerificarProximo().Tipo != TipoToken.TokenRPAREN &&
                       VerificarProximo().Tipo != TipoToken.TokenRBRACKET &&
                       VerificarProximo().Tipo != TipoToken.TokenCOMMA &&
                       VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                       VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Proximo();
                }
            }
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
            while (Match(TipoToken.TokenMULTIPLY) || Match(TipoToken.TokenDIVIDE) || Match(TipoToken.TokenMODULO))
                Fator();
        }

        private void Fator()
        {
            try
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
                else if (Match(TipoToken.TokenNUMBER) || Match(TipoToken.TokenFLOAT_LITERAL) || Match(TipoToken.TokenSTRING_LITERAL) || Match(TipoToken.TokenTRUE) || Match(TipoToken.TokenFALSE))
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
                    if (VerificarProximo().Tipo != TipoToken.TokenEOF)
                    {
                        Proximo();
                    }
                }
            }
            catch (Exception)
            {
                // Sincronizar até próximo operador ou delimitador
                while (VerificarProximo().Tipo != TipoToken.TokenSEMICOLON &&
                       VerificarProximo().Tipo != TipoToken.TokenRPAREN &&
                       VerificarProximo().Tipo != TipoToken.TokenRBRACKET &&
                       VerificarProximo().Tipo != TipoToken.TokenCOMMA &&
                       VerificarProximo().Tipo != TipoToken.TokenPLUS &&
                       VerificarProximo().Tipo != TipoToken.TokenMINUS &&
                       VerificarProximo().Tipo != TipoToken.TokenMULTIPLY &&
                       VerificarProximo().Tipo != TipoToken.TokenDIVIDE &&
                       VerificarProximo().Tipo != TipoToken.TokenRBRACE &&
                       VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Proximo();
                }
            }
        }

        private void DeclaracaoClasse()
        {
            try
            {
                Consumir(TipoToken.TokenCLASS, "Esperado 'class'");
                Consumir(TipoToken.TokenIDENTIFIER, "Esperado nome da classe");
                Consumir(TipoToken.TokenLBRACE, "Esperado '{' após declaração de classe");

                while (VerificarProximo().Tipo != TipoToken.TokenRBRACE && VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    try
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
                    catch (Exception)
                    {
                        Sincronizar();
                    }
                }

                Consumir(TipoToken.TokenRBRACE, "Esperado '}' ao final da classe");
            }
            catch (Exception)
            {
                SincronizarAteProximoBloco();
            }
        }

        private void Parametros()
        {
            try
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
            catch (Exception)
            {
                // Sincronizar até próximo ')'
                while (VerificarProximo().Tipo != TipoToken.TokenRPAREN &&
                       VerificarProximo().Tipo != TipoToken.TokenEOF)
                {
                    Proximo();
                }
            }
        }

        private void ConsumirTipoRetorno()
        {
            try
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
                    Sincronizar();
                }
            }
            catch (Exception)
            {
                Sincronizar();
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