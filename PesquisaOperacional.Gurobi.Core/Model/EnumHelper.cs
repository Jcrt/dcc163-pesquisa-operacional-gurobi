namespace PesquisaOperacional.Gurobi.Core.Model
{
    public static class EnumHelper
    {

        /// <summary>
        /// Retorna o dia anterior
        /// </summary>
        /// <param name="diaDaSemana"></param>
        /// <returns></returns>
        public static DiaDaSemana DiaAnterior(this DiaDaSemana diaDaSemana)
        {
            var indice = (int)diaDaSemana;
            indice--;
            return (DiaDaSemana)indice;
        }
    }
}
