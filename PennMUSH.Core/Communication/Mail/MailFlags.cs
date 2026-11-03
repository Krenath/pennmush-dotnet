namespace PennMUSH.Core.Communication.Mail;

using System;

[Flags]
public enum MailFlags
{
    None = 0,
    Read = 1 << 0,
    Cleared = 1 << 1,
    Tagged = 1 << 2,
    Urgent = 1 << 3
}
