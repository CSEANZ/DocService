using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using DocService.Models;

namespace DocService.Services
{
    public static class DataService
    {
        const string PARTITION_KEY = "Documents";

        // Parse the connection string and return a reference to the storage account.
        private static CloudStorageAccount storageAccount;
        private static CloudTableClient tableClient;
        private static CloudTable documentTable;


        private static CloudTable paragraphTable;

        static DataService()
        {
            storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            tableClient = storageAccount.CreateCloudTableClient();
            documentTable = tableClient.GetTableReference("document");
            documentTable.CreateIfNotExists();
            paragraphTable = tableClient.GetTableReference("paragraph");
            paragraphTable.CreateIfNotExists();

        }

        public static async Task<Para> AddParagraph(Para para)
        {
            TableOperation insertOperation = TableOperation.Insert(ParaEntity.FromPara(para));
            var result = await paragraphTable.ExecuteAsync(insertOperation);

            if (result.HttpStatusCode / 100 != 2)
                throw new Exception("Error adding paragraph");

            return para;
        }

        public static async Task DeleteParagraph(Guid id)
        {

            var paraEnt = GetParaEnt(id);

            TableOperation deleteOperation = TableOperation.Delete(paraEnt);
            var result = await paragraphTable.ExecuteAsync(deleteOperation);

            if (result.HttpStatusCode / 100 != 2)
                throw new Exception($"Error deleting paragraph Id {id}");

            return;

        }


        // this is a fire and forget operation - no awaiting
        public static async Task CleanUpParagraphTable()
        {
            // Delete paragraphs without a Document ID
            TableQuery<ParaEntity> nullQuery = new TableQuery<ParaEntity>();
            var paraEntList = paragraphTable.ExecuteQuery(nullQuery).Where(p => p.DocId == null).ToList();
            if (paraEntList != null)
            {
                var batch = new TableBatchOperation();
                foreach (var paraEnt in paraEntList)
                {
                    batch.Add(TableOperation.Delete(paraEnt));
                }
                paragraphTable.ExecuteBatchAsync(batch);
            }

            // Delete all the paragraphs without a parent table
            TableQuery<ParaEntity> idsQuery = new TableQuery<ParaEntity>();
            var idList = paragraphTable.ExecuteQuery(idsQuery).Select(p => p.DocId).Distinct().ToList();

            if(idList != null)
            {
                foreach (var docId in idList)
                {
                    var pEL = GetDocumentParaEnts(docId.Value);
                    if (pEL != null)
                    {
                        var batch = new TableBatchOperation();
                        foreach (var paraEnt in pEL)
                        {
                            batch.Add(TableOperation.Delete(paraEnt));
                        }
                        paragraphTable.ExecuteBatchAsync(batch);
                    }
                }
            }

        }

        public static async Task DeleteDoc(Guid docId)
        {
            // Delete the document header
            var docEnt = GetDocEnt(docId);
            TableOperation deleteOperation = TableOperation.Delete(docEnt);
            var result = await documentTable.ExecuteAsync(deleteOperation);

            if (result.HttpStatusCode / 100 != 2)
                throw new Exception($"Error deleting document Id {docId}");


            // Delete the document's paragraphs
            // Don't worry about awaiting the result
            var pEL = GetDocumentParaEnts(docId);
            if (pEL != null)
            {
                var batch = new TableBatchOperation();
                foreach (var paraEnt in pEL)
                {
                    batch.Add(TableOperation.Delete(paraEnt));
                }
                paragraphTable.ExecuteBatchAsync(batch);
            }
        }

        public static async Task<Doc> AddDocument(Doc value)
        {
            TableOperation insertOperation = TableOperation.Insert(DocEntity.FromDoc(value));
            var result = await documentTable.ExecuteAsync(insertOperation);

            if (result.HttpStatusCode / 100 != 2)
                throw new Exception("Error adding document");

            return value;
        }

