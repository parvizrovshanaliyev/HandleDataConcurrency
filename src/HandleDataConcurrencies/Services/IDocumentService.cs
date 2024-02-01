using HandleDataConcurrency.Domain.Documents;

namespace HandleDataConcurrency.Services;

public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(CancellationToken cancellationToken);

    ValueTask<string> GenerateDocNumberAsync(DocumentType documentType, string prefix, string budgetCode,
        CancellationToken cancellationToken);
}