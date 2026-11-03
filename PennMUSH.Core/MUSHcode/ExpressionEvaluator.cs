namespace PennMUSH.Core.MUSHcode;

using System;
using System.Collections.Generic;
using System.Text;
using PennMUSH.Core.Types;

public delegate string FunctionDispatcher(string functionName, EvaluationContext context, string[] args);

public class ExpressionEvaluator
{
    private readonly FunctionDispatcher? _functionDispatcher;

    public ExpressionEvaluator(FunctionDispatcher? functionDispatcher = null)
    {
        _functionDispatcher = functionDispatcher;
    }

    public string Evaluate(string expression, EvaluationContext context)
    {
        if (string.IsNullOrEmpty(expression))
            return string.Empty;

        if (!context.CheckExecutionBudget(out string? budgetError))
            return budgetError ?? "#-1 EXECUTION HALTED";

        var sb = new StringBuilder();
        int i = 0;

        while (i < expression.Length)
        {
            char c = expression[i];

            if (c == (char)92) // Backslash
            {
                if (i + 1 < expression.Length)
                {
                    sb.Append(expression[i + 1]);
                    i += 2;
                }
                else
                {
                    sb.Append((char)92);
                    i += 1;
                }
            }
            else if (c == (char)123) // '{'
            {
                int closeBrace = FindMatchingDelimiter(expression, i, (char)123, (char)125);
                if (closeBrace != -1)
                {
                    // Literal string within braces: strip outer braces and append verbatim
                    string literal = expression.Substring(i + 1, closeBrace - (i + 1));
                    sb.Append(literal);
                    i = closeBrace + 1;
                }
                else
                {
                    sb.Append((char)123);
                    i += 1;
                }
            }
            else if (c == (char)91) // '['
            {
                int closeBracket = FindMatchingDelimiter(expression, i, (char)91, (char)93);
                if (closeBracket != -1)
                {
                    string inner = expression.Substring(i + 1, closeBracket - (i + 1));
                    sb.Append(EvaluateBracketContent(inner, context));
                    i = closeBracket + 1;
                }
                else
                {
                    sb.Append((char)91);
                    i += 1;
                }
            }
            else if (c == (char)37) // '%'
            {
                if (i + 1 >= expression.Length)
                {
                    sb.Append((char)37);
                    i += 1;
                }
                else
                {
                    char next = expression[i + 1];
                    if (next == (char)37) // %%
                    {
                        sb.Append((char)37);
                        i += 2;
                    }
                    else if (next == (char)114 || next == (char)82) // %r, %R
                    {
                        sb.Append((char)10);
                        i += 2;
                    }
                    else if (next == (char)116 || next == (char)84) // %t, %T
                    {
                        sb.Append((char)9);
                        i += 2;
                    }
                    else if (next == (char)98 || next == (char)66) // %b, %B
                    {
                        sb.Append((char)32);
                        i += 2;
                    }
                    else if (next == (char)35) // %#
                    {
                        sb.Append(context.Enactor.ToString());
                        i += 2;
                    }
                    else if (next == (char)33) // %!
                    {
                        sb.Append(context.Executor.ToString());
                        i += 2;
                    }
                    else if (next == (char)64) // %@
                    {
                        sb.Append(context.Caller.ToString());
                        i += 2;
                    }
                    else if (next == (char)108 || next == (char)76) // %l, %L
                    {
                        sb.Append(context.Location.ToString());
                        i += 2;
                    }
                    else if (char.IsDigit(next)) // %0-%9
                    {
                        sb.Append(context.GetArgument(next - (char)48));
                        i += 2;
                    }
                    else if (next == (char)113 || next == (char)81) // %q, %Q
                    {
                        if (i + 2 < expression.Length && expression[i + 2] == (char)60) // %q<name>
                        {
                            int endTag = expression.IndexOf((char)62, i + 3);
                            if (endTag != -1)
                            {
                                string regName = expression.Substring(i + 3, endTag - (i + 3));
                                sb.Append(context.GetRegister(regName));
                                i = endTag + 1;
                            }
                            else
                            {
                                sb.Append((char)37);
                                i += 1;
                            }
                        }
                        else if (i + 2 < expression.Length && char.IsLetterOrDigit(expression[i + 2])) // %q0, %qa
                        {
                            sb.Append(context.GetRegister(expression[i + 2].ToString()));
                            i += 3;
                        }
                        else
                        {
                            sb.Append((char)37);
                            i += 1;
                        }
                    }
                    else
                    {
                        // Unknown substitution: preserve % and following character literally
                        sb.Append(c);
                        sb.Append(next);
                        i += 2;
                    }
                }
            }
            else
            {
                sb.Append(c);
                i += 1;
            }
        }

        return sb.ToString();
    }

    private string EvaluateBracketContent(string inner, EvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(inner))
            return string.Empty;

        int firstParen = inner.IndexOf((char)40);
        if (firstParen > 0 && inner.EndsWith((char)41))
        {
            string funcName = inner.Substring(0, firstParen).Trim();
            if (IsValidFunctionName(funcName))
            {
                string rawArgs = inner.Substring(firstParen + 1, inner.Length - firstParen - 2);
                string[] parsedArgs = SplitArguments(rawArgs);
                string[] evaluatedArgs = new string[parsedArgs.Length];
                for (int a = 0; a < parsedArgs.Length; a++)
                {
                    evaluatedArgs[a] = Evaluate(parsedArgs[a], context);
                }

                if (_functionDispatcher != null)
                {
                    return _functionDispatcher(funcName, context, evaluatedArgs);
                }

                return $"#-1 FUNCTION ({funcName.ToUpperInvariant()}) NOT FOUND";
            }
        }

        // Fallback: evaluate the inner content as an expression
        return Evaluate(inner, context);
    }

    private static bool IsValidFunctionName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != (char)95)
                return false;
        }
        return true;
    }

    private static string[] SplitArguments(string rawArgs)
    {
        if (string.IsNullOrEmpty(rawArgs))
            return Array.Empty<string>();

        var args = new List<string>();
        var current = new StringBuilder();
        int parenDepth = 0;
        int bracketDepth = 0;
        int braceDepth = 0;

        for (int i = 0; i < rawArgs.Length; i++)
        {
            char c = rawArgs[i];
            if (c == (char)92 && i + 1 < rawArgs.Length)
            {
                current.Append(c);
                current.Append(rawArgs[++i]);
                continue;
            }

            if (c == (char)123) braceDepth++;
            else if (c == (char)125 && braceDepth > 0) braceDepth--;
            else if (c == (char)91) bracketDepth++;
            else if (c == (char)93 && bracketDepth > 0) bracketDepth--;
            else if (c == (char)40) parenDepth++;
            else if (c == (char)41 && parenDepth > 0) parenDepth--;

            if (c == (char)44 && parenDepth == 0 && bracketDepth == 0 && braceDepth == 0)
            {
                args.Add(current.ToString().TrimStart());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        args.Add(current.ToString().TrimStart());
        return args.ToArray();
    }

    private static int FindMatchingDelimiter(string text, int startIndex, char openChar, char closeChar)
    {
        int depth = 0;
        int braceDepth = 0;

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];
            if (c == (char)92 && i + 1 < text.Length)
            {
                i++;
                continue;
            }

            if (openChar != (char)123)
            {
                if (c == (char)123)
                {
                    braceDepth++;
                    continue;
                }
                if (c == (char)125 && braceDepth > 0)
                {
                    braceDepth--;
                    continue;
                }
                if (braceDepth > 0)
                {
                    continue;
                }
            }

            if (c == openChar)
            {
                depth++;
            }
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }
        return -1;
    }
}