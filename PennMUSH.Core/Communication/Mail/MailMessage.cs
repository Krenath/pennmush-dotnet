namespace PennMUSH.Core.Communication.Mail;

using System;
using PennMUSH.Core.Types;

public class MailMessage
{
    public int Id { get; init; }
    public Dbref Sender { get; init; }
    public Dbref Recipient { get; init; }
    public string Subject { get; set; } = "(No Subject)";
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public int Folder { get; set; } = 0;
    public MailFlags Flags { get; set; } = MailFlags.None;

    public bool IsRead => Flags.HasFlag(MailFlags.Read);
    public bool IsCleared => Flags.HasFlag(MailFlags.Cleared);
    public bool IsTagged => Flags.HasFlag(MailFlags.Tagged);
    public bool IsUrgent => Flags.HasFlag(MailFlags.Urgent);
}
