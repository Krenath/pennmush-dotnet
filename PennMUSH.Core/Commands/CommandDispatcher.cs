namespace PennMUSH.Core.Commands;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ExpressionEvaluator? _evaluator;

    public CommandDispatcher(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public void Register(ICommandHandler handler, params string[] aliases)
    {
        _handlers[handler.Name] = handler;
        foreach (var alias in aliases)
            _handlers[alias] = handler;
    }

    public CommandContext Dispatch(Dbref executor, string input, ObjectDatabase db)
    {
        var context = Parse(executor, input);
        if (_handlers.TryGetValue(context.CommandName, out var handler))
        {
            handler.Execute(context, db);
        }
        else if (!string.IsNullOrEmpty(context.CommandName))
        {
            var execObj = db.GetObject(executor);

            // 1. Check implicit exit matching in executor's location
            if (execObj != null && execObj.Location.IsValid)
            {
                var exitObj = db.FindExit(execObj.Location, context.CommandName);
                if (exitObj != null && _handlers.TryGetValue("go", out var goHandler))
                {
                    var goCtx = new CommandContext
                    {
                        Executor = executor,
                        Enactor = executor,
                        RawCommand = input,
                        CommandName = "go",
                        Arguments = context.CommandName,
                        LeftArg = context.CommandName
                    };
                    goHandler.Execute(goCtx, db);
                    foreach (var note in goCtx.Notifications)
                        context.Notify(note);
                    return context;
                }
            }

            // 2. Check channel prefix (+channel message or +channel/who)
            if (context.CommandName.StartsWith('+') && context.CommandName.Length > 1)
            {
                string chName = context.CommandName.Substring(1);
                if (context.Switches.Count > 0 && _handlers.TryGetValue("@channel", out var chHandler))
                {
                    var chCtx = new CommandContext
                    {
                        Executor = executor,
                        Enactor = executor,
                        RawCommand = input,
                        CommandName = "@channel",
                        Switches = context.Switches,
                        Arguments = chName,
                        LeftArg = chName,
                        RightArg = context.Arguments
                    };
                    chHandler.Execute(chCtx, db);
                    foreach (var note in chCtx.Notifications) context.Notify(note);
                    return context;
                }
                else if (_handlers.TryGetValue("@chat", out var chatHandler))
                {
                    var chatCtx = new CommandContext
                    {
                        Executor = executor,
                        Enactor = executor,
                        RawCommand = input,
                        CommandName = "@chat",
                        LeftArg = chName,
                        RightArg = context.Arguments,
                        Arguments = $"{chName}={context.Arguments}"
                    };
                    chatHandler.Execute(chatCtx, db);
                    foreach (var note in chatCtx.Notifications) context.Notify(note);
                    return context;
                }
            }

            // 3. Check $command:action listeners
            if (execObj != null && TryMatchCommandListener(execObj, input, db, context))
            {
                return context;
            }

            context.Notify("Huh?  (Type \"help\" for help.)");
        }

        return context;
    }

    private bool TryMatchCommandListener(MUSHObject executor, string input, ObjectDatabase db, CommandContext context)
    {
        var candidates = new List<MUSHObject>();

        // Current room
        if (executor.Location.IsValid)
        {
            var room = db.GetObject(executor.Location);
            if (room != null) candidates.Add(room);

            // Objects in room
            foreach (var item in db.GetContents(executor.Location))
            {
                if (item.Id != executor.Id)
                    candidates.Add(item);
            }
        }

        // Executor's inventory
        foreach (var item in db.GetContents(executor.Id))
            candidates.Add(item);

        // Self
        candidates.Add(executor);

        foreach (var candidate in candidates)
        {
            foreach (var attr in candidate.Attributes)
            {
                string val = attr.Value;
                if (!string.IsNullOrEmpty(val) && val.StartsWith('$'))
                {
                    int colon = val.IndexOf(':');
                    if (colon > 1)
                    {
                        string pattern = val.Substring(1, colon - 1).Trim();
                        string action = val.Substring(colon + 1).Trim();

                        if (PatternMatcher.Match(pattern, input, out var captures))
                        {
                            ExecuteListenerAction(candidate, executor, action, captures, db, context);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private void ExecuteListenerAction(MUSHObject listener, MUSHObject enactor, string action, string[] captures, ObjectDatabase db, CommandContext context)
    {
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(listener.Id, enactor.Id, location: listener.Location, args: captures);
            string evaluated = _evaluator.Evaluate(action, evalCtx);
            if (!string.IsNullOrWhiteSpace(evaluated))
            {
                // If the evaluated action starts with an in-game command, dispatch it
                if (evaluated.StartsWith('@') || evaluated.StartsWith('"') || evaluated.StartsWith(':') || evaluated.StartsWith(';'))
                {
                    Dispatch(listener.Id, evaluated, db);
                }
                else
                {
                    context.Notify(evaluated);
                }
            }
        }
        else
        {
            context.Notify(action);
        }
    }

    public static CommandContext Parse(Dbref executor, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new CommandContext { Executor = executor, Enactor = executor };

        string trimmed = input.TrimStart();
        string cmdToken;
        string args = string.Empty;

        // Shortcut prefixes: " for say, : for pose, ; for semipose
        if (trimmed.StartsWith('\"') || trimmed.StartsWith('\''))
        {
            cmdToken = "say";
            args = trimmed.Substring(1);
        }
        else if (trimmed.StartsWith(':') || trimmed.StartsWith(';'))
        {
            cmdToken = "pose";
            args = trimmed.Substring(1);
        }
        else
        {
            int firstSpace = trimmed.IndexOf(' ');
            if (firstSpace == -1)
            {
                cmdToken = trimmed;
            }
            else
            {
                cmdToken = trimmed.Substring(0, firstSpace);
                args = trimmed.Substring(firstSpace + 1).TrimStart();
            }
        }

        var switches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string cmdName = cmdToken;
        if (cmdToken.Contains('/'))
        {
            var parts = cmdToken.Split('/');
            cmdName = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                    switches.Add(parts[i]);
            }
        }

        string left = args;
        string right = string.Empty;
        int eqIndex = args.IndexOf('=');
        if (eqIndex != -1)
        {
            left = args.Substring(0, eqIndex).Trim();
            right = args.Substring(eqIndex + 1).Trim();
        }

        return new CommandContext
        {
            Executor = executor,
            Enactor = executor,
            RawCommand = input,
            CommandName = cmdName,
            Switches = switches,
            Arguments = args,
            LeftArg = left,
            RightArg = right
        };
    }
}
