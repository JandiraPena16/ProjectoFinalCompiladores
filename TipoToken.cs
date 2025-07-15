using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPenaCompiler
{
    public enum TipoToken
    {
        // Palavras-chave
        TokenIF,
        TokenELSE,
        TokenWHILE,
        TokenFOR,
        TokenDO,       // do
        TokenFOREACH,  // foreach
        TokenIN,       // in
        TokenRETURN,
        TokenINT,
        TokenFLOAT,
        TokenSTRING,
        TokenBOOL,
        TokenTRUE,
        TokenFALSE,
        TokenVOID,
        TokenSWITCH,
        TokenCASE,
        TokenDEFAULT,
        TokenBREAK,
        TokenCONTINUE,
        TokenNAMESPACE,
        TokenUSING,

        // Modificadores de acesso
        TokenPUBLIC,
        TokenPRIVATE,
        TokenPROTECTED,
        TokenINTERNAL,

        TokenCLASS,



        // Identificadores e literais
        TokenIDENTIFIER,
        TokenNUMBER,
        TokenFLOAT_LITERAL,
        TokenSTRING_LITERAL,
        TokenCHAR_LITERAL,
        TokenBOOLEAN_LITERAL,

        // Operadores
        TokenPLUS,       // +
        TokenMINUS,      // -
        TokenMULTIPLY,   // *
        TokenDIVIDE,     // /
        TokenASSIGN,     // =
        TokenEQUALS,     // ==
        TokenNOT_EQUALS, // !=
        TokenGREATER,    // >
        TokenLESS,       // <
        TokenGREATER_EQUAL, // >=
        TokenLESS_EQUAL,    // <=
        TokenAND,        // &&
        TokenOR,         // ||
        TokenNOT,        // !
        TokenMODULO,     // %
        TokenINCREMENT,  // ++
        TokenDECREMENT,  // --

        // Delimitadores
        TokenSEMICOLON,  // ;
        TokenCOMMA,      // ,
        TokenDOT,        // .
        TokenLPAREN,     // (
        TokenRPAREN,     // )
        TokenLBRACE,     // {
        TokenRBRACE,     // }
        TokenLBRACKET,   // [
        TokenRBRACKET,   // ]
        TokenCOLON,      //:
        TokenAPOSTROPHE, //'
        

        // Outros
        TokenEOF,        // fim de arquivo
        TokenUNKNOWN     // não reconhecido


    }

}
