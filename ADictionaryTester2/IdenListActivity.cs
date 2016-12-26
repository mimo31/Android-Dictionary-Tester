using Android.App;
using Android.Widget;
using Android.OS;
using Android.Database;
using Android.Views;

using System;
using System.Collections.Generic;

using DictionaryBase;

namespace com.github.mimo31.adictionarytester
{

    /**
     * The first Acivity that should appear when the App is started.
     * Shows a list of all possible Dictionary Identifiers.
     */
    [Activity(Label = "Dictionary Tester", MainLauncher = true, Icon = "@drawable/icon")]
    public class IdenListActivity : ListActivity
    {
        /**
         * Pointer to the Application class.
         */
        private SubApplication app;

        /**
         * An array of identifiers shown in the list.
         */
        private string[] Identifiers { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            base.SetContentView (Resource.Layout.DicList);

            // add the click handler to the list
            ListView list = this.FindViewById<ListView>(Android.Resource.Id.List);
            list.ItemClick += this.OnListItemClicked;

            // add the click handler to the refresh button
            Button refreshButton = this.FindViewById<Button>(Resource.Id.refbutton);
            refreshButton.Click += (sender, e) => {
                this.app.PostDictionaryUpdate();
            };

            // get the instance of the Application class
            this.app = (SubApplication)this.Application;

            // add an event handler to the event when the dictionaries in the Application class are updated
            this.app.DictionariesUpdated += this.OnDictionariesUpdate;

            // update the list of identifiers
            this.ShowIdentifiers();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // set the foreground Activity in the Application class to this
            this.app.ForegroundActivity = this;
        }

        private void OnDictionariesUpdate(object sender, EventArgs e)
        {
            // update the list of identifiers but on the UI thread
            this.RunOnUiThread(() => this.ShowIdentifiers());
        }

        private void OnListItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            if (this.app.DictionariesAvailable)
            {
                string identifier = this.Identifiers[e.Position];
                // TODO start the activity with specific dictionaries of this identifier
            }
        }

        /**
         * Updates the list of identifiers accordingly to the data in the Application class.
         * Should be always run on the UI thread.
         */
        private void ShowIdentifiers()
        {
            // show No Dictionaries when Dictionaries aren't available
            if (!this.app.DictionariesAvailable)
            {
                this.ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, new string[] { "No Dictionaries" });
                return;
            }

            // construct a list of all distinct identifiers
            List<string> identifiers = new List<string>();
            foreach (Dictionary dic in this.app.Dictionaries)
            {
                string dicIdentifier = dic.Identifier;
                bool found = false;
                foreach (string ident in identifiers)
                {
                    if (ident.Equals(dicIdentifier))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    identifiers.Add(dicIdentifier);
                }
            }
            this.Identifiers = identifiers.ToArray();

            // update the items in the ListView
            this.ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, this.Identifiers);
        }
    }
}

