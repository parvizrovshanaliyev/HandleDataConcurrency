namespace HandleDataConcurrency.Domain.Documents;

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
    Purchase=45
}