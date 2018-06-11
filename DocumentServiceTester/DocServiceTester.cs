using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Forms;
using DocumentServiceTester.Models;
using DocumentServiceTester.Services;

namespace DocumentServiceTester
{
    public partial class DocServiceTester : Form
    {
        private HttpClient _httpClient;
        private DateTime _authExpiry = DateTime.MinValue;

        public DocServiceTester()
        {
            InitializeComponent();
        }

        private void DocServiceTester_Load(object sender, EventArgs e)
        {
            InitialiseFieldValues();
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            btnQueryByDate.Text = "Working...";
            btnQueryByDate.Enabled = false;

            try
            {
                PopulateVersionsComboBox(null);
                var result = QueryDocumentByDate();
                ConstructDocumentCollectionTreeNodes(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during Document query: " + CollateInnerExceptionMessages(ex));
            }
            
            btnQueryByDate.Text = "Query";
            btnQueryByDate.Enabled = true;
        }


        private void btnQueryByMatterOrder_Click(object sender, EventArgs e)
        {
            btnQueryByMatterOrder.Text = "Working...";
            btnQueryByMatterOrder.Enabled = false;

            try
            {
                PopulateVersionsComboBox(null);
                var result = QueryDocumentByMatterOrder();
                ConstructDocumentCollectionTreeNodes(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during Document query: " + CollateInnerExceptionMessages(ex));
            }

            btnQueryByMatterOrder.Text = "Query";
            btnQueryByMatterOrder.Enabled = true;
        }
        
        private void btnGetDocument_Click(object sender, EventArgs e)
        {
            try
            {
                var result = RetrieveDocumentByDocumentId();
                ConstructDocumentTreeNodes(result);
                PopulateVersionsComboBox(result.DocumentVersions);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during Document retrieval: " + CollateInnerExceptionMessages(ex));
            }
        }
        
        private void btnDownload_Click(object sender, EventArgs e)
        {
            try
            {
                var result = DownloadDocument();
                SaveDownloadedFile(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during Document download: " + CollateInnerExceptionMessages(ex));
            }
        }

        private static void SaveDownloadedFile(DocumentContent documentContent)
        {
            var saveAsDialog = new SaveFileDialog
            {
                Title = "Save File",
                Filter = "All files (*.*)|*.*",
                DefaultExt = ".pdf",
                FileName = documentContent.DocumentName
            };
            var result = saveAsDialog.ShowDialog();

            if (result == DialogResult.OK && saveAsDialog.FileName != "")
            {
                File.WriteAllBytes(saveAsDialog.FileName, documentContent.Blob);
            }
        }

        private void tvDocuments_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            QueryDocumentById();
        }

        private void InitialiseFieldValues()
        {
            dtpFrom.Value = DateTime.Now;
            dtpTo.Value = DateTime.Now;

            PopulateVersionsComboBox(null);

            txtUsername.Text = ConfigurationManager.AppSettings["Username"];
            txtPassword.Text = ConfigurationManager.AppSettings["Password"];
            txtClientID.Text = ConfigurationManager.AppSettings["ClientId"];
            txtClientSecret.Text = ConfigurationManager.AppSettings["ClientSecret"];

            CheckAuthValidity();
        }

        private IEnumerable<DocumentSummary> QueryDocumentByDate()
        {
            var docService = GetDocumentService();

            return docService.QueryByDateRange(dtpFrom.Value, dtpTo.Value);
        }
        private IEnumerable<DocumentSummary> QueryDocumentByMatterOrder()
        {
            var docService = GetDocumentService();

            return docService.QueryByMatterOrder(txtMatterRef.Text, txtOrderId.Text);
        }
        private DocumentSummaryWithVersions RetrieveDocumentByDocumentId()
        {
            var docService = GetDocumentService();

            return docService.GetDocumentById(txtDocumentId.Text);
        }
        private DocumentContent DownloadDocument()
        {
            var docService = GetDocumentService();

            return docService?.DownloadDocumentContent(txtDocumentId.Text, cmbVersions.SelectedItem.ToString());
        }

        private void ConstructDocumentCollectionTreeNodes(IEnumerable<DocumentSummary> documents)
        {
            tvDocuments.Nodes.Clear();

            var treeNodes = documents.Select(BuildDocumentTreeNode).ToArray();

            tvDocuments.Nodes.AddRange(treeNodes);
        }

        private void ConstructDocumentTreeNodes(DocumentSummaryWithVersions document)
        {
            tvDocuments.Nodes.Clear();

            var treeNode = BuildDocumentTreeNode(document.DocumentSummary);

            var versionNodes = document.DocumentVersions.Select(BuildVersionTreeNode).ToArray();

            tvDocuments.Nodes.Add(treeNode);
            tvDocuments.Nodes.AddRange(versionNodes);
        }

        private void PopulateVersionsComboBox(List<DocumentVersion> documentVersions)
        {
            cmbVersions.Enabled = false;

            cmbVersions.Items.Clear();
            cmbVersions.Items.Add("Most Recent");

            if (documentVersions != null && documentVersions.Any())
            {
                cmbVersions.Items.AddRange(documentVersions.Select(x => x.DocumentVersionId).ToArray());
                cmbVersions.Enabled = true;
            }

            cmbVersions.SelectedIndex = 0;
        }

        private void QueryDocumentById()
        {
            var selectedNode = tvDocuments.SelectedNode;

            if (selectedNode.Parent != null) selectedNode = selectedNode.Parent;

            txtDocumentId.Text = (selectedNode.Tag as DocumentSummary)?.DocumentId;
        }

        private DocumentService GetDocumentService()
        {
            if (_httpClient == null) BuildHttpClient();
            if (_authExpiry < DateTime.Now) Authenticate(_httpClient);
        
            var docService = new DocumentService(_httpClient);
            return docService;
        }

        private void BuildHttpClient()
        {
            _httpClient = new HttpClient {BaseAddress = new Uri(Constants.GlobalXHost)};
        }

        private void Authenticate(HttpClient client)
        {
            var authenticator = new Authenticator();

            try
            {
                var token = authenticator.GetBearerToken(txtUsername.Text, txtPassword.Text, txtClientID.Text,
                    txtClientSecret.Text);

                if (token == null || string.IsNullOrEmpty(token.AccessToken))
                    throw new Exception("No token returned from GlobalX OAuth2 Endpoint.");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                _authExpiry = DateTime.Now.AddSeconds(token.ExpiresIn);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred during Authentication", ex);
            }
        }

        private TreeNode BuildDocumentTreeNode(DocumentSummary documentSummary)
        {
            var treeNodes = new List<TreeNode>();

            PopulateTreeNodeChildrenWithObjectProperties(documentSummary, treeNodes);

            var documentNode = new TreeNode($"{documentSummary.MatterReference}: {documentSummary.Title}",
                treeNodes.ToArray()) {Tag = documentSummary};
            return documentNode;
        }

        private TreeNode BuildVersionTreeNode(DocumentVersion documentVersion)
        {
            var treeNodes = new List<TreeNode>();

            PopulateTreeNodeChildrenWithObjectProperties(documentVersion, treeNodes);

            var documentNode = new TreeNode($"{(documentVersion.IsAwaitingPdf ? "Pending" : "Complete")}: {documentVersion.TimeStamp:yyyy-MM-dd hh:mm:ss}",
                    treeNodes.ToArray())
                { Tag = documentVersion };
            return documentNode;
        }

        private static void PopulateTreeNodeChildrenWithObjectProperties<T>(T objectWithProperties, List<TreeNode> treeNodes) where T : class
        {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(objectWithProperties))
            {
                treeNodes.Add(new TreeNode($"{descriptor.Name}: {descriptor.GetValue(objectWithProperties)}"));
            }
        }

        private void txtClientID_TextChanged(object sender, EventArgs e)
        {
            CheckAuthValidity();
        }

        private void txtClientSecret_TextChanged(object sender, EventArgs e)
        {
            CheckAuthValidity();
        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {
            CheckAuthValidity();
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            CheckAuthValidity();
        }

        private void CheckAuthValidity()
        {
            var authState = !AuthenticationFieldsAreNotFilledOut();

            SetQueryButtonsEnabledState(authState);
        }

        private bool AuthenticationFieldsAreNotFilledOut()
        {
            return (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text) ||
                    string.IsNullOrEmpty(txtClientID.Text) || string.IsNullOrEmpty(txtClientSecret.Text));
        }

        private void SetQueryButtonsEnabledState(bool enabled)
        {
            btnQueryByMatterOrder.Enabled = enabled;
            btnQueryByDate.Enabled = enabled;
        }

        private string CollateInnerExceptionMessages(Exception ex)
        {
            return ex == null ? "" : $@"

{ex.Message}
{ex.StackTrace}{(ex.InnerException != null ? CollateInnerExceptionMessages(ex.InnerException) : "")}";
        }
    }
}
