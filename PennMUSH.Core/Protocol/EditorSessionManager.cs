namespace PennMUSH.Core.Protocol;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public record EditorSession(
    string SessionId,
    Dbref PlayerId,
    Dbref TargetId,
    string Member,
    string MemberType,
    DateTimeOffset OpenedAt
);

public class EditorSessionManager
{
    private readonly ConcurrentDictionary<string, EditorSession> _sessions = new();

    public EditorOpenPayload? StartAttributeSession(Dbref player, Dbref targetId, string attributeName, ObjectDatabase db)
    {
        var target = db.GetObject(targetId);
        if (target == null) return null;

        var user = db.GetObject(player);
        if (user == null) return null;

        // Permission check: player must control target or be wizard
        if (!user.IsWizard && !user.IsGod && target.Owner != player)
        {
            return null;
        }

        string sessionId = "edit_" + Guid.NewGuid().ToString("N")[..12];
        var session = new EditorSession(
            sessionId,
            player,
            targetId,
            attributeName.ToUpperInvariant(),
            "attribute",
            DateTimeOffset.UtcNow
        );

        _sessions[sessionId] = session;

        string existingCode = target.GetAttribute(attributeName)?.Value ?? string.Empty;
        var headers = new List<string>
        {
            $"Target: {target.Name} (#{target.Id.Value})",
            $"Attribute: {attributeName.ToUpperInvariant()}",
            $"Owner: #{target.Owner.Value}",
            $"Type: {target.Type}",
            $"Modified: {target.ModificationTime:u}"
        };

        return new EditorOpenPayload(
            SessionId: sessionId,
            Target: new EditorTarget(target.Id.ToString(), target.Name, attributeName.ToUpperInvariant(), "attribute"),
            Language: "mushcode",
            Title: $"Editing {target.Name}/{attributeName}",
            Headers: headers.ToArray(),
            Code: existingCode,
            ReadOnly: false
        );
    }

    public EditorResultPayload SaveSession(string sessionId, string newCode, ObjectDatabase db, Dbref saver)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return new EditorResultPayload(
                sessionId,
                false,
                new[] { new EditorDiagnostic(1, 1, "Editor session expired or not found.", "error") }
            );
        }

        var target = db.GetObject(session.TargetId);
        if (target == null)
        {
            return new EditorResultPayload(
                sessionId,
                false,
                new[] { new EditorDiagnostic(1, 1, "Target object no longer exists.", "error") }
            );
        }

        var user = db.GetObject(saver);
        if (user == null || (!user.IsWizard && !user.IsGod && target.Owner != saver && session.PlayerId != saver))
        {
            return new EditorResultPayload(
                sessionId,
                false,
                new[] { new EditorDiagnostic(1, 1, "Permission denied.", "error") }
            );
        }

        // Basic MUSHcode bracket/brace balance check for diagnostics
        var diagnostics = ValidateMushcode(newCode);

        // Apply attribute
        var code = target.SetAttribute(session.Member, newCode, saver);

        bool success = (code == AttributeErrorCode.Okay);
        if (!success)
        {
            return new EditorResultPayload(
                sessionId,
                false,
                new[] { new EditorDiagnostic(1, 1, $"Failed to set attribute: {code}", "error") }
            );
        }

        return new EditorResultPayload(sessionId, true, diagnostics.ToArray());
    }

    public bool CloseSession(string sessionId) => _sessions.TryRemove(sessionId, out _);

    private static List<EditorDiagnostic> ValidateMushcode(string code)
    {
        var list = new List<EditorDiagnostic>();
        int braces = 0;
        int brackets = 0;
        int parens = 0;

        int line = 1;
        int col = 1;

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (c == '\n')
            {
                line++;
                col = 1;
                continue;
            }

            if (c == '{') braces++;
            else if (c == '}')
            {
                braces--;
                if (braces < 0)
                {
                    list.Add(new EditorDiagnostic(line, col, "Unmatched closing brace '}'", "warning"));
                    braces = 0;
                }
            }
            else if (c == '[') brackets++;
            else if (c == ']')
            {
                brackets--;
                if (brackets < 0)
                {
                    list.Add(new EditorDiagnostic(line, col, "Unmatched closing bracket ']'", "warning"));
                    brackets = 0;
                }
            }
            else if (c == '(') parens++;
            else if (c == ')')
            {
                parens--;
                if (parens < 0)
                {
                    list.Add(new EditorDiagnostic(line, col, "Unmatched closing parenthesis ')'", "warning"));
                    parens = 0;
                }
            }

            col++;
        }

        if (braces > 0)
            list.Add(new EditorDiagnostic(line, col, $"Unclosed '{braces}' brace(s) '{{'", "warning"));
        if (brackets > 0)
            list.Add(new EditorDiagnostic(line, col, $"Unclosed '{brackets}' bracket(s) '['", "warning"));
        if (parens > 0)
            list.Add(new EditorDiagnostic(line, col, $"Unclosed '{parens}' parenthesis ')'", "warning"));

        return list;
    }
}
