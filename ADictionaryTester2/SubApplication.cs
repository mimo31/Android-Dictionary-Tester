using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
     * A derivative of the Application class to hold all Application-global data and provide Application-global methods.
     */
    [Application]
    class SubApplication : Application
    {
        /**
         * Whether Dictionaries were succefully fetched in any way.
         */
        public bool DictionariesAvailable { get; private set; } = false;

        /**
         * An array of all loaded Dictionaries.
         */
        public Dictionary[] Dictionaries { get; private set; } = new Dictionary[0];

        /**
         * An array of versions of the Dictionaries. Corresponds to the array of the Dictionaries.
         */
        private int[] DictionaryVersions { get; set; } = new int[0];

        /**
         * An array of file names of the Dictionaries. Corresponds to the array of the Dictionaries.
         */
        private string[] DictionaryNames { get; set; } = new string[0];

        /**
         * An array of test states of the Dictionaries. Corresponds to the array of the Dictonaries.
         * When null, the states are not yet loaded.
         * When an element is null, that Dictionary has not a started test.
         */
        public TestState[] TestStates { get; private set; } = null;

        /**
         * An event triggered when an Dictionary update is completed.
         */
        public event EventHandler DictionariesUpdated;

        /**
         * Pointer to the last Foreground Activity of this Application.
         */
        public Activity ForegroundActivity { private get; set; }

        /**
         * URL of the remote directory where Dictionaries are stored.
         */
        const string REMOTE = "https://mimo31.github.io/Android-Dictionary-Tester/";

        public SubApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {

        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.PostDictionaryUpdate();
        }

        /**
         * Loads the states of the tests for all dictionaries from the local storage if a save exist. Then saves the states if some were loaded.
         * (the save is not useless because the saved states that hadn't a corresponding Dictionary were ignored in loading)
         */
        public void LoadStates()
        {
            // init the array to an array full of nulls
            this.TestStates = new TestState[this.Dictionaries.Length];

            // the path of the states save
            string path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "states.dat");

            if (File.Exists(path))
            {
                FileStream inputStream = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(inputStream);

                // number of saved states
                int entries = reader.ReadInt32();
                while (entries-- != 0)
                {
                    // filename and version of the Dictionary of which is this a test state
                    string filename = reader.ReadString();
                    int version = reader.ReadInt32();

                    bool found = false;

                    // look for a Dictionary with such filename, if found, loads the state
                    for (int i = 0; i < this.Dictionaries.Length; i++)
                    {
                        if (this.DictionaryNames[i].Equals(filename) && this.DictionaryVersions[i] >> 16 == version >> 16)
                        {
                            this.TestStates[i] = TestState.Load(this.Dictionaries[i], reader);
                            found = true;
                            break;
                        }
                    }

                    // if not found, skips the state data
                    if (!found)
                    {
                        TestState.SkipInStream(reader);
                    }
                }
                reader.Close();
                inputStream.Close();

                // save the states
                this.PostSaveStates();
            }
        }

        /**
         *  Calls for a save of the test states to local storage asynchronously.
         */
        public void PostSaveStates()
        {
            ((Action)this.SaveStates).BeginInvoke(null, null);
        }

        /**
         * Saves the test states to local storage.
         */
        public void SaveStates()
        {
            // if nothing loaded
            if (this.TestStates == null)
            {
                return;
            }

            // path to the test states file
            string path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "states.dat");

            FileStream outputStream = new FileStream(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(outputStream);

            // find the number states to save
            int entries = 0;
            for (int i = 0; i < this.TestStates.Length; i++)
            {
                if (this.TestStates[i] != null)
                {
                    entries++;
                }
            }

            writer.Write(entries);

            // save all the test states with the Dictionary filenames and versions
            for (int i = 0; i < this.TestStates.Length; i++)
            {
                if (this.TestStates[i] != null)
                {
                    writer.Write(this.DictionaryNames[i]);
                    writer.Write(this.DictionaryVersions[i]);
                    this.TestStates[i].Save(writer);
                }
            }

            writer.Close();
            outputStream.Close();
        }

        /**
         * Saves the loaded data about Dictionaries to the device.
         */
        private void SaveDictionariesToLocal()
        {
            // file names and versions of the previously saved Dictionaries
            List<string> names = new List<string>();
            List<int> versions = new List<int>();

            // path where the data can be saved
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string metaPath = Path.Combine(path, "meta.txt");

            // load the data about what's already saved
            if (File.Exists(metaPath))
            {
                FileStream inputStream = new FileStream(metaPath, FileMode.Open);
                StreamReader reader = new StreamReader(inputStream);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] splits = line.Split(' ');
                    names.Add(splits[0]);
                    versions.Add(int.Parse(splits[1]));
                }
                reader.Close();
                inputStream.Close();
            }

            // save each of the Dictionaries if necessary
            for (int i = 0; i < this.Dictionaries.Length; i++)
            {
                string name = this.DictionaryNames[i];
                bool present = false;
                for (int j = 0; j < names.Count; j++)
                {
                    if (names[j].Equals(name))
                    {
                        if (versions[j] >= this.DictionaryVersions[i])
                        {
                            present = true;
                        }
                        break;
                    }
                }
                if (!present)
                {
                    this.Dictionaries[i].Save(Path.Combine(path, name));
                }
            }
        }

        /**
         * Starts a Dictionary update asynchronously.
         */
        public void PostDictionaryUpdate()
        {
            Action updateAction = () => this.UpdateDictionaries();
            updateAction.BeginInvoke(null, null);
        }

        /**
         * Shows a Toast via the last Foreground Activity.
         */
        private void ShowToast(string message, ToastLength length)
        {
            if (this.ForegroundActivity != null)
            {
                this.ForegroundActivity.RunOnUiThread(() => Toast.MakeText(this.ApplicationContext, "Dictionaries Updated from the Internet", ToastLength.Short).Show());
            }
        }
        
        /**
         * Updates the Dictionaries.
         */
        private void UpdateDictionaries()
        {
            if (TryLoadRemote())
            {
                this.ShowToast("Dictionaries Updated from the Internet", ToastLength.Short);
                this.DictionariesUpdated(this, new EventArgs());
                return;
            }

            if (this.DictionariesAvailable)
            {
                this.ShowToast("Internet Connection not Available, Dictionaries not Updated", ToastLength.Short);
                this.DictionariesUpdated(this, new EventArgs());
                return;
            }

            if (this.TryLoadLocal())
            {
                this.ShowToast("Internet Connection not Available, Dictionaries Loaded from the Device", ToastLength.Short);
            }
            else
            {
                this.ShowToast("Internet Connection not Available, Dictionaries not Updated", ToastLength.Short);
            }
            this.DictionariesUpdated(this, new EventArgs());
        }

        /**
         * Looks for locally saved Dictionaries and loads them if they're present.
         * Potentialy overwrites all already loaded Dictionaries.
         * Returns whether the save was successfully loaded.
         */
        private bool TryLoadLocal()
        {
            // path where the data can be saved
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string metaPath = Path.Combine(path, "meta.txt");

            // if there is actualy something locally saved
            if (!File.Exists(metaPath))
            {
                return false;
            }
            try
            {
                // load the Dictionaries recorded in the meta file 
                FileStream inputStream = new FileStream(metaPath, FileMode.Open);
                StreamReader reader = new StreamReader(inputStream);
                List<Dictionary> dictionaries = new List<Dictionary>();
                List<string> names = new List<string>();
                List<int> versions = new List<int>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] splits = line.Split(' ');
                    string name = splits[0];
                    int version = int.Parse(splits[1]);
                    string dicPath = Path.Combine(path, name);
                    FileStream dicStream = new FileStream(dicPath, FileMode.Open);
                    Dictionary readDictionary = new Dictionary(dicStream);
                    dicStream.Close();
                    dictionaries.Add(readDictionary);
                    names.Add(name);
                    versions.Add(version);
                }
                inputStream.Close();
                this.Dictionaries = dictionaries.ToArray();
                this.DictionaryNames = names.ToArray();
                this.DictionaryVersions = versions.ToArray();
                this.DictionariesAvailable = true;
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }

        /**
         * Tries to connect to the remote and update the set of loaded Dictionaries accordingly.
         * Returns whether the download was successfull.
         */
        private bool TryLoadRemote()
        {
            try
            {
                // fetch the remote meta file
                WebRequest request = WebRequest.Create(REMOTE + "meta.txt");
                WebResponse response = request.GetResponse();

                StreamReader reader = new StreamReader(response.GetResponseStream());

                // data about the future loaded Dictionaries
                List<int> versions = new List<int>();
                List<Dictionary> dictionaries = new List<Dictionary>();
                List<string> dictionaryNames = new List<string>();

                // for each of the remote Dictionaries look for a local version and download if necessary
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] splits = line.Split(' ');
                    string name = splits[0];
                    int version = int.Parse(splits[1]);
                    bool present = false;
                    for (int i = 0; i < this.DictionaryNames.Length; i++)
                    {
                        if (this.DictionaryNames[i].Equals(name))
                        {
                            if (version > this.DictionaryVersions[i])
                            {
                                break;
                            }
                            dictionaryNames.Add(name);
                            versions.Add(this.DictionaryVersions[i]);
                            dictionaries.Add(this.Dictionaries[i]);
                            present = true;
                            break;
                        }
                    }
                    if (!present)
                    {
                        try
                        {
                            // fetch the Dictionary
                            WebRequest dicRequest = WebRequest.Create(REMOTE + name);
                            Dictionary downloadedDictionary = new Dictionary(dicRequest.GetResponse().GetResponseStream());
                            dictionaryNames.Add(name);
                            versions.Add(version);
                            dictionaries.Add(downloadedDictionary);
                        }
                        catch (System.Exception)
                        {

                        }
                    }
                }
                this.DictionaryVersions = versions.ToArray();
                this.Dictionaries = dictionaries.ToArray();
                this.DictionaryNames = dictionaryNames.ToArray();
                this.DictionariesAvailable = true;
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}