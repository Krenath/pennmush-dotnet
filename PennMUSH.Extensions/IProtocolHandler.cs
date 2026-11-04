namespace PennMUSH.Extensions;

using System.Text.Json;
using System.Threading.Tasks;
using PennMUSH.Core.Types;

public interface IProtocolHandler
{
    Task HandleEnvelopeAsync(Dbref player, string type, JsonElement payload, IExtensionHost host);
}
