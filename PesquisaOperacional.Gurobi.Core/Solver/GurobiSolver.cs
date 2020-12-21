using Gurobi;
using PesquisaOperacional.Gurobi.Core.Model;
using System;
using System.Collections.Generic;

namespace PesquisaOperacional.Gurobi.Core.Solver
{
    public class GurobiSolver
    {
        readonly GRBModel _grbModel;

        /// <summary>
        /// Armazena a relação de variáveis do Gurobi por nome
        /// </summary>
        readonly IDictionary<string, GRBVar> _varArray;

        public GurobiSolver()
        {
            var grbEnv = new GRBEnv();
            _grbModel = new GRBModel(grbEnv);
            _varArray = new Dictionary<string, GRBVar>();
        }

        /// <summary>
        /// Executa os passos da criação e resolução do modelo
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        /// <returns>Instancia de <see cref="SaidaViewModel"/> com a quantidade de produção diária para atender a demanda</returns>
        public SaidaViewModel Run(EntradaViewModel entrada)
        {
            CriaVariaveis(entrada);
            
            CriaFuncaoObjetivo(entrada);
            
            CriaRestricaoHR(entrada);
            
            CriaRestricaoHE(entrada);
            
            CriaRestricaoDemandaMinima(entrada);
            
            OtimizaModelo();

            return GeraSaidaViewModel(entrada);
        }

        #region Função Objetivo

        /// <summary>
        /// Cria função objetivo
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        private void CriaFuncaoObjetivo(EntradaViewModel entrada)
        {
            //Cria objeto de expressão linear para podermos gerar uma expressão linear de forma dinâmica
            var expressaoObjetivo = new GRBLinExpr();

            entrada.Produtos.ForEach(produto =>
            {
                var custoHR = produto.CustoRegular;
                var custoHE = produto.CustoHoraExtra;

                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var diaSemanaEnum = (DiaDaSemana)diaSemana;
                    
                    var qtdeProdutoHR = _varArray[produto.GetNomeVariavel(diaSemanaEnum)];
                    expressaoObjetivo.AddTerm(custoHR, qtdeProdutoHR);
                    
                    var qtdeProdutoHE = _varArray[produto.GetNomeVariavel(diaSemanaEnum, true)];
                    expressaoObjetivo.AddTerm(custoHE, qtdeProdutoHE);
                }
            });
            
            _grbModel.SetObjective(expressaoObjetivo, GRB.MINIMIZE);
        }

        #endregion Função Objetivo

        #region Restrições

        /// <summary>
        /// Cria todas as restrições relativas às horas regulares de trabalho disponíveis
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        private void CriaRestricaoHE(EntradaViewModel entrada)
        {
            foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
            {
                var diaSemanaEnum = (DiaDaSemana)diaSemana;

                if (diaSemanaEnum == DiaDaSemana.Domingo)
                    continue;

                var expressaoLinear = new GRBLinExpr();

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

        /// <summary>
        /// Cria todas as restrições relativas às horas extras de trabalho disponíveis
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        private void CriaRestricaoHR(EntradaViewModel entrada)
        {
            foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
            {
                var diaSemanaEnum = (DiaDaSemana)diaSemana;
                if (diaSemanaEnum == DiaDaSemana.Domingo)
                    continue;

                var expressaoLinear = new GRBLinExpr();


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

        /// <summary>
        /// Cria todas as restrições relativas às demandas mínimas de cada produto por dia
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        private void CriaRestricaoDemandaMinima(EntradaViewModel entrada)
        {
            entrada.Produtos.ForEach(item =>
            {
                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var diaSemanaEnum = (DiaDaSemana)diaSemana;

                    if (diaSemanaEnum == DiaDaSemana.Domingo)
                        continue;

                    var qtdeProdutoHR = _varArray[item.GetNomeVariavel(diaSemanaEnum)];
                    var qtdeProdutoHE = _varArray[item.GetNomeVariavel(diaSemanaEnum, true)];
                    var qtdeProdutoExcesso = _varArray[item.GetNomeVariavel(diaSemanaEnum, isExcesso: true)];
                    var demandaProdutoDia = item.Demanda[diaSemanaEnum];
                    var diaAnteriorEnum = EnumHelper.DiaAnterior(diaSemanaEnum);
                    var qtdeProdutoExcessoDiaAnterior = _varArray[item.GetNomeVariavel(diaAnteriorEnum, isExcesso: true)];

                    _grbModel.AddConstr(
                        qtdeProdutoExcessoDiaAnterior + qtdeProdutoHE + qtdeProdutoHR - qtdeProdutoExcesso, 
                        GRB.GREATER_EQUAL, 
                        demandaProdutoDia, 
                        $"DemandaMinima[{item.Nome}][{diaSemanaEnum}]"
                    );
                }
            });
        }

        #endregion Restrições

        #region Variáveis

        /// <summary>
        /// Cria todas as variáveis do problema, que são a quantidade de produto que deve ser feita por dia, tanto em horas 
        /// regulares quanto em horas extra
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        void CriaVariaveis(EntradaViewModel entrada)
        {
            entrada.Produtos.ForEach(item => {

                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var produtoHR = item.GetNomeVariavel((DiaDaSemana)diaSemana, false);
                    var produtoHE = item.GetNomeVariavel((DiaDaSemana)diaSemana, true);
                    var produtoExcesso = item.GetNomeVariavel((DiaDaSemana)diaSemana, isExcesso: true);

                    _varArray.Add(produtoHR, _grbModel.AddVar(0, double.MaxValue, 1, GRB.INTEGER, produtoHR));
                    _varArray.Add(produtoHE, _grbModel.AddVar(0, double.MaxValue, 1, GRB.INTEGER, produtoHE));
                    _varArray.Add(produtoExcesso, _grbModel.AddVar(0, double.MaxValue, 1, GRB.INTEGER, produtoExcesso));
                }
            });
        }

        #endregion Variáveis

        /// <summary>
        /// Executa a otimização do modelo 
        /// </summary>
        private void OtimizaModelo()
        {
            _grbModel.Optimize();
        }

        /// <summary>
        /// Gera a saída do problema para escrita na planilha
        /// </summary>
        /// <param name="entrada">Instância de <see cref="EntradaViewModel"/> contendo os dados informados na planilha de dados</param>
        /// <returns>Instancia de <see cref="SaidaViewModel"/> com a quantidade de produção diária para atender a demanda</returns>
        private SaidaViewModel GeraSaidaViewModel(EntradaViewModel entrada)
        {
            var saidaViewModel = new SaidaViewModel
            {
                Produtos = entrada.Produtos
            };

            saidaViewModel.Produtos.ForEach(produto =>
            {
                foreach (var diaSemana in Enum.GetValues(typeof(DiaDaSemana)))
                {
                    var diaSemanaEnum = (DiaDaSemana)diaSemana;
                    var producaoHR = Convert.ToInt32(_grbModel.GetVarByName(produto.GetNomeVariavel(diaSemanaEnum)).X);
                    var producaoHE = Convert.ToInt32(_grbModel.GetVarByName(produto.GetNomeVariavel(diaSemanaEnum, true)).X);
                    var excesso = Convert.ToInt32(_grbModel.GetVarByName(produto.GetNomeVariavel(diaSemanaEnum, isExcesso:true)).X);

                    produto.Producao[diaSemanaEnum] = producaoHR + producaoHE;
                    produto.Excesso[diaSemanaEnum] = excesso;
                }
            });

            return saidaViewModel;
        }
    }
}
