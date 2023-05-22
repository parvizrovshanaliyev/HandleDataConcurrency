using System.ComponentModel.DataAnnotations;

namespace HandleDataConcurrency.Models.Documents;

public interface ICheckConcurrency
{
    public Guid RowVersion { get; set; }
}

public interface IAudit
{
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}


public class Document
{
    public Document(DocumentType documentType,DocumentStatus status)
    {
        DocumentType = DocumentType;
        Prefix = (byte)documentType;
        Status = Status;
    }
    
    public long Id { get; set; }
    
    public DocumentType DocumentType { get; set; }
    public byte Prefix { get; set; }
    public DocumentStatus Status { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string Number { get; private set; }

    public void SetDocumentNumber(string documentNumber)
    {
        Number = documentNumber;
    }
    
}


public class DocumentNumber : ICheckConcurrency, IAudit
{
    public DocumentNumber()
    {
        
    }
    public DocumentNumber(DocumentType documentType, int year, string budgetCode):this()
    {
        DocumentType = documentType;
        Prefix = (byte)documentType;
        Year =  year;
        BudgetCode= budgetCode;
    }
    
    public long Id { get; set; }
    public int Year { get; set; }
    public string BudgetCode { get; set; }
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

public enum DocumentStatus
{
    Waiting,
    Processing,
    Completed,
    Failed
}

public enum DocumentType
{
    ShortTermLiability=42,
    Mas=45
}