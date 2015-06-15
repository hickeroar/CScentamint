using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CScentamint.Bayes;

namespace CScentamint.Web.Controllers
{
    public class UntrainController : ApiController
    {
        /// <summary>
        /// POST untrain/category
        /// Untrains a category
        /// </summary>
        /// <param name="category">The name of the category we want to untrain</param>
        /// <param name="sample">The text sample we're untraining with</param>
        /// <returns>204 response code</returns>
        public HttpResponseMessage Post(string category, [FromBody]string sample)
        {
            var classifier = new Classifier();
            classifier.UntrainCategory(category, sample);

            return this.Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
