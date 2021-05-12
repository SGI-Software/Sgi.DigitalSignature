using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sgi.DigitalSignature.Models
{
    public class AssinarDTO
    {
        public string Senha { get; set; }
        public string Local { get; set; }
        public string Razao { get; set; }
        public Dimensao Posicao { private get; set; }
        public Dimensao Tamanho { private get; set; }

        public Dimensao GetPosicao() => Posicao ?? new Dimensao() { X = 300, Y = 10 };
        public Dimensao GetTamanho() => Tamanho ?? new Dimensao() { X = 300, Y = 50 };


        /*
         modelo de json:
        {
            "senha": 1234,
            "local": "Estância Velha",
            "razao": "Testar",
            "posicao": {
                "x": 300,
                "y": 10
            },
            "tamanho": {
                "x": 300,
                "y": 50
            }
        }
         */
    }
}
