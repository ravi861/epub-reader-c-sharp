using MasterDetail.Common;
using MasterDetailApp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MasterDetailApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BookPage : Page
    {
        public static Windows.Storage.ApplicationDataCompositeValue compositelocal;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        public BookPage()
        {
            this.InitializeComponent();

            var sampleDataGroups = BookSource.GetGroupsAsync();
            itemGridView.ItemsSource = sampleDataGroups;
            App.fullWidth = Window.Current.Bounds.Width;
            App.halfWidth = Window.Current.Bounds.Width / 2;
            App.readWidth = App.fullWidth;
            //this.DefaultViewModel["Books"] = sampleDataGroups;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            bool update = false;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = null;
            if ((int)localSettings.Values["bookCount"] == 1)
            {
                composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Containers["booksContainer"].Values["book0"];
                composite["chapter"] = 0;
                composite["offset"] = 0;
                localSettings.Values["nowReading"] = composite;
                update = true;
            }
            else if ((int)localSettings.Values["bookCount"] > 0)
            {
                composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["nowReading"];
                update = true;

            }
            if (update == true)
            {
                Book readingbook = new Book(composite["uniqueid"].ToString(), composite["title"].ToString(), composite["author"].ToString(),
                                                            composite["image"].ToString(), composite["location"].ToString());
                int chap = Convert.ToInt32(composite["chapter"]) + 1;
                this.chapProgress.Text = chap.ToString();
                this.chapProgressInline.Text = chap.ToString();
                this.currentBook.DataContext = readingbook;
                this.currentBookInline.DataContext = readingbook;
                this.currentBook.Visibility = Windows.UI.Xaml.Visibility.Visible;
                App.splitBook = readingbook;
                String currentBookFolder = readingbook.Location;
                Windows.Data.Xml.Dom.XmlDocument doc = await Routines.GetFile(readingbook.Location + "\\META-INF", "container.xml");
                var opfPathinit = doc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
                String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
                String opfPath = opfPath_tmp[0];
                String opfFile = opfPath_tmp[1];
                currentBookFolder = currentBookFolder + "/" + opfPath;

                doc = await Routines.GetFile(currentBookFolder, opfFile);
                this.chapCount.Text = doc.GetElementsByTagName("itemref").Count.ToString();
                this.chapCountInline.Text = doc.GetElementsByTagName("itemref").Count.ToString();
            }
            if (Convert.ToInt32(composite["count"]) > 0)
            {
                int chapcount = Convert.ToInt32(composite["count"]);
                //this.bookProgressBar.Value = (chap * 100) / chapcount;
            }

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;
            if (rootFrame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Collapsed;
            }

        }        

        private async Task<int> populateimagegrid(StorageFile zipfilename, StorageFolder storfolder)
        {
            String bookTitle = "";
            String bookAuthor = "";
            String currentBookFolder = zipfilename.DisplayName;
            Windows.Data.Xml.Dom.XmlDocument localDoc = null;
            try
            {
                localDoc = await Routines.GetFile(currentBookFolder + "/META-INF", "container.xml");

                var opfPathinit = localDoc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
                String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
                String opfPath = opfPath_tmp[0];
                String opfFile = opfPath_tmp[1];
                currentBookFolder = currentBookFolder + "/" + opfPath;
                localDoc = await Routines.GetFile(currentBookFolder, opfFile);

                bookTitle = localDoc.GetElementsByTagName("dc:title").Item(0).ChildNodes.Item(0).NodeValue.ToString();
                bookAuthor = localDoc.GetElementsByTagName("dc:creator").Item(0).ChildNodes.Item(0).NodeValue.ToString();
                BookPhotos.Photos.Clear();
                var itemCount = localDoc.GetElementsByTagName("item").Count;
                for (uint index = 0; index < itemCount; index++)
                {
                    var mediaType = localDoc.GetElementsByTagName("item").Item(index).Attributes.GetNamedItem("media-type").NodeValue;
                    if (mediaType.ToString().Equals("image/jpeg") || mediaType.ToString().Equals("image/png"))
                    {
                        String imPath = localDoc.GetElementsByTagName("item").Item(index).Attributes.GetNamedItem("href").NodeValue.ToString();
                        String[] val = Routines.parseString(imPath);
                        BookPhotos.Photos.Add(new PhotoItem { Loc = currentBookFolder + val[0], Name = val[1], PictureUri = new Uri("ms-appdata:///local/" + currentBookFolder + imPath) });
                    }
                }
                BookPhotos.Photos.Add(new PhotoItem { Loc = "thumbs", Name = "default.jpg", PictureUri = new Uri("ms-appdata:///local/thumbs/default.jpg") });
                this.imageGridViewText.Text = "Select a thumbnail";
            }
            catch
            {
                //this.pbar.IsIndeterminate = false;
                //this.Add.IsEnabled = true;
                this.imageGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                await storfolder.DeleteAsync();
                //Routines.DisplayMsg("Error creating thumbnail list");
                return -1;
            }
            finally
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                Windows.Storage.ApplicationDataCompositeValue composite = new Windows.Storage.ApplicationDataCompositeValue();
                composite["uniqueid"] = "book" + localSettings.Values["bookCount"].ToString();
                composite["title"] = bookTitle;
                composite["author"] = bookAuthor;
                composite["image"] = "thumbs\\" + "book" + localSettings.Values["bookCount"].ToString() + ".jpg"; ;
                composite["location"] = storfolder.DisplayName;
                compositelocal = composite;
            }
            return 0;
        }

        private async Task<StorageFolder> extractFiles(StorageFile zipfilename)
        {
            StorageFolder storfolder = null;
            try
            {
                // Create stream for compressed files in memory
                using (MemoryStream zipMemoryStream = new MemoryStream())
                {
                    using (Windows.Storage.Streams.IRandomAccessStream zipStream = await zipfilename.OpenAsync(FileAccessMode.Read))
                    {
                        // Read compressed data from file to memory stream
                        using (Stream instream = zipStream.AsStreamForRead())
                        {
                            byte[] buffer = new byte[1024];
                            while (instream.Read(buffer, 0, buffer.Length) > 0)
                            {
                                zipMemoryStream.Write(buffer, 0, buffer.Length);
                            }
                        }
                    }
                    storfolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(zipfilename.DisplayName, CreationCollisionOption.GenerateUniqueName);
                    // Create zip archive to access compressed files in memory stream
                    using (ZipArchive zipArchive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read))
                    {
                        // For each compressed file...
                        foreach (ZipArchiveEntry entry in zipArchive.Entries)
                        {
                            // ... read its uncompressed contents
                            using (Stream entryStream = entry.Open())
                            {
                                if (entry.Name != "")
                                {
                                    string fileName = entry.FullName.Replace("/", @"\");
                                    byte[] buffer = new byte[entry.Length];
                                    entryStream.Read(buffer, 0, buffer.Length);

                                    // Create a file to store the contents
                                    StorageFile uncompressedFile = await storfolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                                    // Store the contents
                                    using (Windows.Storage.Streams.IRandomAccessStream uncompressedFileStream = await uncompressedFile.OpenAsync(FileAccessMode.ReadWrite))
                                    {
                                        using (Stream outstream = uncompressedFileStream.AsStreamForWrite())
                                        {
                                            outstream.Write(buffer, 0, buffer.Length);
                                            outstream.Flush();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                //this.pbar.IsIndeterminate = false;
                //this.Add.IsEnabled = true;
                //this.imageGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                if (storfolder != null) await storfolder.DeleteAsync();
                return null;
            }
            finally
            {

                zipfilename.DeleteAsync();
            }
            return storfolder;
        }

        public async void extracter(StorageFile zipfilename)
        {
            this.imageGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            this.imageGridViewText.Text = "Loading...";
            //this.Add.IsEnabled = false;
            StorageFolder storfolder = await extractFiles(zipfilename);
            if (storfolder == null) return;
            int ret = await populateimagegrid(zipfilename, storfolder);
            if (ret == -1) return;
        }
        async void addBook_Click(object sender, RoutedEventArgs e)
        {
            //this.pbar.IsIndeterminate = true;
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add(".epub");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (null != file)
            {
                try
                {
                    StorageFile savedFile = await file.CopyAsync(Windows.Storage.ApplicationData.Current.LocalFolder, file.Name, NameCollisionOption.ReplaceExisting);
                    extracter(savedFile);
                }
                catch
                {
                }
            }
            else { //this.pbar.IsIndeterminate = false; }
            }
            this.SamplesSplitView.IsPaneOpen = !this.SamplesSplitView.IsPaneOpen;
        }
        public void openBook_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SplitPage), null);
        }

        public void help_Click(object sender, RoutedEventArgs e)
        {
            Routines.DisplayMsg("Help", "Epub Reader 8 \nVersion: 3.0.0\nhttp://www.psi-apps.com/privacy-policy");
        }

        public async void add_Book(object sender, ItemClickEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String uniqueId = "book" + localSettings.Values["bookCount"].ToString();
            PhotoItem selImage = (PhotoItem)e.ClickedItem;
            String imageName = selImage.Name;
            StorageFolder imageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(selImage.Loc.Replace("/", "\\"));
            StorageFile storageFile = await imageFolder.GetFileAsync(imageName);
            imageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync("thumbs");
            await storageFile.CopyAsync(imageFolder, uniqueId + ".jpg");
            var books = new Book(compositelocal["uniqueid"].ToString(), compositelocal["title"].ToString(), compositelocal["author"].ToString(),
                                                                        compositelocal["image"].ToString(), compositelocal["location"].ToString());
            BookSource.AddBookAsync(books);
            if (localSettings.Containers.ContainsKey("booksContainer"))
            {
                localSettings.Containers["booksContainer"].Values[uniqueId] = compositelocal;
                int count = (int)localSettings.Values["bookCount"];
                localSettings.Values["bookCount"] = ++count;
            }
            //this.Add.IsEnabled = true;
            this.imageGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.imageGridViewText.Text = "";
            BookPhotos.Photos.Clear();
            //this.pbar.IsIndeterminate = false;
            storageFile = null;
        }

        private async void clear_Click(object sender, RoutedEventArgs e)
        {
            var messageDialog = new Windows.UI.Popups.MessageDialog("Warning");
            messageDialog.Title = "Warning !!!";
            messageDialog.Content = "This will clear all settings and library contents and cannot be restored.\nAre you sure you want to continue?";
            messageDialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(this.CommandInvokedHandler)));
            messageDialog.Commands.Add(new UICommand("No", new UICommandInvokedHandler(this.CommandInvokedHandler)));
            messageDialog.DefaultCommandIndex = 0;
            messageDialog.CancelCommandIndex = 1;
            await messageDialog.ShowAsync();
            Routines.DisplayMsg("Restart", "Please restart the app to add new books.");
        }

        private async void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label == "Yes")
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                BookSource.ClearBooks();
                this.currentBook.DataContext = null;
                this.currentBook.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                localSettings.Values.Clear();
                localSettings.DeleteContainer("booksContainer");
                try
                {
                    await Routines.resetFolders();
                }
                catch { }
            }
        }

        private async void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = null;
            composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["nowReading"];

            GridView gv = (GridView)sender;
            Book bk = (Book)gv.SelectedItem;
            this.currentBook.DataContext = bk;
            this.currentBookInline.DataContext = bk;
            App.splitBook = bk;
            String currentBookFolder = bk.Location;
            Windows.Data.Xml.Dom.XmlDocument doc = await Routines.GetFile(bk.Location + "\\META-INF", "container.xml");
            var opfPathinit = doc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
            String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
            String opfPath = opfPath_tmp[0];
            String opfFile = opfPath_tmp[1];
            currentBookFolder = currentBookFolder + "/" + opfPath;

            doc = await Routines.GetFile(currentBookFolder, opfFile);
            this.chapCount.Text = doc.GetElementsByTagName("itemref").Count.ToString();
            this.chapCountInline.Text = doc.GetElementsByTagName("itemref").Count.ToString();

            if (bk.UniqueId == composite["uniqueid"].ToString())
            {
                this.chapProgressInline.Text = (Convert.ToInt32(composite["chapter"]) + 1).ToString();
            }
            else
            {
                this.chapProgressInline.Text = (1).ToString();
            }
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            this.SamplesSplitView.IsPaneOpen = !this.SamplesSplitView.IsPaneOpen;
        }

    }
}
