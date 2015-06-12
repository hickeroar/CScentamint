using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CScentamint.Bayes
{
    public class Classifier
    {
        public static Dictionary<string, Dictionary<string, int>> Categories;

        public Classifier()
        {
            if (Classifier.Categories == null)
            {
                Classifier.Categories = new Dictionary<string, Dictionary<string, int>>();
            }
        }

        /// <summary>
        /// Adds a category to our classifier
        /// </summary>
        /// <param name="name">The name of the category we want to add</param>
        protected void AddCategory(string name)
        {
            Classifier.Categories.Add(name, new Dictionary<string, int>());
        }

        /// <summary>
        /// Trains a category with given sample text
        /// </summary>
        /// <param name="category">The name of the category we want to train</param>
        /// <param name="text">The text we're going to tokenize and train with</param>
        public void TrainCategory(string category, string text)
        {
            // Adding the category if it doesn't exist
            if (!Classifier.Categories.ContainsKey(category))
            {
                this.AddCategory(category);
            }

            // training our category with each token from the sample text
            foreach (var token in this.Tokenize(text))
            {
                // If this token doesn't exist in this category yet, we add it.
                if (!Classifier.Categories[category].ContainsKey(token))
                {
                    Classifier.Categories[category].Add(token, 0);
                }

                // Each instance of a token increments its count (weight) in a given category
                Classifier.Categories[category][token]++;
            }
        }

        /// <summary>
        /// Untrains a category with given sample text
        /// </summary>
        /// <param name="category">The name of the category we want to untrain</param>
        /// <param name="text">The text we're going to tokenize and untrain with</param>
        public void UntrainCategory(string category, string text)
        {
            // If this category doesn't exist, we just return
            if (!Classifier.Categories.ContainsKey(category))
            {
                return;
            }

            // untraining our category with each token from the sample text
            foreach (var token in this.Tokenize(text))
            {
                // If this token doesn't exist in this category, we just skip it
                if (!Classifier.Categories[category].ContainsKey(token))
                {
                    continue;
                }

                // We don't train below zero.
                if (Classifier.Categories[category][token] == 0)
                {
                    continue;
                }

                // Each instance of a token decreases its count (weight) in a given category
                Classifier.Categories[category][token]--;
            }
        }

        /// <summary>
        /// Tokenizes the text so we can train the classifier with it
        /// </summary>
        /// <param name="text">the sample text we want to train</param>
        /// <returns>List of tokens</returns>
        protected List<string> Tokenize(string text)
        {
            var workingText = text.Split(null);

            var tokenizedText = new List<string>();

            // Only including tokens that are longer than 2 characters
            foreach (string token in workingText)
            {
                if (token.Length > 2)
                {
                    tokenizedText.Add(token);
                }
            }

            return tokenizedText;
        }
    }
}
