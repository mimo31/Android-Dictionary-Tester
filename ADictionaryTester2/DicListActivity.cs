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
     * Shows a list of Dictionaries with identifier as specified in the Intent.
     */
    [Activity(Label = "Dicionary Tester")]
    public class DicListActivity : ListActivity
    {
        /**
         * Pointer to the Application class.
         */
        private SubApplication app;

        /**
         * Identifier of the Dictionaries shown in this instance of the Activity.
         */
        private string Identifier { get; set; }

        /**
         * Indexes of the Application class's Dictionary array corresponding respectively to the Dictionaries shown.
         */
        private int[] DictionaryIndexes { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            base.SetContentView(Resource.Layout.DicList);

            // get a pointer to the Application class
            this.app = (SubApplication)this.Application;

            // get the identifier from the intent
            this.Identifier = this.Intent.GetStringExtra("Identifier");

            // add the click handler to the list items
            ListView list = this.FindViewById<ListView>(Android.Resource.Id.List);
            list.ItemClick += this.OnListItemClicked;

            // add the click handler to the refresh button
            Button refreshButton = this.FindViewById<Button>(Resource.Id.refbutton);
            refreshButton.Click += (sender, e) => {
                this.app.PostDictionaryUpdate();
            };

            // set the title accordingly to the identifier
            this.Title = this.Identifier + " Dictionaries";

            // update the list of Dictionaries
            this.UpdateDictionaryList();
        }

        protected override void OnResume()
        {
            base.OnResume();

            this.app.ForegroundActivity = this;
        }

        /**
         * Updates the list of Dictionaries accordingly to the Dictionary array in the Application class.
         * Should be always run on the UI thread.
         */
        private void UpdateDictionaryList()
        {
            // respective list of names and Aplication class's Dictionary array indexes of the Dictionaries that will be shown in the list
            List<string> names = new List<string>();
            List<int> indexes = new List<int>();
            
            // find all the Dictionaries with the identifier of this Activity instance
            for (int i = 0; i < this.app.Dictionaries.Length; i++)
            {
                Dictionary dictionary = this.app.Dictionaries[i];
                if (dictionary.Identifier.Equals(this.Identifier))
                {
                    names.Add(dictionary.Name);
                    indexes.Add(i);
                }
            }

            // if there are no such Dictionaries, show back the Activity with a list of identifiers
            if (indexes.Count == 0)
            {
                this.StartActivity(typeof(IdenListActivity));
                return;
            }

            this.DictionaryIndexes = indexes.ToArray();
            this.ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, names.ToArray());
        }

        private void OnListItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            Intent intent = new Intent(this, typeof(DicDetailsActivity));
            intent.PutExtra("DIndex", this.DictionaryIndexes[e.Position]);
            this.StartActivity(intent);
        }

        private void OnDictionariesUpdate(object sender, EventArgs e)
        {
            // updates the Dictionary list but on the UI thread
            this.RunOnUiThread(() => this.UpdateDictionaryList());
        }

        protected override void OnStart()
        {
            base.OnStart();
            
            this.app.DictionariesUpdated += this.OnDictionariesUpdate;
        }

        protected override void OnStop()
        {
            base.OnStop();

            this.app.DictionariesUpdated -= this.OnDictionariesUpdate;

            // save request a save of the Dictionaries
            this.app.PostDictionarySave();
        }
    }
}