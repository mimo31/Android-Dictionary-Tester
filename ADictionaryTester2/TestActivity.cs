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
using Android.Views.InputMethods;

namespace com.github.mimo31.adictionarytester
{
    /**
     * Test the user of a Dictionary which's index is passed in the intent.
     */
    [Activity(Label = "Dictionary Tester")]
    public class TestActivity : Activity
    {
        /**
         * Index of the Application class's Dictionary array of the Dictionary this Activity tests of.
         */
        private int Index { get; set; }

        /**
         * State of the ongoing test.
         */
        private TestState State { get; set; }

        /**
         * Pointer to the Application object.
         */
        private SubApplication app;

        /**
         * Activity's answer EditText.
         */
        private EditText AnswerEditText { get; set; }

        /**
         * Activity's question TextView.
         */
        private TextView QuestionTextView { get; set; }

        /**
         * Activity's response TextView that display whether the last answer was correct.
         */
        private TextView ResponseTextView { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.SetContentView(Resource.Layout.Test);

            this.app = (SubApplication)this.Application;

            this.Index = this.Intent.GetIntExtra("DIndex", -1);

            // get the test state from the Application object
            this.State = this.app.TestStates[this.Index];

            this.AnswerEditText = this.FindViewById<EditText>(Resource.Id.answerEditText);
            this.QuestionTextView = this.FindViewById<TextView>(Resource.Id.questionTextView);
            this.ResponseTextView = this.FindViewById<TextView>(Resource.Id.responseTextView);

            this.QuestionTextView.Text = this.State.GetNextQuestion();

            // assigns the event when the user confirms their answer
            this.AnswerEditText.EditorAction += this.AnswerSubmitted;

            // automatically pops up the keyboard
            this.AnswerEditText.RequestFocus();
            this.Window.SetSoftInputMode(SoftInput.StateVisible);

            // set the response TextView accordingly to the saved instace state
            if (savedInstanceState == null)
            {
                this.ResponseTextView.Text = "";
            }
            else
            {
                string response = savedInstanceState.GetString("Response");
                this.ResponseTextView.Text = response;
                Android.Graphics.Color textColor;
                if (response.Equals("Correct!"))
                {
                    textColor = Android.Graphics.Color.Green;
                }
                else
                {
                    textColor = Android.Graphics.Color.Red;
                }
                this.ResponseTextView.SetTextColor(textColor);
            }

            this.Title = this.app.Dictionaries[this.Index].Name;
        }

        protected override void OnResume()
        {
            base.OnResume();

            this.app.ForegroundActivity = this;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            
            // save the text of the response TextView to the Bundle
            outState.PutString("Response", this.ResponseTextView.Text);
        }

        /**
         * Handles the user's submit of their answer.
         */
        protected void AnswerSubmitted(object sender, EventArgs e)
        {
            // the answer the user has entered
            string userAnswer = this.AnswerEditText.Text;

            // whether their answer is correct
            bool correct = this.State.PutAnswer(userAnswer);

            // set the response TextView accordingly
            if (correct)
            {
                this.ResponseTextView.Text = "Correct!";
                this.ResponseTextView.SetTextColor(Android.Graphics.Color.Green);
            }
            else
            {
                this.ResponseTextView.Text = userAnswer + '\n' + this.State.LastCorrect;
                this.ResponseTextView.SetTextColor(Android.Graphics.Color.Red);
            }

            // put on the next question
            this.QuestionTextView.Text = this.State.GetNextQuestion();

            // reset the answer EditText
            this.AnswerEditText.Text = "";

            // if the test is completed, show the results in the CompletedActivity and close finish this one,
            // since the user should not be able to navigate back to this Activity
            if (this.State.Finished)
            {
                Intent intent = new Intent(this, typeof(CompletedActivity));
                intent.PutExtra("DIndex", this.Index);
                this.StartActivity(intent);
                this.Finish();
            }
        }

        protected override void OnStop()
        {
            base.OnStop();

            // request a save of the test states as the test if now paused (the Activity was quit for whatever reason) or the test is done
            this.app.PostSaveStates();
        }

    }
}