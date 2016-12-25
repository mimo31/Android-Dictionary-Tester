using Android.App;
using Android.Widget;
using Android.OS;
using Android.Database;
using Android.Views;
using Java.Lang;
using System;
using DictionaryBase;

namespace com.github.mimo31.adictionarytester
{
    [Activity(Label = "Dictionary Tester", MainLauncher = true, Icon = "@drawable/icon")]
    public class DicListActivity : ListActivity
    {

        protected bool DictionariesAvailable { get; private set; }
        private int[] DictionaryVersions { get; set; }
        

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            base.SetContentView (Resource.Layout.DicList);

            ListView list = this.FindViewById<ListView>(Android.Resource.Id.List);
            string[] strings = new string[] { "hey", "it", "is", "me", "yes", "of", "course", "it", "is", "what", "did", "you", "think" };
            this.ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, strings);
        }

        protected void UpdateDictionaries()
        {
            
        }
    }
}

