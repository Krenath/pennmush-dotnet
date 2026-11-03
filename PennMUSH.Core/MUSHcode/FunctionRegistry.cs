namespace PennMUSH.Core.MUSHcode;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;

public delegate string MUSHFunction(EvaluationContext context, string[] args);

public class FunctionRegistry
{
    private readonly Dictionary<string, MUSHFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    public ObjectDatabase? Database { get; }
    public ExpressionEvaluator? Evaluator { get; set; }

    public FunctionRegistry(ObjectDatabase? database = null)
    {
        Database = database;
        RegisterBuiltins();
    }

    public void Register(string name, MUSHFunction function)
    {
        _functions[name] = function;
    }

    public bool HasFunction(string name) => _functions.ContainsKey(name);

    public string Dispatch(string functionName, EvaluationContext context, string[] args)
    {
        if (_functions.TryGetValue(functionName, out var func))
        {
            return func(context, args);
        }
        return $"#-1 FUNCTION ({functionName.ToUpperInvariant()}) NOT FOUND";
    }

    private void RegisterBuiltins()
    {
        Functions.MathFunctions.Register(this);
        Functions.StringFunctions.Register(this);
        Functions.ListFunctions.Register(this);
        Functions.RegexFunctions.Register(this);
        Functions.TimeFunctions.Register(this);
        Functions.LogicFunctions.Register(this);
        Functions.DbFunctions.Register(this);
        Functions.AsyncFunctions.Register(this);
        Functions.CronFunctions.Register(this);
    }
}