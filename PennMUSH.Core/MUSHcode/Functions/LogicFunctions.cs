namespace PennMUSH.Core.MUSHcode.Functions;

using System;

public static class LogicFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("T", T);
        registry.Register("IF", If);
        registry.Register("IFEQ", Ifeq);
        registry.Register("CAND", Cand);
        registry.Register("COR", Cor);
        registry.Register("SETQ", Setq);
        registry.Register("R", R);
    }

    public static bool IsTruthy(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s == "0") return false;
        if (s.StartsWith("#-", StringComparison.Ordinal)) return false;
        return true;
    }

    public static string T(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        return IsTruthy(args[0]) ? "1" : "0";
    }

    public static string If(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        bool cond = IsTruthy(args[0]);
        if (cond)
            return args.Length > 1 ? args[1] : string.Empty;
        else
            return args.Length > 2 ? args[2] : string.Empty;
    }

    public static string Ifeq(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        bool eq = string.Equals(args[0], args[1], StringComparison.OrdinalIgnoreCase);
        if (eq)
            return args.Length > 2 ? args[2] : string.Empty;
        else
            return args.Length > 3 ? args[3] : string.Empty;
    }

    public static string Cand(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "1";
        foreach (var a in args)
        {
            if (!IsTruthy(a)) return "0";
        }
        return "1";
    }

    public static string Cor(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        foreach (var a in args)
        {
            if (IsTruthy(a)) return "1";
        }
        return "0";
    }

    public static string Setq(EvaluationContext ctx, string[] args)
    {
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            ctx.SetRegister(args[i].Trim(), args[i + 1]);
        }
        return string.Empty;
    }

    public static string R(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return ctx.GetRegister(args[0].Trim());
    }
}