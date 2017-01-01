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
        }

        // TODO: code functions that serve the next question
    }
}