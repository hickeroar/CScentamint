using System;
using System.Collections.Generic;
using System.Dynamic;
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
        /// Counts the occurance of each token in our string
        /// </summary>
        /// <param name="words">list of all the tokens in the sample text</param>
        /// <returns>dictionary of counts of each token in sample text</returns>
        protected Dictionary<string, int> CountTokenOccurances(List<string> words)
        {
            var counts = new Dictionary<string, int>();

            foreach (var word in words)
            {
                if (!counts.ContainsKey(word))
                {
                    counts[word] = 0;
                }

                counts[word]++;
            }

            return counts;
        }

        /// <summary>
        /// Classifies a sample of text
        /// </summary>
        /// <param name="text">sampe text that we want to classify</param>
        /// <returns>resulting category name</returns>
        public ExpandoObject Classify(string text)
        {
            var scores = this.Score(text);

            dynamic result = new ExpandoObject();

            if (scores.Count() == 0)
            {
                result.result = null;
                return result;
            }

            KeyValuePair<string, float> maxScore = scores.First();
            foreach (var score in scores)
            {
                if (score.Value > maxScore.Value)
                {
                    maxScore = score;
                }
            }

            result.result = maxScore.Key;

            return result;
        }

        /// <summary>
        /// Scores sample text
        /// </summary>
        /// <param name="text">sample text that we want to score</param>
        /// <returns>dictionary of scores</returns>
        public Dictionary<string, float> Score(string text)
        {
            // getting the occurance counts for each token in our sample text
            var occurs = this.CountTokenOccurances(this.Tokenize(text));

            var workingScores = new Dictionary<string, float>();

            // Setting up temp dictionary of scores for each category
            foreach (var category in Classifier.Categories)
            {
                workingScores[category.Key] = 0;
            }

            // Looping through each token to calculate its score
            foreach (var token in occurs)
            {
                var tokenScores = new Dictionary<string, int>();

                string word = token.Key;
                int count = token.Value;

                // Getting the per-category counts of tokens for later probability calculations
                foreach (var category in Classifier.Categories)
                {
                    string cat = category.Key;
                    Dictionary<string, int> categoryData = category.Value;

                    if (categoryData.ContainsKey(word))
                    {
                        tokenScores[cat] = categoryData[word];
                    }
                    else
                    {
                        tokenScores[cat] = 0;
                    }
                }

                // tally of all instances of this token from all categories
                int tokenTally = tokenScores.Sum(x => x.Value);

                if (tokenTally == 0)
                {
                    continue;
                }

                // Calculating bayes probabiltity for this token
                // http://en.wikipedia.org/wiki/Naive_Bayes_spam_filtering
                foreach (var category in tokenScores)
                {
                    string cat = category.Key;
                    int score = category.Value;

                    workingScores[cat] += count * this.CalculateBayesianProbability(cat, score, tokenTally);
                }
            }


            var scores = new Dictionary<string, float>();

            // Assembling the final scores from any scores that are greater than 0
            foreach (var score in workingScores)
            {
                string cat = score.Key;
                float scr = score.Value;

                if (scr > 0)
                {
                    scores[cat] = scr;
                }
            }

            return scores;
        }

        /// <summary>
        /// Calculates the bayesian probability for a given token for a given category
        /// </summary>
        /// <param name="category">the category that we're scoring for this token</param>
        /// <param name="tokenScore">the tally of this token for this category</param>
        /// <param name="tokenTally">the tally of this token for all categories</param>
        /// <returns>bayesian probability</returns>
        protected float CalculateBayesianProbability(string category, int tokenScore, int tokenTally)
        {
            // P that any given token IS in this category
            float prc = Classifier.Probabilities[category]["prc"];
            // P that any given token is NOT in this category
            float prnc = Classifier.Probabilities[category]["prnc"];
            // P that this token is NOT of this category
            float prtnc = (tokenTally - tokenScore) / tokenTally;
            // P that this token IS of this category
            float prtc = tokenScore / tokenTally;

            // Assembling the parts of the bayes equation
            float numerator = prtc * prc;
            float denominator = (numerator + (prtnc + prnc));

            if (denominator != 0.0)
            {
                return numerator / denominator;
            }
            else
            {
                return new float();
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
