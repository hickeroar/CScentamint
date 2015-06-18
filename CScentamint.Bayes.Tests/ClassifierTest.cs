using CScentamint.Bayes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CScentamint.Bayes.Tests
{
    [TestClass]
    public class ClassifierTest
    {
        [TestInitialize]
        public void SetUp()
        {
            Classifier.Categories = null;
            Classifier.Probabilities = null;
        }

        [TestMethod]
        public void TestClassifierStaticStorageIsInitializedOnInstantiation()
        {
            // Assert
            Assert.IsNull(Classifier.Categories);
            Assert.IsNull(Classifier.Probabilities);

            // Arrange/Act
            Classifier cls = new Classifier();

            // Assert
            Assert.IsInstanceOfType(Classifier.Categories, typeof(Dictionary<string, Dictionary<string, int>>));
            Assert.IsInstanceOfType(Classifier.Probabilities, typeof(Dictionary<string, Dictionary<string, float>>));
        }
    }
}
