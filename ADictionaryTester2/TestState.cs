using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DictionaryBase;

namespace com.github.mimo31.adictionarytester
{

    /**
     * Represents the user test of a particular Dictionary at one point of time.
     * Serves the questions to be asked and 
     */
    class TestState
    {
        /**
         * Determines whether the questions was aswered correctly the last time. Corresponds to the list of DictionaryEntries in the Dictionary.
         */
        private bool[] AnsRight { get; set; }

        /**
         * The array of question indexes that will be asked in the current question batch. These are indexes of the Dictionary's DictionariyEntries list.
         */
        private int[] CurrentTestList { get; set; }

        /**
         * The index of CurrentTestList of the question that will be asked next.
         */
        private int CurrentTestItem { get; set; } = 0;

        /**
         * Indicates whether the test has been already finished.
         */
        public bool Finished { get; private set; } = false;

        /**
         * The correct answer to the last wrongly answered question.
         */
        public string LastCorrect { get; private set; }

        /**
         * The number of questions asked in this test up to this point.
         */
        public int QuestionsAsked { get; private set; }

        /**
         * The number of correctly answered questions in this test up to this point.
         */
        public int RightAnswers { get; private set; }

        /**
         * The fraction of correctly answered questions of all asked questions in this test up to this point.
         */
        public double Accuracy { get
            {
                if (this.QuestionsAsked == 0)
                {
                    return 1;
                }
                return this.RightAnswers / (double)this.QuestionsAsked;
            } }

        /**
         * The Dictionary this is a test of.
         */
        private Dictionary Dic;

        /**
         * Random object that is used to shuffle the questiosns. 
         */
        private Random R = new Random();

        /**
         * Creates a new test state representing the beginning of a new test.
         */
        public TestState(Dictionary dic)
        {
            this.Dic = dic;

            // set all questions as yet answered incorrectly
            this.AnsRight = new bool[this.Dic.GetNumberOfEntries()];

            // create a list of the next questions with a random permutation of the indexes 0 to the number of DictionaryEntires
            this.CurrentTestList = Enumerable.Range(0, this.Dic.GetNumberOfEntries()).OrderBy(v1 => this.R.NextDouble()).ToArray();
        }

        private TestState()
        {

        }

        /**
         * Loads the test state from a stream.
         */
        public static TestState Load(Dictionary dic, BinaryReader reader)
        {
            TestState obj = new TestState();
            obj.Dic = dic;

            // read the number of questions to ask in the current question batch
            obj.CurrentTestList = new int[reader.ReadInt32()];

            // read the questions indexes of the current batch
            for (int i = 0; i < obj.CurrentTestList.Length; i++)
            {
                obj.CurrentTestList[i] = reader.ReadInt32();
            }

            // read the state index of the current batch
            obj.CurrentTestItem = reader.ReadInt32();

            // read the total number of questions (which can also be read from the Dictionary object)
            obj.AnsRight = new bool[reader.ReadInt32()];

            // read which questions were answered correctly
            for (int i = 0; i < obj.AnsRight.Length; i++)
            {
                obj.AnsRight[i] = reader.ReadBoolean();
            }

            // read the numbers of asked questions
            obj.QuestionsAsked = reader.ReadInt32();
            obj.RightAnswers = reader.ReadInt32();

            return obj;
        }

        /**
         * Reads the data representing a next test state in a stream to skip it when it's not actually needed.
         */
        public static void SkipInStream(BinaryReader reader)
        {
            // read all the questions indexes of the batch
            int testListLength = reader.ReadInt32();
            for (int i = 0; i < testListLength; i++)
            {
                reader.ReadInt32();
            }

            // read the current index in the batch
            reader.ReadInt32();

            // read all the data about the correctness of answers
            int answeredLength = reader.ReadInt32();
            for (int i = 0; i < answeredLength; i++)
            {
                reader.ReadBoolean();
            }
            
            // read the numbers of asked questions
            reader.ReadInt32();
            reader.ReadInt32();
        }

        /**
         * Writes readable information from which this object can be restored to a stream.
         */
        public void Save(BinaryWriter writer)
        {
            // write all the questions indexes of the batch
            writer.Write(this.CurrentTestList.Length);
            for (int i = 0; i < this.CurrentTestList.Length; i++)
            {
                writer.Write(this.CurrentTestList[i]);
            }

            // write the current index in the batch
            writer.Write(this.CurrentTestItem);

            // write all the data about the correctness of answers
            writer.Write(this.AnsRight.Length);
            for (int i = 0; i < this.AnsRight.Length; i++)
            {
                writer.Write(this.AnsRight[i]);
            }

            // write the numbers of asked questions
            writer.Write(this.QuestionsAsked);
            writer.Write(this.RightAnswers);
        }

        /**
         * Returns the question the user is now being asked.
         * Returns the same value until the next call of PutAnswer.
         * Returns an empty string if the test is already finished.
         */
        public string GetNextQuestion()
        {
            if (this.Finished)
            {
                return "";
            }
            int entryIndex = this.CurrentTestList[this.CurrentTestItem];
            return this.Dic.GetEntry(entryIndex).Question;
        }
        
        /**
         * Submits the user's answer and returns whether it's correct.
         */
        public bool PutAnswer(string ans)
        {
            this.QuestionsAsked++;

            // the Dictionary entry the current question is from
            int entryIndex = this.CurrentTestList[this.CurrentTestItem];

            // the correct answer
            string correctAnswer = this.Dic.GetEntry(entryIndex).Answer;

            // whether the user's answer is correct
            bool correct = correctAnswer.Equals(ans);

            if (!correct)
            {
                this.LastCorrect = correctAnswer;
            }
            else
            {
                this.RightAnswers++;
            }

            this.AnsRight[entryIndex] = correct;

            this.CurrentTestItem++;

            // if the end of the current question batch has been reached
            if (this.CurrentTestItem == this.CurrentTestList.Length)
            {
                // the indexes of questions that will appear in the next question batch
                List<int> newIndexes = new List<int>();

                // the indexes of correctly answered questions
                List<int> rightAnsIndexes = new List<int>();

                // load all the incorrectly answered indexes into newIndexes and populate the list of correctly answered indexes
                for (int i = 0; i < this.AnsRight.Length; i++)
                {
                    if (!this.AnsRight[i])
                    {
                        newIndexes.Add(i);
                    }
                    else
                    {
                        rightAnsIndexes.Add(i);
                    }
                }

                // if there are no incorrectly answered indexes, the test is finished
                if (newIndexes.Count == 0)
                {
                    this.Finished = true;
                    return correct;
                }

                // number of already correctly answered questions that will be asked too
                int extraCount = (int)Math.Sqrt(newIndexes.Count * (double)this.AnsRight.Length) - newIndexes.Count;

                // shuffle the list of correctly answered indexes
                rightAnsIndexes = rightAnsIndexes.OrderBy(v => this.R.NextDouble()).ToList();

                // add the first extraCount correctly answered indexes to the new question batch
                newIndexes.AddRange(rightAnsIndexes.Take(extraCount));

                // shuffle the new question batch
                this.CurrentTestList = newIndexes.OrderBy(v => this.R.NextDouble()).ToArray();

                this.CurrentTestItem = 0;
            }
            return correct;
        }
    }
}
