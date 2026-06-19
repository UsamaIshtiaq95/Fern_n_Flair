// C#
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserDomain;
public interface IAnthropicService
{
    Task<string> SendContextAndGetRawResponseAsync(IList<MessageDto> context, CancellationToken cancellationToken = default);
}