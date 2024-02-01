using System.ComponentModel.DataAnnotations;

namespace HandleDataConcurrency.Domain.Documents;

public class DocumentNumber : ICheckConcurrency, IAudit
{
    public DocumentNumber()
    {
        
    }
    public DocumentNumber(DocumentType documentType, int year, string code):this()
    {
        DocumentType = documentType;
        Prefix = (byte)documentType;
        Year =  year;
        Code= code;
    }
    
    public long Id { get; set; }
    public int Year { get; set; }
    public string Code { get; set; }
    public DocumentType DocumentType { get; private set; }
    /// <summary>
    /// Prefix the document
    /// </summary>
    public byte Prefix { get; private set; }
    
    public long SequenceNumber  { get;  set; }

    [ConcurrencyCheck]
    public Guid RowVersion { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public void SetSequenceNumber(long sequenceNumber)
    {
        this.SequenceNumber = sequenceNumber;
    }
}