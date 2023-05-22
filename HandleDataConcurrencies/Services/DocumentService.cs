using HandleDataConcurrencies.Data;
using HandleDataConcurrency.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace HandleDataConcurrency.Services;


public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(CancellationToken cancellationToken);
    
    ValueTask<string> GenerateDocNumberAsync(DocumentType documentType,string prefix, string budgetCode,CancellationToken cancellationToken);
    
}
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    
    public DocumentService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    
    public async Task<Document> CreateDocumentAsync(CancellationToken cancellationToken)
    {
        string budgetCode = "2728501";
        //1. first step prefix
        var documentType = DocumentType.ShortTermLiability;
        var prefix = ((byte)documentType).ToString();
        
        //2. 
        var documentNumber = await GenerateDocNumberAsync(documentType:documentType,prefix: prefix, budgetCode: budgetCode, cancellationToken);
        
        //3. create document
        var document = new Document(documentType: documentType, status: DocumentStatus.Waiting);
        document.SetDocumentNumber(documentNumber);
        
        await _context.Documents.AddAsync(document, cancellationToken).ConfigureAwait(false);
        
        //4. save document
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return document;
    }

    public async ValueTask<string> GenerateDocNumberAsync(DocumentType documentType, string prefix, string budgetCode,CancellationToken cancellationToken)
    {
        int maxRetryCount = 3;
        int retryCount = 0;
        bool success = false;
        var currentYear = DateTime.Now.Year;
        DocumentNumber? lastDocumentNumber = null;
        while (retryCount < maxRetryCount && !success)
        {
            try
            {

                if (lastDocumentNumber != null)
                {
                    // Detach the tracked entity
                    _context.Entry(lastDocumentNumber).State = EntityState.Detached;
                    lastDocumentNumber = null;
                }
                
                //1. get last documentNumber by document type
                lastDocumentNumber = await _context.DocumentNumbers
                    .Where(d => d.DocumentType == documentType && d.BudgetCode == budgetCode && d.Year == currentYear)
                    .OrderByDescending(d => d.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
        
                //2. create new or update last documentNumber
                long sequenceNumber = 1; 

                if (lastDocumentNumber == null)
                {
                    // Create
                    lastDocumentNumber = new DocumentNumber(documentType: documentType, year: currentYear, budgetCode: budgetCode);

                    lastDocumentNumber.SetSequenceNumber(sequenceNumber);
            
                    await _context.AddAsync(lastDocumentNumber, cancellationToken);
                }
                else
                {
                    // Update
                    sequenceNumber = lastDocumentNumber.SequenceNumber + 1;
            
                    lastDocumentNumber.SetSequenceNumber(sequenceNumber);
                    
                    // // Detach the tracked entity
                    // _context.Entry(lastDocumentNumber).State = EntityState.Detached;
                    //
                    // // Attach the modified entity
                    // _context.Attach(lastDocumentNumber);
                    _context.Entry(lastDocumentNumber).Property(d => d.SequenceNumber).IsModified = true;
                }
        
                //3. save documentNumber
                await _context.SaveChangesAsync(cancellationToken);
                success = true;
                // formatted document number example : prefix(45) + budgetCode(2728501) + sequenceNumber(00001) = 45272850100001
                return GetFormattedDocumentNumber(lastDocumentNumber);
            }
            //4. Check Concurrency Exception if has exception try again from step 1.
            catch (DbUpdateConcurrencyException e)
            {
                retryCount++;
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Handle other exceptions and break the loop
                break;
            }
        }

        // Maximum retry count reached or save operation failed, handle the failure case here
        throw new Exception("Failed to generate document number.");
    }
    
    /// <summary>
    /// formatted document number example :
    /// prefix(45) + budgetCode(2728501) + sequenceNumber(00001) = 45272850100001
    /// </summary>
    /// <param name="documentNumber"></param>
    /// <returns></returns>
    private string GetFormattedDocumentNumber(DocumentNumber? documentNumber)
    {
        return $"{documentNumber.Prefix}{documentNumber.BudgetCode}{documentNumber.SequenceNumber:D5}";
    }
}