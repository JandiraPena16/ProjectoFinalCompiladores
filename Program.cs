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
                            return 2 + 2
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
                            Console.WriteLine(""Olá, mundo!"")
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
                // Análise léxica
                Lexer lexer = new Lexer(codigoFonte);
                List<Token> tokens = lexer.Tokenizar();

                Console.WriteLine("Tokens Reconhecidos:");
                foreach (var token in tokens)
                {
                    Console.WriteLine($"[Linha {token.Linha}] Tipo: {token.Tipo}, Valor: '{token.Lexema}'");
                }


                Console.WriteLine("-------------Lista de Erros---------------");
                // Análise sintática
                Parser parser = new Parser(tokens);
                parser.Analisar();

                // Relatório de erros
                if (parser.Erros.Count > 0)
                {
                    Console.WriteLine("\nErros Sintáticos Encontrados:");
                    foreach (var erro in parser.Erros)
                    {
                        Console.WriteLine(erro);
                    }
                }
                else
                {
                    Console.WriteLine("\nAnálise sintática concluída com sucesso. Nenhum erro encontrado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex.Message}");
            }

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }
}
