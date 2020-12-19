using Gurobi;
using PesquisaOperacional.Gurobi.Core.Model;
using System;
using System.Collections.Generic;

namespace PesquisaOperacional.Gurobi.Core.Solver
{
    public class GurobiSolver
    {
        readonly GRBEnv _grbEnv;
        readonly GRBModel _grbModel;
        private readonly IDictionary<string, GRBVar> _varArray;

        public GurobiSolver()
        {
            _grbEnv = new GRBEnv();
            _grbModel = new GRBModel(_grbEnv);
            _varArray = new Dictionary<string, GRBVar>();
        }

        public void Run(EntradaViewModel entrada)
        {
            CriaVariaveis(entrada);
            CriaRestricaoHR(entrada);
        }

        private void CriaRestricaoHR(EntradaViewModel entrada)
        {
            foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
            {
                var diaSemanaEnum = (DiaDaSemana)diaSemana;

                var cargaHorariaDisponivelDiaria = entrada.CargaHorariaDisponivel[diaSemanaEnum];

                foreach (var produto in entrada.Produtos)
                {
                    var taxaProducao = produto.GetTaxaUnidadeHora();
                    var variavelProduto = _varArray[produto.GetNomeVariavel(diaSemanaEnum)];

                }
            }
        }

        void CriaVariaveis(EntradaViewModel entrada)
        {
            entrada.Produtos.ForEach(item => {

                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var produtoHR = item.GetNomeVariavel((DiaDaSemana)diaSemana, false);
                    var produtoHE = item.GetNomeVariavel((DiaDaSemana)diaSemana, true);

                    _varArray.Add(produtoHR, _grbModel.AddVar(0, double.MaxValue, 1, GRB.INTEGER, produtoHR));
                    _varArray.Add(produtoHE, _grbModel.AddVar(0, double.MaxValue, 1, GRB.INTEGER, produtoHE));
                }
            });
        }
    }
}
