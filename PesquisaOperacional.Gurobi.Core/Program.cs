/* Copyright 2020, Gurobi Optimization, LLC */

/* This example reads an LP model from a file and solves it.
   If the model is infeasible or unbounded, the example turns off
   presolve and solves the model again. If the model is infeasible,
   the example computes an Irreducible Inconsistent Subsystem (IIS),
   and writes it to a file. */

using System;
using Gurobi;
using PesquisaOperacional.Gurobi.Core.Model;
using PesquisaOperacional.Gurobi.Core.Solver;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var _solver = new GurobiSolver();
            _solver.Run(new EntradaViewModel());
        }
        catch (GRBException e)
        {
            Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
        }
    }
}
