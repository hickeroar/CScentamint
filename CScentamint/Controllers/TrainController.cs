using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace CScentamint.Controllers
{
    public class TrainController : ApiController
    {
        // GET api/values
        public ExpandoObject Get()
        {
            dynamic jsonObject = new ExpandoObject();
            jsonObject.test = "foo";

            return jsonObject;
        }

        // GET api/values/5
        public int Get(int id)
        {
            return 3;
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}