        public static Para GetPara(Guid id)
        {
            return GetParaEnt(id).ToPara();
        }

        internal static ParaEntity GetParaEnt(Guid id)
        {
            // find the paragraph
            TableQuery<ParaEntity> query = new TableQuery<ParaEntity>();
            var paraEnt = paragraphTable.ExecuteQuery(query).Where(p => p.Id == id).FirstOrDefault();
            // if we can't find it, raise an exception
            if (paraEnt == null)
                throw new KeyNotFoundException($"Paragraph ID {id} does not exist");

            return paraEnt;
        }

        public static Doc GetDoc(Guid id)
        {
            return GetDocEnt(id).ToDoc();
        }

        internal static DocEntity GetDocEnt(Guid id)
        {
            // find the paragraph
            TableQuery<DocEntity> query = new TableQuery<DocEntity>();
            var docEnt = documentTable.ExecuteQuery(query).Where(d => d.Id == id).FirstOrDefault();
            // if we can't find it, raise an exception
            if (docEnt == null)
                throw new KeyNotFoundException($"Document ID {id} does not exist");

            return docEnt;
        }

        internal static List<ParaEntity> GetDocumentParaEnts(Guid docId)
        {
            // find the paragraphs
            TableQuery<ParaEntity> query = new TableQuery<ParaEntity>();
            var paraEntList = paragraphTable.ExecuteQuery(query).Where(p => p.DocId == docId).ToList();

            return paraEntList;
        }

        public static FullDoc getFullDoc(Guid id)
        {
            var fullDoc = new FullDoc
            {
                Header = GetDocEnt(id).ToDoc(),
                Paragraphs = new List<Para>()
            };

            var paraEntList = GetDocumentParaEnts(id);

            if(paraEntList != null)
            {
                foreach (var paraEnt in paraEntList)
                {
                    fullDoc.Paragraphs.Add(paraEnt.ToPara());
                }
            }

            return fullDoc;
        }


        public static IEnumerable<Doc> GetDocumentHeaders()
        {
            List<Doc> docs = new List<Doc>();

            var docEntList = GetDocEnts();
            if(docEntList != null)
            {
                foreach (var docEnt in docEntList)
                {
                    docs.Add(docEnt.ToDoc());
                }
            }

            return docs;

        }

        private static List<DocEntity> GetDocEnts()
        {
            TableQuery<DocEntity> query = new TableQuery<DocEntity>();
            return documentTable.ExecuteQuery(query).ToList();
        }
    }

    internal class DocEntity :  TableEntity
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public DateTimeOffset Created { get; set; }

        public static DocEntity FromDoc(Doc doc)
        {
            return new DocEntity
            {
                PartitionKey = "DocumentService",
                RowKey = doc.Id.ToString("N"),
                Id = doc.Id,
                FileName = doc.FileName,
                Title = doc.Title,
                Created = doc.Created
            };
        }

        public Doc ToDoc()
        {
            return new Doc
            {
                Id = Id,
                FileName = FileName,
                Title = Title,
                Created = Created
            };
        }
    }

    internal class ParaEntity : TableEntity
    {
        public Guid Id { get; set; }
        public Guid? DocId { get; set; }
        public string Text { get; set; }
        public string Style { get; set; }
        public int Order { get; set; }
        public DateTimeOffset TimeStamp { get; set; }

        public static ParaEntity FromPara(Para para)
        {
            return new ParaEntity
            {
                PartitionKey = "DocumentService",
                RowKey = para.Id.ToString("N"),
                Id = para.Id,
                DocId = para.DocId,
                Text = para.Text,
                Style = para.Style,
                Order = para.Order,
                TimeStamp = para.TimeStamp
            };
        }

        public Para ToPara()
        {
            return new Para
            {
                Id = Id,
                DocId = DocId,
                Text = Text,
                Style = Style,
                Order = Order,
                TimeStamp = TimeStamp
            };
        }

    }
}