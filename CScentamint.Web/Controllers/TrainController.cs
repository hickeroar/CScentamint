using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CScentamint.Bayes;

namespace CScentamint.Web.Controllers
{
    public class TrainController : ApiController
    {
        /// <summary>
        /// POST train/category
        /// Trains a category
        /// </summary>
        /// <param name="category">The name of the category we want to train</param>
        /// <param name="sample">The text sample we're training with</param>
        /// <returns>204 response code</returns>
        public HttpResponseMessage Post(string category, [FromBody]string sample)
        {
            var classifier = new Classifier();
            classifier.TrainCategory(category, sample);

            return this.Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}