using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DocServiceCore.Models;
using DocServiceCore.Services;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocServiceCore.Helpers;
using System.Net;
using DocumentFormat.OpenXml;
using Swashbuckle.SwaggerGen.Annotations;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace DocServiceCore.Controllers
{
    [Route("api/[controller]")]
    public class DocumentController : Controller
    {
        // GET: api/document
        [HttpGet]
        public IEnumerable<Doc> Get()
        {
            //TODO Get all of the Docs in the list
            return DataService.GetDocumentHeaders();
        }

        // GET api/document/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(Guid id)
        {
            {
                try
                {
                    var fullDoc = DataService.GetFullDoc(id);

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

                        // Title and Sub-title
                        Paragraph titlePara = body.AppendChild(new Paragraph());
                        Run run = titlePara.AppendChild(new Run());
                        run.AppendChild(new Text(fullDoc.Header.Title));

                        Paragraph subTitlePara = body.AppendChild(new Paragraph());
                        subTitlePara.AppendChild(new Run(new Text($"Created {fullDoc.Header.Created} (UTC)")));

                        // Paragraph for each para in the list
                        foreach (var para in fullDoc.Paragraphs)
                        {
                            var paragraph = body.AppendChild(new Paragraph(new Run(new Text(
                                $"[{para.TimeStamp} (UTC)] - {para.Text}"))));
                        }


                        mainPart.Document.Save();
                    }

                    mem.Seek(0, SeekOrigin.Begin);

                    return File(mem, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fullDoc.Header.FileName);


                }
                catch (KeyNotFoundException e)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return null;
                }

            }
        }

        // POST api/document
        [HttpPost]
        [SwaggerOperation("PostData")]
        [ProducesResponseType(typeof(Doc), 200)]
        
        public async Task<ActionResult> Post([FromBody]Doc value)
        {
            value.Id = Guid.NewGuid();
            value.Created = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(value.Title))
                value.Title = "Transcript";

            if (string.IsNullOrWhiteSpace(value.FileName))
                value.FileName = $"{value.Id}.docx";


            var newDoc = await DataService.AddDocument(value);

            return Ok(newDoc);
        }

        // PUT api/document/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(Guid id, [FromBody]Para value)
        {
            try
            {
                value.DocId = id;
                return Json(await DataService.AddParagraph(value));
            }
            catch (KeyNotFoundException)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }

        // DELETE api/document/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                await DataService.DeleteDoc(id);
                return null;
            }
            catch (KeyNotFoundException)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }
    }
}
