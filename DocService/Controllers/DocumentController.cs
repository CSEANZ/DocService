using DocService.Helpers;
using DocService.Models;
using DocService.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace DocService.Controllers
{
    public class DocumentController : ApiController
    {
        // GET: api/Document
        public async Task<IEnumerable<Doc>> Get()
        {
            //TODO Get all of the Docs in the list
            return DataService.GetDocumentHeaders();
        }

        // GET: api/Document/{Guid}
        public async Task<IHttpActionResult> Get(Guid DocId)
        {
            try
            {
                var fullDoc = DataService.getFullDoc(DocId);

                IHttpActionResult result;
                MemoryStream mem = new MemoryStream();


                // Create Document
                using (WordprocessingDocument wordDocument =
                    WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
                {
                    // Add a main document part. 
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    DocHelper.AddStyles(mainPart);

                    // Create the document structure and add some text.
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    // Title and Sub-title
                    Paragraph titlePara = body.AppendChild(new Paragraph());
                    DocHelper.ApplyStyleToParagraph(wordDocument, "unknown", "Title", titlePara);
                    Run run = titlePara.AppendChild(new Run());
                    run.AppendChild(new Text(fullDoc.Header.Title));

                    Paragraph subTitlePara = body.AppendChild(new Paragraph());
                    DocHelper.ApplyStyleToParagraph(wordDocument, "unknown", "Subtitle", subTitlePara);
                    subTitlePara.AppendChild(new Run(new Text($"Created {fullDoc.Header.Created} (UTC)")));

                    // Paragraph for each para in the list
                    foreach (var para in fullDoc.Paragraphs)
                    {
                        var paragraph = body.AppendChild(new Paragraph(new Run(new Text(
                            $"[{para.TimeStamp} (UTC)] - {para.Text}"))));
                        if (!string.IsNullOrWhiteSpace(para.Style))
                            DocHelper.ApplyStyleToParagraph(wordDocument, "unknown", para.Style, paragraph);
                    }


                    mainPart.Document.Save();
                }

                mem.Seek(0, SeekOrigin.Begin);

                HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.OK);
                msg.Content = new StreamContent(mem);

                msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                msg.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"{fullDoc.Header.FileName}"
                };
                result = ResponseMessage(msg);


                return result;
            }
            catch (KeyNotFoundException e)
            {
                return NotFound();
            }


        }

        // POST: api/Document
        public async Task<IHttpActionResult> Post([FromBody]Doc value)
        {
            value.Id = Guid.NewGuid();
            value.Created = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(value.Title))
                value.Title = "Transcript";

            if (string.IsNullOrWhiteSpace(value.FileName))
                value.FileName = $"{value.Id}.docx";

            

            return Json<Doc>(value);
        }

        // PATCH: api/Document/{id}
        public async Task<IHttpActionResult> Patch(Guid docId, [FromBody]Para value)
        {
            try
            {
                value.DocId = docId;
                return Json<Para>(await DataService.AddParagraph(value));
            }
            catch (KeyNotFoundException)
            {

                return NotFound();
            }
        }

        // DELETE: api/Document/{id}
        public async Task<IHttpActionResult> Delete(Guid docId)
        {
            try
            {
                await DataService.DeleteParagraph(docId);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
