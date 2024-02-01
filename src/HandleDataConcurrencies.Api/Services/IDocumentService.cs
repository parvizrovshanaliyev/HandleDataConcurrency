using HandleDataConcurrency.Api.Domain.Documents;

namespace HandleDataConcurrency.Api.Services;

public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(CancellationToken cancellationToken);

    ValueTask<string> GenerateDocNumberAsync(DocumentType documentType, string prefix, string budgetCode,
        CancellationToken cancellationToken);
}