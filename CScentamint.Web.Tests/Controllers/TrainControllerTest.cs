using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CScentamint;
using CScentamint.Web.Controllers;

namespace CScentamint.Web.Tests.Controllers
{
    [TestClass]
    public class TrainControllerTest : BaseControllerTest
    {
        [TestMethod]
        public void Post()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/train/foo");
            TrainController controller = new TrainController
            {
                Request = request,
            };

            // Act
            var result = controller.Post("foo", "foo bar baz");

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }
    }
}
