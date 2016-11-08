using DocService.Models;
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
            return new Doc[]
            {
                new Models.Doc { FileName="FirstFile.docx", Id=Guid.NewGuid(), Title="Hello World" }
            };
        }

        // GET: api/Document/{Guid}
        public async Task<IHttpActionResult> Get(Guid id)
        {
            //TODO Build the document (or 404 if no such id)
            if (id.ToString() == "00000000-0000-0000-0000-000000000000")
            {
                return NotFound();
            }
            else
            {
                IHttpActionResult result;
                MemoryStream mem = new MemoryStream();


                // Create Document
                using (WordprocessingDocument wordDocument =
                    WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
                {
                    // Add a main document part. 
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                    // Create the document structure and add some text.
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());
                    Paragraph para = body.AppendChild(new Paragraph());
                    Run run = para.AppendChild(new Run());
                    run.AppendChild(new Text("Hello world!"));
                    mainPart.Document.Save();
                }

                mem.Seek(0, SeekOrigin.Begin);

                HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.OK);
                msg.Content = new StreamContent(mem);

                string fileName = "HelloWorld.docx";

                msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                msg.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"{fileName}"
                };
                result = ResponseMessage(msg);


                return result;
            }

        }

        // POST: api/Document
        public async Task<Guid> Post([FromBody]Doc value)
        {
            //TODO Create the document header
            value.Id = Guid.NewGuid();

            return value.Id;
        }

        // PATCH: api/Document/5
        public async Task<IHttpActionResult> Patch(Guid id, [FromBody]Para value)
        {
            //TODO Add a paragraph
            if (id.ToString() == "00000000-0000-0000-0000-000000000000")
                return NotFound();
            else
                return Ok();
        }

        // DELETE: api/Document/5
        public async Task<IHttpActionResult> Delete(Guid id)
        {
            //TODO Delete the document and all of its paragraphs
            if (id.ToString() == "00000000-0000-0000-0000-000000000000")
                return NotFound();
            else
                return Ok();
        }
    }
}
