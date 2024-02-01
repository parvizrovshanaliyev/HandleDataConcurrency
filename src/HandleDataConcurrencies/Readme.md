# Managing Data Concurrency in a Web Application with Optimistic Concurrency Control

## Introduction

Handling data concurrency in a web application is a critical aspect to ensure data integrity, especially when dealing with multiple users making simultaneous edits. 
This example, built on Dotnet Core 6 and Entity Framework Core 6, demonstrates an effective implementation of Optimistic Concurrency Control.

## Document Numbering System

### The Concurrency Challenge

In a web application, the challenge arises when multiple users attempt to edit the same data concurrently. 
This scenario can lead to data overwrites, a common issue when several users access identical data simultaneously. 
The risk is especially evident when one user is editing a record, and another user concurrently saves changes, inadvertently overwriting the modifications made by the first user.

### The Optimal Solution

To address this challenge, implementing a concurrency control mechanism becomes imperative. 
This mechanism prevents simultaneous edits by different users and can be achieved through techniques like using a version number or a timestamp to track changes made to a record.
When a user attempts to save modifications, the application checks whether the record has been altered by another user since it was last loaded. 
If modifications are detected, the application prompts the user to reload the record and reapply the changes, ensuring data consistency.

## Project Overview

### The Core Problem

The primary goal of this project is to establish a system that ensures accurate and non-repetitive numbering of documents,
all while incorporating Optimistic Concurrency Control. The challenge becomes apparent when two users simultaneously receive the latest document number. 
If one processes it faster than the other, the last document number held by the slower user becomes outdated.
To counter this, the system employs a version number to track changes made to the record. When attempting to save modifications, the application checks if the record has been altered by another user since it was last loaded. 
If modifications are identified, the application notifies the user with an error message, prompting them to reload the record and reapply the changes.

Now, let's delve into the key components and the step-by-step implementation of this solution:

### 1. ICheckConcurrency Interface

The first step involves creating an interface, `ICheckConcurrency`, ensuring that domain models implement Concurrency Control. 
This interface includes a `RowVersion` property, acting as a unique identifier for concurrency checks.

```csharp
public interface ICheckConcurrency
{
    Guid RowVersion { get; set; }
}
```

### 2. DocumentNumber Domain Model

The `DocumentNumber` domain model implements the `ICheckConcurrency` interface, signaling that it participates in concurrency control. The `RowVersion` field uses the `ConcurrencyCheck` DataAnnotation, instructing the ORM to perform Concurrency Control.

```csharp
public class DocumentNumber : ICheckConcurrency, IAudit
{
    // ... Other properties and methods ...

    [ConcurrencyCheck]
    public Guid RowVersion { get; set; }

    // ... Other properties and methods ...
}
```

### 3. ApplicationDbContext SaveChangesAsync Method Override

Within the `ApplicationDbContext` class, we override the `SaveChangesAsync` method to ensure that, during the save operation, the `RowVersion` for entities implementing `ICheckConcurrency` receives a different value.

```csharp
public class ApplicationDbContext : DbContext
{
    // ... Other DbContext configurations ...

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyConcurrencyUpdates();
        ApplyAuditUpdates();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyConcurrencyUpdates()
    {
            var entities = ChangeTracker.Entries<ICheckConcurrency>()
                .Where(e => e.State is EntityState.Modified or EntityState.Added);

            foreach (var entityEntry in entities)
            {
                entityEntry.Entity.RowVersion = Guid.NewGuid();
            }
    }

    private void ApplyAuditUpdates()
    {
        // ... Apply updates for audit fields ...
    }
}
```

### 4. GenerateDocNumberAsync Method

The `GenerateDocNumberAsync` method outlines the intricate process of generating a document number while handling Concurrency Control. The key aspect is the incorporation of a retry mechanism to address concurrency issues and reattempt the operation.

```csharp
public async ValueTask<string> GenerateDocNumberAsync(
        DocumentType documentType,
        string prefix,
        string code,
        CancellationToken cancellationToken)
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

                // 1. Get the last documentNumber by document type, budget code, and current year
                lastDocumentNumber = await _context.DocumentNumbers
                    .Where(d => d.DocumentType == documentType && d.Code == code && d.Year == currentYear)
                    .OrderByDescending(d => d.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 2. Create a new documentNumber or update the last documentNumber
                long sequenceNumber = 1;

                if (lastDocumentNumber == null)
                {
                    // Create a new documentNumber
                    lastDocumentNumber =
                        new DocumentNumber(documentType: documentType, year: currentYear, code: code);
                    lastDocumentNumber.SetSequenceNumber(sequenceNumber);
                    await _context.AddAsync(lastDocumentNumber, cancellationToken);
                }
                else
                {
                    // Update the last documentNumber
                    sequenceNumber = lastDocumentNumber.SequenceNumber + 1;
                    lastDocumentNumber.SetSequenceNumber(sequenceNumber);

                    // Mark the SequenceNumber property as modified
                    _context.Entry(lastDocumentNumber).Property(d => d.SequenceNumber).IsModified = true;
                }

                // 3. Save the documentNumber
                await _context.SaveChangesAsync(cancellationToken);
                success = true;

                // Format the document number: prefix(45) + budgetCode(2728501) + sequenceNumber(00001) = 45272850100001
                return GetFormattedDocumentNumber(lastDocumentNumber);
            }
            // 4. Handle concurrency exception and retry from step 1
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
```

In conclusion, this comprehensive approach ensures a robust solution for managing data concurrency, providing a secure and reliable system for generating document numbers in a web application. 