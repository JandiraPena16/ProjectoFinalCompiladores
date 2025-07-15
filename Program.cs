using System;
using System.Collections.Generic;
using JPenaCompiler;
using static JPenaCompiler.Lexer;   // Supondo que Lexer, Token, TipoToken estão neste namespace
using static JPenaCompiler.Parser; // Supondo que Parser está neste namespace
using static JPenaCompiler.Token;
using static JPenaCompiler.TipoToken;

namespace AnalisadorCompleto
{
    class Program
    {
        static void Main(string[] args)
        {
            // Código fonte diretamente embutido como string
            string codigoFonte = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;

                namespace JPenaCompiler
                {
                    class Program
                    {
                        int x;
                        int x = ""João"";
                        string nome = ""João"";

                        if (x > y) 
                            x = x + 1;
                        else 
                            y = y - 1;

                        for (i = 0; i < numeros; i++)
                        {
                            Console.WriteLine(numeros[i]);
                        }                

                        while (i < numeros)
                        {
                            int x = ""João"";
                        }

                        do
                        {
            
                        } while (i < numeros);

                        foreach (int numero in numeros)
                        {
                        }

                        switch (numero)
                        {
                            case 1:
                                Console.WriteLine(""Você escolheu UM."");
                                break;
                            case 2:
                                Console.WriteLine(""Você escolheu DOIS."");
                                break;
                            case 3:
                                Console.WriteLine(""Você escolheu TRÊS."");
                                break;
                            default:
                                Console.WriteLine(""Número fora do intervalo."");
                                break;
                        }

                        switch (fruta)
                        {
                            case ""maçã"":
                                Console.WriteLine(""Você escolheu maçã."");
                                break;
                            case ""banana"":
                                Console.WriteLine(""Você escolheu banana."");
                                break;
                            case ""uva"":
                                Console.WriteLine(""Você escolheu uva."");
                                break;
                            default:
                                Console.WriteLine(""Fruta desconhecida."");
                                break;
                        }
             
                        int numero = 2;

                        switch (numero)
                        {
                            case 1:
                                Console.WriteLine(""Fruta desconhecida."");
                                int numero = 2;
                                break;
                            case 2:
                                break;
                            default:
                                break;
                        }

                        public int Soma(int x, int y)
                        {
                            return 2 + 2;
                        }
                
                        public void MetodoVazio()
                        {
                        }

                        private float CalcularArea(float largura, float altura)
                        {
                            return largura * altura;
                        }

                        protected void MostrarMensagem()
                        {
                            Console.WriteLine(""Olá, mundo!"");
                        }
                
                        public bool VerificarIdade(int idade, bool maioridadeObrigatoria)
                        {
                            if (maioridadeObrigatoria)
                            {
                                return idade >= 18;
                            }
                            else
                            {
                                return true;
                            }
                        }
            
                        foreach (int numero in numeros)
                        {
                        }
                    }
                }

                ";

            try
            {
                Console.WriteLine("=== COMPILADOR JPenaCompiler ===");
                Console.WriteLine($"Código embutido no programa");
                Console.WriteLine($"Tamanho: {codigoFonte.Length} caracteres");
                Console.WriteLine($"Linhas: {codigoFonte.Split('\n').Length}");
                Console.WriteLine();

                // ========================================
                // FASE 1: ANÁLISE LÉXICA
                // ========================================
                Console.WriteLine("=== FASE 1: ANÁLISE LÉXICA ===");
                Lexer lexer = new Lexer(codigoFonte);
                List<Token> tokens = lexer.Tokenizar();

                Console.WriteLine("Tokens Reconhecidos:");
                foreach (var token in tokens)
                {
                    Console.WriteLine($"[Linha {token.Linha}] Tipo: {token.Tipo}, Valor: '{token.Lexema}'");
                }
                Console.WriteLine($"Total de tokens: {tokens.Count}");

                // ========================================
                // FASE 2: ANÁLISE SINTÁTICA (GERAR AST)
                // ========================================
                Console.WriteLine("\n=== FASE 2: ANÁLISE SINTÁTICA ===");
                Console.WriteLine("-------------Lista de Erros---------------");

                // Usar o novo ParserAST que gera AST
                Parser parserAST = new Parser(tokens);
                ProgramaNode programaAST = parserAST.AnalisarPrograma();

                // Verificar erros sintáticos
                if (parserAST.Erros.Count > 0)
                {
                    Console.WriteLine("\nErros Sintáticos Encontrados:");
                    foreach (var erro in parserAST.Erros)
                    {
                        Console.WriteLine(erro);
                    }
                    Console.WriteLine($"Total de erros sintáticos: {parserAST.Erros.Count}");

                    // Se há erros sintáticos, não continuar para análise semântica
                    Console.WriteLine("\n❌ Não é possível continuar para análise semântica devido a erros sintáticos.");

                    // ========================================
                    // RESUMO FINAL COM ERROS
                    // ========================================
                    Console.WriteLine("\n=== RESUMO DA COMPILAÇÃO ===");
                    Console.WriteLine($"🔤 Tokens gerados: {tokens.Count}");
                    Console.WriteLine($"❌ Erros sintáticos: {parserAST.Erros.Count}");
                    Console.WriteLine("💥 Compilação falhou devido a erros sintáticos.");
                }
                else
                {
                    Console.WriteLine("\n✅ Análise sintática concluída com sucesso. Nenhum erro encontrado.");
                    Console.WriteLine($"📊 AST gerada com sucesso!");

                    // ========================================
                    // FASE 3: ANÁLISE SEMÂNTICA
                    // ========================================
                    Console.WriteLine("\n=== FASE 3: ANÁLISE SEMÂNTICA ===");

                    // Análise semântica com AST real
                    Semantic analisador = new Semantic();
                    analisador.Analisar(programaAST);

                    // Mostrar resultados da análise semântica
                    if (analisador.Erros.Count > 0)
                    {
                        Console.WriteLine("Erros Semânticos Encontrados:");
                        foreach (var erro in analisador.Erros)
                        {
                            Console.WriteLine(erro);
                        }
                        Console.WriteLine($"Total de erros semânticos: {analisador.Erros.Count}");

                        // ========================================
                        // RESUMO FINAL COM ERROS SEMÂNTICOS
                        // ========================================
                        Console.WriteLine("\n=== RESUMO DA COMPILAÇÃO ===");
                        Console.WriteLine($"🔤 Tokens gerados: {tokens.Count}");
                        Console.WriteLine($"✅ Erros sintáticos: {parserAST.Erros.Count}");
                        Console.WriteLine($"❌ Erros semânticos: {analisador.Erros.Count}");
                        Console.WriteLine("⚠️ Compilação completada com erros semânticos.");
                    }
                    else
                    {
                        Console.WriteLine("✅ Análise semântica concluída com sucesso. Nenhum erro encontrado.");

                        // ========================================
                        // RESUMO FINAL SUCESSO TOTAL
                        // ========================================
                        Console.WriteLine("\n=== RESUMO DA COMPILAÇÃO ===");
                        Console.WriteLine($"🔤 Tokens gerados: {tokens.Count}");
                        Console.WriteLine($"✅ Erros sintáticos: {parserAST.Erros.Count}");
                        Console.WriteLine($"✅ Erros semânticos: {analisador.Erros.Count}");
                        Console.WriteLine("🎉 Compilação bem-sucedida! Código sem erros.");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex.Message}");
                Console.WriteLine($"Tipo do erro: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Erro interno: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }
}