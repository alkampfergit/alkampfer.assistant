using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.Interfaces.Bookmarks;

public interface IContentExtractionService
{
    Task<ExtractionResult> ExtractAsync(string url, CancellationToken cancellationToken = default);
}

public record ExtractionResult(string Content, string Title, string Description, List<Attachment> Attachments);
