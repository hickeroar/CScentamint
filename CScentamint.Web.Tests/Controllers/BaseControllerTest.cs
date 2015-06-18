using CScentamint.Bayes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CScentamint.Web.Tests.Controllers
{
    public class BaseControllerTest
    {
        [TestInitialize]
        public void SetUp()
        {
            Classifier.Categories = null;
            Classifier.Probabilities = null;
        }
    }
}
