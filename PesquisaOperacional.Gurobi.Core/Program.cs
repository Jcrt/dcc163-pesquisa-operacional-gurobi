using ExcelDataReader;
using Gurobi;
using OfficeOpenXml;
using PesquisaOperacional.Gurobi.Core.Model;
using PesquisaOperacional.Gurobi.Core.Solver;
using System;
using System.Data;
using System.IO;
using System.Text;

class Program
{
    private static string _inputPath = "..\\..\\..\\data\\input.xlsx";
    private static string _outputPath = "..\\..\\..\\data\\output.xlsx";
    private static EntradaViewModel _entradaViewModel;
    private static SaidaViewModel _saidaViewModel;

    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando otimização!");

        CarregaInformacoes();
        ExecutaOtimizacao();
        GravaResultadoOtimizacao();

        Console.WriteLine("Acesse o arquivo output.xlsx para ver o resultado da otimização");
        Console.WriteLine("Aperte qualquer tecla para encerrar...");
        Console.ReadKey();
    }

    #region Leitura dos dados

    /// <summary>
    /// Carrega informações dos arquivo input.xlsx
    /// </summary>
    private static void CarregaInformacoes()
    {
        Console.WriteLine("Iniciando leitura dos dados");
        _entradaViewModel = new EntradaViewModel();

        using (var stream = File.Open(_inputPath, FileMode.Open, FileAccess.Read))
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet();

                LeDadosDaEquipe(result.Tables[0]);
                LeDadosDosProdutos(result.Tables[0]);
            }
        }
        Console.WriteLine("Leitura dos dados encerrada");
    }

    /// <summary>
    /// Le as informações a respeito de carga horária da equipe
    /// </summary>
    /// <param name="dataSet"></param>
    private static void LeDadosDaEquipe(DataTable dataSet)
    {
        Console.WriteLine("Iniciando leitura dos dados da equipe");
        for (int i = 1; i < 7; i++)
        {
            var horaRegular = dataSet.Rows[2].ItemArray[i];
            var horaExtra = dataSet.Rows[3].ItemArray[i];
            _entradaViewModel.CargaHorariaDisponivel.Add((DiaDaSemana)i, int.Parse(horaRegular.ToString()));
            _entradaViewModel.CargaHorariaExtraDisponivel.Add((DiaDaSemana)i, int.Parse(horaExtra.ToString()));
        }
        Console.WriteLine("Leitura dos dados da equipe encerrada");
    }

    /// <summary>
    /// Le as informações dos produtos a serem produzidos
    /// </summary>
    /// <param name="dataSet"></param>
    private static void LeDadosDosProdutos(DataTable dataSet)
    {

        Console.WriteLine("Iniciando leitura dos dados dos produtos");

        for (int i = 7; i < dataSet.Rows.Count; i++)
        {
            var produto = new ProdutoViewModel();
            produto.Nome = dataSet.Rows[i].ItemArray[0].ToString();
            produto.TaxaProducao = float.Parse(dataSet.Rows[i].ItemArray[1].ToString());
            for (int j = 2; j < 8; j++)
            {
                var item = dataSet.Rows[i].ItemArray[j];
                produto.Demanda.Add((DiaDaSemana)j - 1, int.Parse(item.ToString()));
            }
            produto.CustoRegular = float.Parse(dataSet.Rows[i].ItemArray[8].ToString());
            produto.CustoHoraExtra = float.Parse(dataSet.Rows[i].ItemArray[9].ToString());
            _entradaViewModel.Produtos.Add(produto);
        }
        Console.WriteLine("Leitura dos dados dos produtos encerrada");
    }

    #endregion

    /// <summary>
    /// Executa a otimização
    /// </summary>
    private static void ExecutaOtimizacao()
    {
        try
        {
            var _solver = new GurobiSolver();
            _saidaViewModel = _solver.Run(_entradaViewModel);
        }
        catch (GRBException e)
        {
            Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
        }
    }

    /// <summary>
    /// Grava o resultado da otimização em arquivo
    /// </summary>
    private static void GravaResultadoOtimizacao()
    {
        File.Delete(_outputPath);
        FileInfo saida = new FileInfo(_outputPath);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        ExcelPackage pck = new ExcelPackage(saida);

        var planilhaSaida = pck.Workbook.Worksheets.Add("Saida");
        planilhaSaida.Cells["A1"].Value = "Produto";
        planilhaSaida.Cells["B1"].Value = "Produção segunda";
        planilhaSaida.Cells["C1"].Value = "Excesso segunda";
        planilhaSaida.Cells["D1"].Value = "Necessário hora extra segunda";
        planilhaSaida.Cells["E1"].Value = "Produção terça";
        planilhaSaida.Cells["F1"].Value = "Excesso terça";
        planilhaSaida.Cells["G1"].Value = "Necessário hora extra terça";
        planilhaSaida.Cells["H1"].Value = "Produção quarta";
        planilhaSaida.Cells["I1"].Value = "Excesso quarta";
        planilhaSaida.Cells["J1"].Value = "Necessário hora extra quarta";
        planilhaSaida.Cells["K1"].Value = "Produção quinta";
        planilhaSaida.Cells["L1"].Value = "Excesso quinta";
        planilhaSaida.Cells["M1"].Value = "Necessário hora extra quinta";
        planilhaSaida.Cells["N1"].Value = "Produção sexta";
        planilhaSaida.Cells["O1"].Value = "Excesso sexta";
        planilhaSaida.Cells["P1"].Value = "Necessário hora extra sexta";
        planilhaSaida.Cells["Q1"].Value = "Produção sábado";
        planilhaSaida.Cells["R1"].Value = "Excesso sábado";
        planilhaSaida.Cells["S1"].Value = "Necessário hora extra sábado";
        planilhaSaida.Cells["A1:S1"].Style.Font.Bold = true;

        var linha = 2;

        foreach (var produto in _saidaViewModel.Produtos)
        {
            planilhaSaida.Cells["A" + linha].Value = produto.Nome;
            planilhaSaida.Cells["B" + linha].Value = produto.Producao[DiaDaSemana.Segunda];
            planilhaSaida.Cells["C" + linha].Value = produto.Excesso[DiaDaSemana.Segunda];
            planilhaSaida.Cells["D" + linha].Value = _saidaViewModel.TeveHoraExtra[DiaDaSemana.Segunda] ? "Sim" : "Não";
            planilhaSaida.Cells["E" + linha].Value = produto.Producao[DiaDaSemana.Terça];
            planilhaSaida.Cells["F" + linha].Value = produto.Excesso[DiaDaSemana.Terça];
            planilhaSaida.Cells["G" + linha].Value = _saidaViewModel.TeveHoraExtra[DiaDaSemana.Terça] ? "Sim" : "Não";
            planilhaSaida.Cells["H" + linha].Value = produto.Producao[DiaDaSemana.Quarta];
            planilhaSaida.Cells["I" + linha].Value = produto.Excesso[DiaDaSemana.Quarta];
            planilhaSaida.Cells["J" + linha].Value = _saidaViewModel.TeveHoraExtra[DiaDaSemana.Quarta] ? "Sim" : "Não";
            planilhaSaida.Cells["K" + linha].Value = produto.Producao[DiaDaSemana.Quinta];
            planilhaSaida.Cells["L" + linha].Value = produto.Excesso[DiaDaSemana.Quinta];
            planilhaSaida.Cells["M" + linha].Value = _saidaViewModel.TeveHoraExtra[DiaDaSemana.Quinta] ? "Sim" : "Não";
            planilhaSaida.Cells["N" + linha].Value = produto.Producao[DiaDaSemana.Sexta];
            planilhaSaida.Cells["O" + linha].Value = produto.Excesso[DiaDaSemana.Sexta];
            planilhaSaida.Cells["P" + linha].Value = _saidaViewModel.TeveHoraExtra[DiaDaSemana.Sexta] ? "Sim" : "Não";
            planilhaSaida.Cells["Q" + linha].Value = produto.Producao[DiaDaSemana.Sábado];
            planilhaSaida.Cells["R" + linha].Value = produto.Excesso[DiaDaSemana.Sábado];
            planilhaSaida.Cells["S" + linha].Value = _saidaViewModel.TeveHoraExtra[DiaDaSemana.Sábado] ? "Sim" : "Não";

            linha++;
        }

        pck.Save();
    }
}
