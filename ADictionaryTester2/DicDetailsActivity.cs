using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DictionaryBase;

namespace com.github.mimo31.adictionarytester
{
    /**
     * Shows details of a particular Dictionary a allows the user to navigate to a test of that Dictionary.
     */
    [Activity(Label = "Dictionary Tester")]
    public class DicDetailsActivity : Activity
    {
        /**
         * Index of the Application class's Dictionary array of the Dictionary this Activity displays details about.
         */
        private int Index { get; set; }

        /**
         * Pointer to the Application class.
         */
        private SubApplication app;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            base.SetContentView(Resource.Layout.DicDetails);

            // get a pointer to the Application class
            this.app = (SubApplication)this.Application;

            // get the Dictionary index from the intent
            this.Index = this.Intent.GetIntExtra("DIndex", -1);

            // set the Activity's properties the data of the Dictionary
            Dictionary dictionary = this.app.Dictionaries[this.Index];
            this.FindViewById<TextView>(Resource.Id.nameTextView).Text = dictionary.Name;
            this.FindViewById<TextView>(Resource.Id.identifierTextView).Text = dictionary.Identifier;
            this.FindViewById<TextView>(Resource.Id.authorTextView).Text = dictionary.Author;
            this.FindViewById<TextView>(Resource.Id.creationTimeTextView).Text = dictionary.CreatedOn.ToString();
            this.Title = dictionary.Name;

            // load the test states if not alreay loaded
            if (this.app.TestStates == null)
            {
                this.app.LoadStates();
            }
            Button startButton = this.FindViewById<Button>(Resource.Id.testButton);
            Button continueButton = this.FindViewById<Button>(Resource.Id.continueButton);
            // if there is no test state for this Dictionary, remove the continue button and make the new test button match parent
            if (this.app.TestStates[this.Index] == null)
            {
                ((ViewGroup)continueButton.Parent).RemoveView(continueButton);
                RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                layout.AddRule(LayoutRules.AlignParentBottom);
                startButton.LayoutParameters = layout;
            }
            else
            {
                continueButton.Click += this.ContinueTestClicked;
            }
            startButton.Click += this.NewTestClicked;
        }

        /**
         * Starts a test of the Dictionary by opening the test Activity and passing it the current Dictionary index.
         */
        private void StartTest()
        {
            //TODO open the test Activiry that finally tests the user of the Dictionary and continues the test from the state in the test states array
        }

        /**
         * Handles a click of the continue button. Just lets a test start (continue).
         */
        private void ContinueTestClicked(object sender, EventArgs e)
        {
            this.StartTest();
        }

        /**
         * Handles a click of the new test button. Resets the test state of a new test and starts the test.
         */
        private void NewTestClicked(object sender, EventArgs e)
        {
            // reset the state of the test
            this.app.TestStates[this.Index] = new TestState(this.app.Dictionaries[this.Index]);

            this.StartTest();
        }

        protected override void OnResume()
        {
            base.OnResume();

            this.app.ForegroundActivity = this;
        }
    }
}