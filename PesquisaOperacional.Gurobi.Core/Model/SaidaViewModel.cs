using System;
using System.Collections.Generic;

namespace PesquisaOperacional.Gurobi.Core.Model
{
    public class SaidaViewModel
    {
        public SaidaViewModel()
        {
            TeveHoraExtra = new Dictionary<DiaDaSemana, bool>();
            foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
            {
                var diaSemanaEnum = (DiaDaSemana)diaSemana;
                TeveHoraExtra[diaSemanaEnum] = false;
            }
        }

        public List<ProdutoViewModel> Produtos { get; set; }

        public Dictionary<DiaDaSemana, bool> TeveHoraExtra { get; set; }
    }
}
