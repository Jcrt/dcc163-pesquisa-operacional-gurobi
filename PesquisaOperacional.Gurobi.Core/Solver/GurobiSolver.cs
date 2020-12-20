using Gurobi;
using PesquisaOperacional.Gurobi.Core.Model;
using System;
using System.Collections.Generic;

namespace PesquisaOperacional.Gurobi.Core.Solver
{
    public class GurobiSolver
    {
        readonly GRBModel _grbModel;
        readonly IDictionary<string, GRBVar> _varArray;

        public GurobiSolver()
        {
            var grbEnv = new GRBEnv();
            _grbModel = new GRBModel(grbEnv);
            _varArray = new Dictionary<string, GRBVar>();
        }

        public void Run(EntradaViewModel entrada)
        {
            CriaVariaveis(entrada);
            CriaFuncaoObjetivo(entrada);
            CriaRestricaoHR(entrada);
            CriaRestricaoHE(entrada);
            CriaRestricaoDemandaMinima(entrada);
            OtimizaModelo();
        }

        private void OtimizaModelo()
        {
            _grbModel.Optimize();
        }

        #region Função Objetivo

        private void CriaFuncaoObjetivo(EntradaViewModel entrada)
        {
            var expressaoObjetivo = new GRBLinExpr();

            entrada.Produtos.ForEach(item =>
            {
                var custoHR = item.CustoRegular;
                var custoHE = item.CustoHoraExtra;

                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var diaSemanaEnum = (DiaDaSemana)diaSemana;
                    
                    var qtdeProdutoHR = _varArray[item.GetNomeVariavel(diaSemanaEnum)];
                    expressaoObjetivo.AddTerm(custoHR, qtdeProdutoHR);
                    
                    var qtdeProdutoHE = _varArray[item.GetNomeVariavel(diaSemanaEnum, true)];
                    expressaoObjetivo.AddTerm(custoHE, qtdeProdutoHE);
                }
            });
            
            _grbModel.SetObjective(expressaoObjetivo, GRB.MINIMIZE);
        }

        #endregion Função Objetivo

        #region Restrições

        private void CriaRestricaoHE(EntradaViewModel entrada)
        {
            foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
            {
                var expressaoLinear = new GRBLinExpr();

                var diaSemanaEnum = (DiaDaSemana)diaSemana;

                var cargaHorariaExtraDisponivelDiaria = entrada.CargaHorariaExtraDisponivel[diaSemanaEnum];

                entrada.Produtos.ForEach(produto =>
                {
                    var taxaProducao = produto.GetTaxaUnidadeHora();
                    var variavelProduto = _varArray[produto.GetNomeVariavel(diaSemanaEnum, true)];

                    expressaoLinear.AddTerm(taxaProducao, variavelProduto);
                });

                _grbModel.AddConstr(
                    expressaoLinear, 
                    GRB.LESS_EQUAL, 
                    cargaHorariaExtraDisponivelDiaria, 
                    $"HorasExtrasDisponiveis[{diaSemana}]"
                );
            }
        }

        private void CriaRestricaoHR(EntradaViewModel entrada)
        {
            foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
            {
                var expressaoLinear = new GRBLinExpr();

                var diaSemanaEnum = (DiaDaSemana)diaSemana;

                var cargaHorariaDisponivelDiaria = entrada.CargaHorariaDisponivel[diaSemanaEnum];

                entrada.Produtos.ForEach(produto =>
                {
                    var taxaProducao = produto.GetTaxaUnidadeHora();
                    var variavelProduto = _varArray[produto.GetNomeVariavel(diaSemanaEnum)];

                    expressaoLinear.AddTerm(taxaProducao, variavelProduto);
                });

                _grbModel.AddConstr(
                    expressaoLinear, 
                    GRB.EQUAL, 
                    cargaHorariaDisponivelDiaria, 
                    $"HorasRegularesDisponiveis[{diaSemana}]"
                );
            }
        }

        private void CriaRestricaoDemandaMinima(EntradaViewModel entrada)
        {
            entrada.Produtos.ForEach(item =>
            {
                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var diaSemanaEnum = (DiaDaSemana)diaSemana;
                    var qtdeProdutoHR = _varArray[item.GetNomeVariavel(diaSemanaEnum)];
                    var qtdeProdutoHE = _varArray[item.GetNomeVariavel(diaSemanaEnum, true)];
                    var demandaProdutoDia = item.Demanda[diaSemanaEnum];

                    _grbModel.AddConstr(
                        qtdeProdutoHE + qtdeProdutoHR, 
                        GRB.GREATER_EQUAL, 
                        demandaProdutoDia, 
                        $"DemandaMinima[{item.Nome}][{diaSemanaEnum}]"
                    );
                }
            });
        }

        #endregion Restrições

        #region Variáveis

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

        #endregion Variáveis
    }
}
