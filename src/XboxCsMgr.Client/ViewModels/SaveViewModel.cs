using Stylet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using XboxCsMgr.Client.ViewModels.Controls;
using XboxCsMgr.XboxLive;
using XboxCsMgr.XboxLive.Model.TitleStorage;
using XboxCsMgr.XboxLive.Services;
namespace XboxCsMgr.Client.ViewModels
{
    public class SaveViewModel : Stylet.Screen
    {
        private IEventAggregator _events;
        private XboxLiveConfig _xblConfig;
        private TitleStorageService _storageService;

        private string _packageFamilyName;
        public string PackageFamilyName
        {
            get => _packageFamilyName;
            set
            {
                _packageFamilyName = value;
            }
        }

        private string _serviceConfigurationId;
        public string ServiceConfigurationId
        {
            get => _serviceConfigurationId;
            set
            {
                _serviceConfigurationId = value;
            }
        }

        private ObservableCollection<SavedBlobsViewModel> _saveData;
        public ObservableCollection<SavedBlobsViewModel> SaveData
        {
            get => _saveData;
        }

        public SavedAtomsViewModel? SelectedAtom
        {
            get;
            set;
        }
        public SavedBlobsViewModel? SelectedBlob
        {
            get;
            set;
        }
        public SavedBlobsViewModel? SelectedAtomsParent
        {
            get;
            set;
        }
        public SaveViewModel(IEventAggregator events, XboxLiveConfig config, string pfn, string scid)
        {
            _events = events;
            _xblConfig = config;
            _packageFamilyName = pfn;
            _serviceConfigurationId = scid;

            _storageService = new TitleStorageService(_xblConfig, _packageFamilyName, _serviceConfigurationId);
            _saveData = new ObservableCollection<SavedBlobsViewModel>();

            FetchSaveMetadata();
        }

        private async void FetchSaveMetadata()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                TitleStorageBlobMetadataResult blobMetadataResult = await _storageService.GetBlobMetadata();
                if (blobMetadataResult != null && blobMetadataResult.Blobs != null)
                {
                    foreach (TitleStorageBlobMetadata entry in blobMetadataResult.Blobs)
                    {
                        _saveData.Add(new SavedBlobsViewModel(_storageService, entry));
                    }
                }
            }
            catch
            {
                var fakemetadata = new TitleStorageBlobMetadata();
                fakemetadata.DisplayName = "No saves found.";
                var fakesave = new SavedBlobsViewModel(_storageService, fakemetadata);
                _saveData.Add(fakesave);
            }
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
        }

        public void SelectedItemChanged(object args)
        {
            SelectedBlob = args as SavedBlobsViewModel;
            SelectedAtom = args as SavedAtomsViewModel;
        }

        public async void Download()
        {
            if (SelectedAtom == null)
                return;

            string atom = SelectedAtom.AtomValue;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = SelectedAtom.AtomName;

            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                byte[] atomData = await _storageService.DownloadAtomAsync(atom);
                await File.WriteAllBytesAsync(dlg.FileName, atomData);
            }
        }
        public async void DownloadFolder()
        {
            Debug.WriteLine("Download Folder Called");
            if (SelectedBlob == null)
                return;
            Debug.WriteLine("Not Null");
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            var res = fbd.ShowDialog();
            if (res.HasFlag(DialogResult.OK))
            {
                var allatoms = await _storageService.GetBlobAtoms(SelectedBlob.BlobName);
                Progress prg = new Progress();
                prg.downtxt.Text = "Downloading";
                prg.prgbar.Maximum = allatoms.Atoms.Count;
                prg.prgbar.Value = 0;
                prg.Show();
                foreach (var atom in allatoms.Atoms)
                {
                    prg.downtxt.Text = "Downloading " + atom.Key.ToString();
                    byte[] atomData = await _storageService.DownloadAtomAsync(atom.Value);
                    await File.WriteAllBytesAsync(Path.Join(fbd.SelectedPath,atom.Key), atomData);
                    prg.prgbar.Value++;
                }
                prg.Close();
            }
        }
        public async void Upload()
        {
            //MICROSOFT REMOVED PERMISSION TO UPLOAD
            // nathan brown here
            if (SelectedAtom == null)
                return;

            // Remove the file type, just need the UUID
            var ogatom = SelectedAtom;
            string atom = SelectedAtom.AtomValue.Substring(0, SelectedAtom.AtomValue.IndexOf(','));
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = SelectedAtom.AtomName;
            Debug.WriteLine(atom);
            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                byte[] atomData = await File.ReadAllBytesAsync(dlg.FileName);
                await _storageService.UploadBlobAsync(ogatom.parentblobMetadata, atomData, atom);
            }
        }
    }
}
