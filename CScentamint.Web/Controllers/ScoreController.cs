using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CScentamint.Bayes;

namespace CScentamint.Web.Controllers
{
    public class ScoreController : ApiController
    {
        /// <summary>
        /// POST score
        /// Scores a sample of text
        /// </summary>
        /// <param name="sample">text we want to score</param>
        /// <returns>dictionary of scores</returns>
        public Dictionary<string, float> Post([FromBody]string sample)
        {
            var classifier = new Classifier();

            return classifier.Score(sample);
        }
    }
}