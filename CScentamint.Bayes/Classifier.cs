using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CScentamint.Bayes
{
    public class Classifier
    {
        // The categories 
        public static Dictionary<string, Dictionary<string, int>> Categories;
        public static Dictionary<string, Dictionary<string, float>> Probabilities;

        public Classifier()
        {
            if (Classifier.Categories == null && Classifier.Probabilities == null)
            {
                this.InitStorage();
            }
        }

        /// <summary>
        /// Initializes the storage variables for categories and probabilities
        /// </summary>
        protected void InitStorage()
        {
            Classifier.Categories = new Dictionary<string, Dictionary<string, int>>();
            Classifier.Probabilities = new Dictionary<string, Dictionary<string, float>>();
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
        /// Calculates/Caches per category probabilities.
        /// Essentially we're saving probabilities that any token will and won't be in a given category.
        /// </summary>
        protected void CalculateCategoryProbabilities()
        {
            int totalTally = 0;
            var tokenCounts = new Dictionary<string, int>();
            var probabilities = new Dictionary<string, float>();

            // Calculating a total tally as well as per category totals
            foreach (var category in Classifier.Categories)
            {
                var cat = category.Key;
                Dictionary<string, int> tokens = category.Value;

                totalTally += tokens.Count;
                tokenCounts[cat] = tokens.Count;
            }

            // Calculating a probability for each category (the chance that a given token is in each category)
            foreach (var category in tokenCounts)
            {
                string cat = category.Key;
                var count = category.Value;

                if (totalTally > 0)
                {
                    probabilities[cat] = (float) count / (float) totalTally;
                }
                else
                {
                    probabilities[cat] = 0.0f;
                }
            }

            // This should be 1.0, but we're doing the math because it's the right things to do.
            float probSum = probabilities.Sum(x => x.Value);

            // Saving probability values
            foreach (var category in probabilities)
            {
                string cat = category.Key;
                float prob = category.Value;

                var catProbability = new Dictionary<string, float>();

                // Probability that a given token is in this category
                catProbability["prc"] = prob;

                // Probability that a given token is NOT in this category
                catProbability["prnc"] = probSum - prob;

                Classifier.Probabilities[cat] = catProbability;
            }
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

            this.CalculateCategoryProbabilities();
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

            this.CalculateCategoryProbabilities();
        }

        /// <summary>
        /// Empties all token/probability storage
        /// </summary>
        public void Flush()
        {
            this.InitStorage();
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
