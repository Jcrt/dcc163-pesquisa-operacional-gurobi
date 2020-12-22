using System;
using System.Collections.Generic;

namespace PesquisaOperacional.Gurobi.Core.Model
{
    public class ProdutoViewModel
    {
        public ProdutoViewModel()
        {
            Demanda = new Dictionary<DiaDaSemana, int>();
            Producao = new Dictionary<DiaDaSemana, int>();
            Excesso = new Dictionary<DiaDaSemana, int>();
        }

        public string Nome { get; set; }

        public float TaxaProducao { get; set; }

        public Dictionary<DiaDaSemana, int> Demanda { get; set; }

        public float CustoRegular { get; set; }

        public float CustoHoraExtra { get; set; }

        public double GetTaxaUnidadeHora()
        {
            if (TaxaProducao <= 0.0f)
                throw new InvalidOperationException($"A propriedade {nameof(TaxaProducao)} não pode ser 0");

            return 1 / TaxaProducao;
        }

        public string GetNomeVariavel(DiaDaSemana diaSemana, bool isHoraExtra = false, bool isExcesso = false)
        {
            string modificador = string.Empty;
            var tipoHora = !isHoraExtra ? "HR" : "HE";
            if (isExcesso)
                modificador = "excesso";
            else
                modificador = tipoHora;

            return $"{GetNomeLimpo()}_{modificador}_{diaSemana.ToString().ToUpper()}";
        }

        public string GetNomeLimpo()
        {
            return Nome.Trim().ToLowerInvariant();
        }

        public Dictionary<DiaDaSemana, int> Producao { get; set; }
        public Dictionary<DiaDaSemana, int> Excesso { get; set; }
    }
}
