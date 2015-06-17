using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CScentamint.Bayes;

namespace CScentamint.Web.Controllers
{
    public class FlushController : ApiController
    {
        /// <summary>
        /// POST flush
        /// Empties all token/probability storage
        /// </summary>
        /// <returns>204 response code</returns>
        public HttpResponseMessage Post()
        {
            var classifier = new Classifier();
            classifier.Flush();

            return this.Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
