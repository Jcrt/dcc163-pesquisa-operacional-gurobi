using System.Collections.Generic;
using System.Linq;

namespace PesquisaOperacional.Gurobi.Core.Model
{
    public class EntradaViewModel
    {
        private IDictionary<string, ProdutoViewModel> _mapeamentoProdutos;

        public EntradaViewModel()
        {
            Produtos = new List<ProdutoViewModel>();
            CargaHorariaDisponivel = new Dictionary<DiaDaSemana, int>();
        }

        public List<ProdutoViewModel> Produtos { get; set; }

        public Dictionary<DiaDaSemana,int> CargaHorariaDisponivel  { get; set; }

        public IDictionary<string, ProdutoViewModel> GetMapeamentoProduto()
        {
            if(_mapeamentoProdutos == null)
                _mapeamentoProdutos = Produtos.ToDictionary(key => key.GetNomeLimpo(), value => value);
            
            return _mapeamentoProdutos;
        }
    }
}
