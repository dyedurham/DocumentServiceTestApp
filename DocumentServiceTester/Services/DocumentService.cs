using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using DocumentServiceTester.Models;
using HoneyBear.HalClient;
using HoneyBear.HalClient.Models;
using Newtonsoft.Json;

namespace DocumentServiceTester.Services
{
    public class DocumentService
    {
        private readonly HttpClient _httpClient;

        public DocumentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IEnumerable<DocumentSummary> QueryByDateRange(DateTime after, DateTime before)
        {
            return QueryDocumentsWithParameters(new
            {
                startDate = after.ToString("yyyy-MM-dd"),
                endDate = before.AddDays(1).ToString("yyyy-MM-dd"),
                pageSize = 100
            });
        }

        public IEnumerable<DocumentSummary> QueryByMatterOrder(string matterReference, string orderId)
        {
            return QueryDocumentsWithParameters(new
            {
                matterReference,
                orderId,
                pageSize = 100
            });
        }

        public DocumentSummaryWithVersions GetDocumentById(string documentId)
        {
            var halRoot = BuildHalClient().Root(Constants.DocumentsRoute);

            CheckThatApiRootIsAvailable(halRoot, "document");
            var documentResource = halRoot.Get("document", new
            {
                documentId
            });
            var document = documentResource.Item<DocumentSummary>().Data;

            var documentVersionsResource = documentResource.Get("versions");
            var documentVersions = documentVersionsResource.Items<DocumentVersion>().Data();

            return new DocumentSummaryWithVersions
            {
                DocumentSummary = document,
                DocumentVersions = documentVersions.ToList()
            };
        }

        public DocumentContent DownloadDocumentContent(string documentId, string versionId)
        {
            var version = versionId == "Most Recent" || string.IsNullOrEmpty(versionId)
                ? GetLatestDocumentVersion(documentId)
                : GetDocumentVersion(documentId, versionId);

            var blob = _httpClient.GetByteArrayAsync($"{Constants.DocumentsRoute}/documents/{documentId}/versions/{version.DocumentVersionId}/blob").Result;

            return new DocumentContent
            {
                Blob = blob,
                DocumentId = documentId,
                VersionId = version.DocumentVersionId,
                MimeType = version.MimeType,
                DocumentName = version.DocumentName
            };
        }

        private DocumentVersion GetDocumentVersion(string documentId, string versionId)
        {
            var halRoot = BuildHalClient().Root(Constants.DocumentsRoute);

            CheckThatApiRootIsAvailable(halRoot, "documentVersion");
            var versionResource = halRoot.Get("documentVersion", new { documentId, documentVersionId = versionId });

            return versionResource.Item<DocumentVersion>().Data;
        }
        private DocumentVersion GetLatestDocumentVersion(string documentId)
        {
            var halRoot = BuildHalClient().Root(Constants.DocumentsRoute);

            CheckThatApiRootIsAvailable(halRoot, "documentMostRecentVersion");
            var versionResource = halRoot.Get("documentMostRecentVersion", new { documentId });

            return versionResource.Item<DocumentVersion>().Data;
        }

        private IEnumerable<DocumentSummary> QueryDocumentsWithParameters(object parameters)
        {
            var halRoot = BuildHalClient().Root(Constants.DocumentsRoute);

            CheckThatApiRootIsAvailable(halRoot, "documents");
            var documentsPaged = halRoot.Get("documents", parameters);

            if (!documentsPaged.Has("documents")) throw new Exception("No Documents found for these parameters: " + JsonConvert.SerializeObject(parameters));
            var documents = documentsPaged.Get("documents");

            var documentItems = documents.Items<DocumentSummary>().Data();

            return documentItems;
        }

        private static void CheckThatApiRootIsAvailable(IHalClient halRoot, string desiredMethod)
        {
            if (!halRoot.Has(desiredMethod))
                throw new Exception(
                    $"Could not access the root of the API, either Authentication failed or the URL \"{Constants.GlobalXHost}{Constants.DocumentsRoute}\" could not be reached.");
        }

        private HalClient BuildHalClient()
        {
            return new HalClient(_httpClient);
        }
    }
}