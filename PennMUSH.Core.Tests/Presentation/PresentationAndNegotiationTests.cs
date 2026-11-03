namespace PennMUSH.Core.Tests.Presentation;

using System;
using System.Text;
using Xunit;
using PennMUSH.Core.Presentation;

public class PresentationAndNegotiationTests
{
    [Fact]
    public void TelnetNegotiator_InitialHandshake_SendsExpectedTelopts()
    {
        var negotiator = new TelnetNegotiator();
        var handshake = negotiator.GetInitialHandshake();

        Assert.Contains(TelnetConstants.IAC, handshake);
        Assert.Contains(TelnetConstants.TELOPT_NAWS, handshake);
        Assert.Contains(TelnetConstants.TELOPT_TTYPE, handshake);
        Assert.Contains(TelnetConstants.TELOPT_SGA, handshake);
    }

    [Fact]
    public void TelnetNegotiator_NAWS_UnpacksWindowDimensions()
    {
        var capabilities = new ClientCapabilities();
        var negotiator = new TelnetNegotiator(capabilities);

        // IAC SB NAWS 0 120 0 45 IAC SE
        byte[] nawsBytes = new byte[]
        {
            TelnetConstants.IAC, TelnetConstants.SB, TelnetConstants.TELOPT_NAWS,
            0, 120, 0, 45,
            TelnetConstants.IAC, TelnetConstants.SE
        };

        string remainingText = negotiator.ProcessInput(nawsBytes);
        Assert.Empty(remainingText);
        Assert.Equal(120, capabilities.TerminalWidth);
        Assert.Equal(45, capabilities.TerminalHeight);
    }

    [Fact]
    public void TelnetNegotiator_PuebloIdentification_EnablesHtmlMode()
    {
        var capabilities = new ClientCapabilities();
        var negotiator = new TelnetNegotiator(capabilities);

        byte[] input = Encoding.ASCII.GetBytes("connect Guest PUEBLOCLIENT\r\n");
        string filtered = negotiator.ProcessInput(input);

        Assert.True(capabilities.SupportsPueblo);
        Assert.True(capabilities.SupportsHtml);
        Assert.Equal("connect Guest", filtered.Trim());
    }

    [Fact]
    public void TelnetNegotiator_TTYPE_DetectsColorCapabilities()
    {
        var capabilities = new ClientCapabilities();
        var negotiator = new TelnetNegotiator(capabilities);

        // IAC SB TTYPE 0 "xterm-256color" IAC SE
        byte[] ttype256 = new byte[]
        {
            TelnetConstants.IAC, TelnetConstants.SB, TelnetConstants.TELOPT_TTYPE, 0
        };
        byte[] ttypePayload = Encoding.ASCII.GetBytes("xterm-256color");
        byte[] ttypeEnd = new byte[] { TelnetConstants.IAC, TelnetConstants.SE };

        byte[] fullPacket = new byte[ttype256.Length + ttypePayload.Length + ttypeEnd.Length];
        Buffer.BlockCopy(ttype256, 0, fullPacket, 0, ttype256.Length);
        Buffer.BlockCopy(ttypePayload, 0, fullPacket, ttype256.Length, ttypePayload.Length);
        Buffer.BlockCopy(ttypeEnd, 0, fullPacket, ttype256.Length + ttypePayload.Length, ttypeEnd.Length);

        negotiator.ProcessInput(fullPacket);
        Assert.Equal(ColorDepth.Color256, capabilities.ColorMode);
        Assert.Equal("xterm-256color", capabilities.TerminalType);
    }

    [Fact]
    public void DirectHtmlDelivery_StreamsRawHtmlForHtmlClients()
    {
        var capabilities = new ClientCapabilities
        {
            SupportsHtml = true,
            SupportsPueblo = true
        };

        string htmlMessage = "<div class=\"room\"><h1 class=\"title\">The Great Chamber</h1><p>You stand in a vast hall.</p></div>";
        string formatted = OutputFormatter.FormatOutput(htmlMessage, capabilities);

        Assert.Equal(htmlMessage, formatted);

        string init = OutputFormatter.BuildPuebloInit("http://localhost:8080/css/classic.css");
        Assert.Contains("<link rel=\"stylesheet\" href=\"http://localhost:8080/css/classic.css\">", init);
    }

    [Fact]
    public void HtmlToAnsi_Fallback_TranslatesTagsToAnsiAndStripsArtifacts()
    {
        var capabilities = new ClientCapabilities
        {
            SupportsHtml = false,
            ColorMode = ColorDepth.TrueColor
        };

        string html = "<style>.room { color: red; }</style><link rel=\"stylesheet\" href=\"/css/style.css\"><b>Welcome</b> to <font color=\"#00FF00\">PennMUSH</font>!<br/>";
        string output = OutputFormatter.FormatOutput(html, capabilities);

        // Non-renderable tags (<style>, <link>) are stripped
        Assert.DoesNotContain("<style>", output);
        Assert.DoesNotContain("<link", output);
        Assert.DoesNotContain(".room", output);

        // Formatting converted to ANSI
        Assert.Contains("\x1B[1mWelcome\x1B[22m", output);
        Assert.Contains("\x1B[38;2;0;255;0mPennMUSH\x1B[39m", output);
        Assert.Contains(Environment.NewLine, output);
    }

    [Fact]
    public void HtmlToAnsi_PlaintextDumbTerminal_StripsAllTags()
    {
        var capabilities = new ClientCapabilities
        {
            SupportsHtml = false,
            ColorMode = ColorDepth.None
        };

        string html = "<b>Bold</b> and <i>Italic</i> and <font color=\"red\">Red</font> text.";
        string output = OutputFormatter.FormatOutput(html, capabilities);

        Assert.Equal("Bold and Italic and Red text.", output);
        Assert.DoesNotContain("\x1B", output, StringComparison.Ordinal);
        Assert.DoesNotContain("<", output);
    }
}
