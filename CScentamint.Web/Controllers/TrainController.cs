using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CScentamint.Bayes;

namespace CScentamint.Web.Controllers
{
    public class TrainController : ApiController
    {
        // GET api/values
        /* public ExpandoObject Get()
        {
            dynamic jsonObject = new ExpandoObject();
            jsonObject.test = "foo";

            return jsonObject;
        }*/

        // POST train/category
        public HttpResponseMessage Post(string category, [FromBody]string sample)
        {
            var classifier = new Classifier();
            classifier.TrainCategory(category, sample);

            return this.Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}