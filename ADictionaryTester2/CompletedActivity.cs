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

namespace com.github.mimo31.adictionarytester
{
    /**
     * Shows the information about the user's performance after a test.
     */
    [Activity(Label = "Dictionary Tester")]
    public class CompletedActivity : Activity
    {

        /**
         * Index of the Application's Dictionaries array of the Dictionary to which belongs the completed test.
         */
        private int Index { get; set; }

        /**
         * Pointer to the Application object.
         */
        private SubApplication app;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.SetContentView(Resource.Layout.Completed);

            this.Index = this.Intent.GetIntExtra("DIndex", -1);

            this.app = (SubApplication)this.Application;

            // get the test state from the Application object
            TestState state = this.app.TestStates[this.Index];

            // set the views's properties according to the test state
            this.FindViewById<TextView>(Resource.Id.questionsAskedTextView).Text = state.QuestionsAsked.ToString();
            this.FindViewById<TextView>(Resource.Id.accuracyTextView).Text = (state.Accuracy * 100).ToString("F") + " %";

            // set the click handler for the ok button
            this.FindViewById<Button>(Resource.Id.okButton).Click += this.OkButtonClicked;

            this.Title = this.app.Dictionaries[this.Index].Name;

            // delete the test state as the test is already finished
            this.app.TestStates[this.Index] = null;
        }

        private void OkButtonClicked(object sender, EventArgs e)
        {
            this.OnBackPressed();
        }

        protected override void OnResume()
        {
            base.OnResume();

            this.app.ForegroundActivity = this;
        }
    }
}