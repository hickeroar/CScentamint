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
    public class ClassifyController : ApiController
    {
        /// <summary>
        /// POST classify
        /// Classifies a sample of text
        /// </summary>
        /// <param name="sample">The text sample we're training with</param>
        /// <returns>object representing classification result</returns>
        public ExpandoObject Post([FromBody]string sample)
        {
            var classifier = new Classifier();

            return classifier.Classify(sample);
        }
    }
}