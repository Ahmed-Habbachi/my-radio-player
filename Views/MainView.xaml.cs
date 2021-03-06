﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MyBrain.Applications.MyRadioPlayer.ObjectModel;
using System.IO;
using Microsoft.Win32;
using System.Drawing.Imaging;

namespace MyBrain.Applications.MyRadioPlayer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private Profile profile;
        //private readonly List<Image> radioImageList;
        private readonly Dictionary<string, Image> radioBitmapImageDictionary;
        private RadioStation _currentRadio;
        private bool isNewRadioStation = false;
        private bool isTreeViewItemMoving = false;

        public MainView()
        {
            this.radioBitmapImageDictionary = new Dictionary<string, Image>();
            InitializeComponent();
            InitializeProfile();
            InitializeCultureCombox(this.profile.Language);
            InitializeResources();
            SetLastSessionState(profile);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadTreeNodeImages();
                InitializeTreeViewRadioStations(profile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Initializing Radio Station", MessageBoxButton.OK);
            }
        }

        private void TrvRadioStations_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var trv = sender as TreeView;

            if (trv.SelectedItem is TreeViewItem)
            {
                TreeViewItem selectedItem = trv.SelectedItem as TreeViewItem;

                if (selectedItem.Tag is RadioStation radio)
                {
                    this._currentRadio = radio;
                    this.btnEditRadio.IsEnabled = true;
                    ChangeRadioStation(this._currentRadio);
                }

                if (!isTreeViewItemMoving)
                {
                    int index = this.trvRadioStations.Items.IndexOf(selectedItem);

                    if (index == this.trvRadioStations.Items.Count - 1)
                    {
                        this.btnDown.IsEnabled = false;
                    }
                    else if (index == 0)
                    {
                        this.btnUp.IsEnabled = false;
                    }
                    else
                    {
                        this.btnDown.IsEnabled = true;
                        this.btnUp.IsEnabled = true;
                    }
                }
            }
        }

        private void MediaPlayerMain_BufferingStarted(object sender, RoutedEventArgs e)
        {
            this.txtBlkSatus.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.msgBufferingStarted;
        }

        private void MediaPlayerMain_BufferingEnded(object sender, RoutedEventArgs e)
        {
            this.txtBlkSatus.Text = String.Format("{0} {1}", this._currentRadio.Name, MyRadioPlayer.Resources.MyRadioPlayerResource.msgIsPlaying);
            //this volume setting is to adjust volume on some OS versions
            sliderVolume.Value += 0.1;
            sliderVolume.Value -= 0.1;
        }

        private void MediaPlayerMain_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            this.txtBlkSatus.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.msgMediaFailed;
        }

        private void MediaPlayerMain_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.txtBlkSatus.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.msgMediaEnded;
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            this.mediaPlayerMain.Source = this._currentRadio.StreamingURI;
            this.mediaPlayerMain.Play();
            this.txtBlkSatus.Text = String.Format("{0} {1}", this._currentRadio.Name, MyRadioPlayer.Resources.MyRadioPlayerResource.msgIsPlaying);
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            this.mediaPlayerMain.Pause();
            this.txtBlkSatus.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.msgMediaPaused;
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            this.mediaPlayerMain.Stop();
            this.txtBlkSatus.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.msgMediaStoped;
        }

        private void BtnEditRadio_Click(object sender, RoutedEventArgs e)
        {
            ChangeControlsState(true);

            this.txtRadioName.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.txtRadioName.Text) && !string.IsNullOrEmpty(this.txtRadioStreamingURL.Text))
            {
                _currentRadio.Name = this.txtRadioName.Text;
                FileInfo fileInfo = this.txtRadioLogo.Tag as FileInfo;

                if (fileInfo != null  && !File.Exists(System.IO.Path.Combine(Helper.Configuration.AppDataFolderFullPath, this.txtRadioLogo.Text)))
                {
                    File.Copy(fileInfo.FullName, System.IO.Path.Combine(Helper.Configuration.AppDataFolderFullPath, this.txtRadioLogo.Text));
                }

                _currentRadio.Logo = this.txtRadioLogo.Text;
                if (Uri.TryCreate(this.txtRadioStreamingURL.Text, UriKind.RelativeOrAbsolute, out _))
                {
                    _currentRadio.StreamingSource = this.txtRadioStreamingURL.Text; 
                }
                _currentRadio.WebSite = this.txtRadioWebSite.Text;
                ChangeControlsState(false);

                if (!isNewRadioStation)
                {
                    TreeViewItem trvSelectedItem = this.trvRadioStations.SelectedItem as TreeViewItem;
                    UpdateTreeViewItem(trvSelectedItem);
                    SyncControlsWithCurrentRadioStation(_currentRadio);
                }
                else
                {
                    profile.RadioStations.Add(_currentRadio);
                    TreeViewItem radioItem = new TreeViewItem();
                    UpdateTreeViewItem(radioItem);
                    radioItem.Tag = _currentRadio;
                    this.trvRadioStations.Items.Add(radioItem);
                    ChangeRadioStation(_currentRadio);
                }
            }

            this.isNewRadioStation = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ChangeControlsState(false);
        }

        private void Txt_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void BtnEditRadioLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                InitialDirectory = Helper.Configuration.AppDataFolderFullPath,
                Filter = "Images (*.*)|*.*|(.jpg)|*.jpg|(gif)|*.gif|(png)|*.png|(bmp)|*.bmp|(jpe)|*.jpe|(jpeg)|*.jpeg"
            };

            if (ofd.ShowDialog() == true)
            {
                FileInfo fInfo = new FileInfo(ofd.FileName);

                this.txtRadioLogo.Tag = fInfo;
                this.txtRadioLogo.Text = fInfo.Name;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            CreateNewStation();
            ChangeControlsState(true);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem itemToDelete = this.trvRadioStations.SelectedItem as TreeViewItem;
            RadioStation radioToDelete = itemToDelete.Tag as RadioStation;

            if (itemToDelete != null && radioToDelete != null)
            {
                string message = "Do you really want to delete " + radioToDelete.Name + " from your collection?";
                const string caption = "Radio Station removal";
                if (MessageBox.Show(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        profile.RadioStations.Remove(radioToDelete);
                        int index = this.trvRadioStations.Items.IndexOf(itemToDelete);
                        this.trvRadioStations.Items.RemoveAt(index);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCurrentProfile();
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            this.isTreeViewItemMoving = true;
            TreeViewItem itemToMove = this.trvRadioStations.SelectedItem as TreeViewItem;
            RadioStation radioToMove = itemToMove.Tag as RadioStation;
            int index = this.trvRadioStations.Items.IndexOf(itemToMove);
            this.trvRadioStations.Items.RemoveAt(index);
            this.trvRadioStations.Items.Insert(index - 1, itemToMove);
            this.isTreeViewItemMoving = false;
            itemToMove.IsSelected = true;
            index = profile.RadioStations.IndexOf(radioToMove);
            profile.RadioStations.RemoveAt(index);
            profile.RadioStations.Insert(index -1, radioToMove);
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            this.isTreeViewItemMoving = true;
            TreeViewItem itemToMove = this.trvRadioStations.SelectedItem as TreeViewItem;
            RadioStation radioToMove = itemToMove.Tag as RadioStation;
            int index = this.trvRadioStations.Items.IndexOf(itemToMove);
            this.trvRadioStations.Items.RemoveAt(index);
            this.trvRadioStations.Items.Insert(index + 1, itemToMove);
            this.isTreeViewItemMoving = false;
            itemToMove.IsSelected = true;
            index = profile.RadioStations.IndexOf(radioToMove);
            profile.RadioStations.RemoveAt(index);
            profile.RadioStations.Insert(index + 1, radioToMove);
        }

        private void CmbCulture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MyRadioPlayer.Resources.MyRadioPlayerResource.Culture = cmbCulture.SelectedItem as System.Globalization.CultureInfo;
            InitializeResources();
        }

        private void MediaVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            this.mediaPlayerMain.Volume = sliderVolume.Value;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentProfile();
            const string message = "Current profile succesfully saved.";
            const string caption = "Saving default collection";
            MessageBox.Show(message, caption, MessageBoxButton.OK);
        }

        private void InitializeProfile()
        {
            if (!Directory.Exists(Helper.Configuration.AppDataFolderFullPath))
            {
                Directory.CreateDirectory(Helper.Configuration.AppDataFolderFullPath);

                if (Directory.Exists(Helper.Configuration.DefaultFolderFullPath))
                {
                    foreach (var file in Directory.GetFiles(Helper.Configuration.DefaultFolderFullPath))
                    {
                        File.Copy(file, Path.Combine(Helper.Configuration.AppDataFolderFullPath, Path.GetFileName(file)));
                    }
                }
                else
                {
                    const string message = " We are unable to load your radio collection \n And We are unable to load the default collection \n You will have to create your radio station list from scratch.";
                    MessageBox.Show(message, "Error Initializing default data", MessageBoxButton.OK);
                    this.profile = new Profile();
                }
            }

            DataLibrary dataLibrary = new DataLibrary(Helper.Configuration.AppDataJSONFullPath);
            this.profile = dataLibrary.LoadProfile();
        }

        private void InitializeCultureCombox(string languageToTrySelect)
        {
            foreach (string cultureName in new string[] { "en", "fr", "ar", "de" })
            {
                System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo(cultureName);
                this.cmbCulture.Items.Add(cultureInfo);

                if (cultureInfo.NativeName == languageToTrySelect)
                {
                    this.cmbCulture.SelectedIndex = this.cmbCulture.Items.IndexOf(cultureInfo);
                }
            }

            this.cmbCulture.DisplayMemberPath = "NativeName";

            if (this.cmbCulture.SelectedIndex < 0)
            {
                this.cmbCulture.SelectedIndex = 0;
            }
        }

        private void InitializeResources()
        {
            this.lblTreeViewRoot.Content = MyRadioPlayer.Resources.MyRadioPlayerResource.lblTreeViewRoot;
            this.lblRadioName.Content = MyRadioPlayer.Resources.MyRadioPlayerResource.lblRadioName;
            this.lblRadioLogo.Content = MyRadioPlayer.Resources.MyRadioPlayerResource.lblRadioLogo;
            this.lblRadioWebSite.Content = MyRadioPlayer.Resources.MyRadioPlayerResource.lblRadioWebSite;
            this.lblRadioUrlStreaming.Content = MyRadioPlayer.Resources.MyRadioPlayerResource.lblRadioUrlStreaming;
            this.txtBlkVolume.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.txtBlkVolume;
            this.txtBlkStat.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.txtBlkStat;
            this.txtBlkSatus.Text = MyRadioPlayer.Resources.MyRadioPlayerResource.txtBlkSatus;
        }

        private void SetLastSessionState(Profile profil)
        {
            this.Top = profil.WindowTop;
            this.Left = profil.WindowLeft;
            sliderVolume.Value = profil.Volume;
        }

        private void LoadTreeNodeImages()
        {
            try
            {
                if (!string.IsNullOrEmpty(Helper.Configuration.AppDataFolderFullPath))
                {
                    DirectoryInfo di = new DirectoryInfo(Helper.Configuration.AppDataFolderFullPath);

                    IEnumerable<FileInfo> fileList = di.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                    IEnumerable<FileInfo> fileQuery = from file in fileList
                                                      where (Helper.Configuration.PHOTOFILTER.Contains(file.Extension))
                                                      orderby file.CreationTime descending
                                                      select file;
                    foreach (FileInfo fi in fileQuery)
                    {
                        AddRadioLogoToCollections(fi.FullName, fi.Name);
                    }

                    AddRadioLogoToCollections(@"pack://application:,,,/Resources/Images/radio-26.png", "radio");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load radio logo Images", ex);
            }
        }

        private void AddRadioLogoToCollections(string imagePath, string imageName)
        {
            if (!File.Exists(imagePath))
            {
                return;
            }

            if (!this.radioBitmapImageDictionary.ContainsKey(imageName))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(imagePath);
                bitmapImage.DecodePixelWidth = 24;
                bitmapImage.DecodePixelHeight = 24;
                bitmapImage.EndInit();
                Image image = new Image();
                image.Source = bitmapImage;
                image.Tag = bitmapImage;
                this.radioBitmapImageDictionary.Add(imageName, image);
            }
        }

        private void InitializeTreeViewRadioStations(Profile profile)
        {
            if (this.trvRadioStations.Items.Count != 0)
            {
                this.trvRadioStations.Items.Clear();
            }

            foreach (RadioStation radio in profile.RadioStations)
            {
                AddRadioNodeToTreeView(radio);
            }

            if (this.trvRadioStations.Items.Count > 0)
            {
                if (!string.IsNullOrEmpty(profile.LastRadioId))
                {
                    foreach (TreeViewItem item in this.trvRadioStations.Items)
                    {
                        if (item.Tag is RadioStation itemRadio && itemRadio.Id == profile.LastRadioId)
                        {
                            _currentRadio = itemRadio;
                            item.IsSelected = true;
                            ChangeRadioStation(_currentRadio);
                        }
                    }
                }
                else
                {
                    if (this.trvRadioStations.Items[0] is TreeViewItem firstRadio)
                    {
                        firstRadio.IsSelected = true;
                    }
                }
            }
        }

        private void AddRadioNodeToTreeView(RadioStation radio)
        {
            TreeViewItem radioItem = new TreeViewItem { IsExpanded = true };
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal, Width = 150};
            Label lbl = new Label { Content = radio.Name };
            Image image;

            image = GetImage(radio.Logo);

            stack.Children.Add(image);
            stack.Children.Add(lbl);

            radioItem.Header = stack;
            radioItem.Tag = radio;
            this.trvRadioStations.Items.Add(radioItem);

            radioItem.IsSelected = true;
        }

        private Image GetImage(string radioLogo)
        {
            if (string.IsNullOrEmpty(radioLogo) || !this.radioBitmapImageDictionary.TryGetValue(radioLogo, out Image image))
            {
                this.radioBitmapImageDictionary.TryGetValue("radio", out image);
            }

            return new Image() { Source = image.Source};
        }

        private void UpdateTreeViewItem(TreeViewItem trvItem)
        {
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal, Width = 150};
            Label lbl = new Label { Content = _currentRadio.Name };
            var logoFullPath = Path.Combine(Helper.Configuration.AppDataFolderFullPath, _currentRadio.Logo);
            FileInfo fileInfo;
            Image radioImage;

            if (File.Exists(logoFullPath))
            {
                fileInfo = new FileInfo(logoFullPath);
                AddRadioLogoToCollections(fileInfo.FullName, fileInfo.Name);
                radioImage = GetImage(_currentRadio.Logo);

                if (radioImage != null)
                {
                    stack.Children.Add(radioImage);
                }
            }

            stack.Children.Add(lbl);
            trvItem.Header = stack;
        }

        private void ChangeRadioStation(RadioStation radioStation)
        {
            if (this.mediaPlayerMain.Source != radioStation.StreamingURI)
            {
                try
                {
                    this.mediaPlayerMain.Source = radioStation.StreamingURI;
                    this.txtBlkSatus.Text = String.Format("{0} is Loading...", radioStation.Name);
                    this.mediaPlayerMain.Play();
                    SyncControlsWithCurrentRadioStation(radioStation);
                }
                catch (Exception)
                {
                    const string message = "Unable to read media please to correct the stream source or delete the station.";
                    const string caption = "Error read stream";
                    MessageBox.Show(message, caption, System.Windows.MessageBoxButton.OK);
                }
            }
        }

        private void SyncControlsWithCurrentRadioStation(RadioStation radioStation)
        {
            var currentRadioLogo = GetImage(radioStation.Logo);
            this.imageBoxRadio.Source = GetBitmapFromImage(currentRadioLogo);
            this.txtRadioName.Text = radioStation.Name;
            this.txtRadioLogo.Text = radioStation.Logo;
            this.txtRadioWebSite.Text = radioStation.WebSite;
            this.txtRadioStreamingURL.Text = radioStation.StreamingSource;
        }

        private void ChangeControlsState(bool isEnabled)
        {
            this.btnEditRadioLogo.IsEnabled = isEnabled;
            this.txtRadioLogo.IsEnabled = isEnabled;
            this.txtRadioName.IsEnabled = isEnabled;
            this.txtRadioStreamingURL.IsEnabled = isEnabled;
            this.txtRadioWebSite.IsEnabled = isEnabled;
            this.btnAdd.IsEnabled = isEnabled;
            this.btnOk.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
            this.btnCancel.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
        }

        private void CreateNewStation()
        {
            this.txtRadioName.Text = "";
            this.txtRadioStreamingURL.Text = "";
            this.txtRadioWebSite.Text = "";
            this.txtRadioLogo.Text = "";
            this.isNewRadioStation = true;
            this._currentRadio = new RadioStation();
            SyncControlsWithCurrentRadioStation(this._currentRadio);
        }

        private void SaveCurrentProfile()
        {
            profile.WindowTop = this.Top;
            profile.WindowLeft = this.Left;
            profile.Volume = this.mediaPlayerMain.Volume;
            profile.LastRadioId = _currentRadio.Id;
            profile.Language = ((System.Globalization.CultureInfo)this.cmbCulture.SelectedItem).NativeName;
            DataLibrary dataLibrary = new DataLibrary(Helper.Configuration.AppDataJSONFullPath);
            dataLibrary.SaveProfile(profile);
        }

        public BitmapImage GetBitmapFromImage(Image imgage)
        {
            if (imgage.Tag is BitmapImage bitmap)
            {
                return bitmap;
            }

            return null;
        }
    }
}
