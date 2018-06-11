using System;
using System.Collections.Generic;

namespace DocumentServiceTester.Models
{
    public class DocumentSummary
    {
        public string Title { get; set; }
        public string Criteria { get; set; }
        public string MatterReference { get; set; }
        public string StatusDescription { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime OrderDate { get; set; }
        public string DocumentId { get; set; }
        public string UserId { get; set; }
        public string OrderId { get; set; }
        public int ItemNumber { get; set; }
    }

    public class DocumentVersion
    {
        public string DocumentVersionId { get; set; }
        public string MimeType { get; set; }
        public DateTime TimeStamp { get; set; }
        public int VersionSequence { get; set; }
        public bool IsAwaitingPdf { get; set; }
        public string DocumentName { get; set; }
    }

    public class DocumentSummaryWithVersions
    {
        public DocumentSummary DocumentSummary { get; set; }
        public List<DocumentVersion> DocumentVersions { get; set; }
    }

    public class DocumentContent
    {
        public string DocumentId { get; set; }
        public string VersionId { get; set; }
        public byte[] Blob { get; set; }
        public string MimeType { get; set; }
        public string DocumentName { get; set; }
    }
}