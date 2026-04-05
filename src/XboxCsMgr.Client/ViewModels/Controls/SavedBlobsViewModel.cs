using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XboxCsMgr.XboxLive.Model.TitleStorage;
using XboxCsMgr.XboxLive.Services;

namespace XboxCsMgr.Client.ViewModels.Controls
{
    public class SavedBlobsViewModel : TreeViewItemViewModel, INotifyPropertyChanged
    {
        private TitleStorageService _storageService;
        private TitleStorageBlobMetadata _blobMetadata;

        public TitleStorageBlobMetadata BlobMetadata
        {
            get => _blobMetadata;
        }

        public string BlobName
        {
            get => _blobMetadata.FileName;
        }
        private Visibility _ShowFolderIcon = Visibility.Visible;
        public Visibility ShowFolderIcon
        {
            get => _ShowFolderIcon;
            set
            {
                _ShowFolderIcon = value;
                OnPCImg("ShowFolderIcon");
            }
        }
        private Visibility _ShowImage = Visibility.Collapsed;
        public Visibility ShowImage {
            get => _ShowImage;
            set
            {
                _ShowImage = value;
                OnPCImg("ShowImage");
            }
        }
        private ImageSource? _ImageURI;
        public ImageSource? ImageURI 
        { get => _ImageURI;
            set
            {
                _ImageURI = value;
                OnPCImg("ImageURI");
            }
        }
        public string BlobDisplayName
        {
            get
            {
                if (_blobMetadata.DisplayName != null)
                {
                    Debug.WriteLine(_blobMetadata.DisplayName.Length);
                    return _blobMetadata.DisplayName;
                }
                else
                {
                    return _blobMetadata.FileName;
                }
            }
        }

        public SavedBlobsViewModel(TitleStorageService storageService, TitleStorageBlobMetadata blobMetadata) : base(null, true)
        {
            _storageService = storageService;
            _blobMetadata = blobMetadata;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPCImg(string varname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(varname));
        }

        protected override async void LoadChildren()
        {
            if (_blobMetadata.FileName != "No saves found.")
            {
                TitleStorageAtomMetadataResult atoms = await _storageService.GetBlobAtoms(_blobMetadata.FileName);
                foreach (string atom in atoms.Atoms.Keys)
                    base.Children.Add(new SavedAtomsViewModel(null, atom, atoms.Atoms[atom], this));
            }
        }
    }
}
