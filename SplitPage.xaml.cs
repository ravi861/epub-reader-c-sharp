using MasterDetailApp.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MasterDetailApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplitPage : Page
    {
        private NavigationHelper navigationHelper;
        public StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        String globalCurrentBookFolder = "";
        public Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public Double offset = 0;
        public bool panoOn = false;
        public static int returnIndex = 0;

        public SplitPage()
        {
            this.InitializeComponent();
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            // Setup the navigation helper
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            AdMediator_Bottom.AdSdkError += AdMediator_Bottom_AdError;
            AdMediator_Bottom.AdMediatorFilled += AdMediator_Bottom_AdFilled;
            AdMediator_Bottom.AdMediatorError += AdMediator_Bottom_AdMediatorError;
            AdMediator_Bottom.AdSdkEvent += AdMediator_Bottom_AdSdkEvent;

            //this.navigationHelper.GoBackCommand = new RelayCommand(() => this.GoBack(), () => this.CanGoBack());
        }
        // and then add these functions
        void AdMediator_Bottom_AdSdkEvent(object sender, Microsoft.AdMediator.Core.Events.AdSdkEventArgs e)
        {
            Debug.WriteLine("AdSdk event {0} by {1}", e.EventName, e.Name);
        }

        void AdMediator_Bottom_AdMediatorError(object sender, Microsoft.AdMediator.Core.Events.AdMediatorFailedEventArgs e)
        {
            Debug.WriteLine("AdMediatorError:" + e.Error + " " + e.ErrorCode);
            // if (e.ErrorCode == AdMediatorErrorCode.NoAdAvailable)
            // AdMediator will not show an ad for this mediation cycle
        }

        void AdMediator_Bottom_AdFilled(object sender, Microsoft.AdMediator.Core.Events.AdSdkEventArgs e)
        {
            Debug.WriteLine("AdFilled:" + e.Name);
        }

        void AdMediator_Bottom_AdError(object sender, Microsoft.AdMediator.Core.Events.AdFailedEventArgs e)
        {
            Debug.WriteLine("AdSdkError by {0} ErrorCode: {1} ErrorDescription: {2} Error: {3}", e.Name, e.ErrorCode, e.ErrorDescription, e.Error);

        }
        private void App_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;

            // Navigate back if possible, and if the event has not 
            // already been handled .
            // Navigate back if possible, and if the event has not 
            // already been handled .
            if (rootFrame.CanGoBack && e.Handled == false)
            {
                Windows.Storage.ApplicationDataCompositeValue composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["nowReading"];
                composite["chapter"] = returnIndex;// this.MasterListView.SelectedIndex;
                composite["count"] = this.MasterListView.Items.Count;
                composite["offset"] = panoOn ? this.panoView.HorizontalOffset : this.columnView.HorizontalOffset;
                localSettings.Values["nowReading"] = composite;
                //BookChapters.Chapters.Clear();
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Book currentBook = App.splitBook;
            globalCurrentBookFolder = currentBook.Location;
            await this.getBookContent(currentBook);
            Windows.Storage.ApplicationDataCompositeValue composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["nowReading"];
            if (composite["uniqueid"].ToString() == currentBook.UniqueId)
            {
            }
            else
            {
                composite["uniqueid"] = currentBook.UniqueId;
                composite["location"] = currentBook.Location;
                composite["title"] = currentBook.Title;
                composite["author"] = currentBook.Author;
                composite["image"] = "thumbs/" + currentBook.UniqueId + ".jpg";
                composite["chapter"] = 0;
                composite["offset"] = 0;
                localSettings.Values["nowReading"] = composite;
            }
            if (e.PageState == null)
            {
                //this.MasterListView.SelectedItem = null;
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.MasterListView.Items.Count > 0)
                {
                    this.MasterListView.IsEnabled = true;
                    this.MasterListView.SelectedIndex = Convert.ToInt32(composite["chapter"].ToString());
                    this.currChapter.Text = composite["chapter"].ToString();
                    offset = Convert.ToDouble(composite["offset"].ToString());
                    if (localSettings.Values.ContainsKey("readmode"))
                    {
                        if (localSettings.Values["readmode"].ToString() == "pano")
                        {
                            panoOn = true;
                            this.panoView.ChangeView(offset, null, null);
                        }
                        else
                        {
                            panoOn = false;
                            this.columnView.ChangeView(offset, null, null);
                        }
                    }
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (e.PageState.ContainsKey("SelectedItem") && this.MasterListView.Items.Count > 0)
                {
                    //this.MasterListView.SelectedIndex = Convert.ToInt32(e.PageState["SelectedIndex"]);
                }
            }
            this.pbar.IsActive = false;
            //this.updatenotifier.Text = "";
        }
        
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            e.PageState["SelectedIndex"] = this.MasterListView.SelectedIndex;
        }

        private bool UsingLogicalPageNavigation()
        {
            return false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;

            // Navigate back if possible, and if the event has not 
            // already been handled .

            if (rootFrame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    Windows.UI.Core.AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }
        

        public async Task<Dictionary<string, string>> parseNcx(Book book)
        {
            Dictionary<String, String> ncxDict = new Dictionary<string, string>();
            try
            {
                String currentBookFolder = book.Location;
                Windows.Data.Xml.Dom.XmlDocument doc = await Routines.GetFile(book.Location + "\\META-INF", "container.xml");
                var opfPathinit = doc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
                String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
                String opfPath = opfPath_tmp[0];
                String opfFile = opfPath_tmp[1];
                String opfFileFolder = currentBookFolder + "/" + opfPath;

                doc = await Routines.GetFile(opfFileFolder, opfFile);
                var itemcount = doc.GetElementsByTagName("item").Count;
                for (uint index1 = 0; index1 < itemcount; index1++)
                {
                    var id = doc.GetElementsByTagName("item").Item(index1).Attributes.GetNamedItem("id").NodeValue;
                    var mediaType = doc.GetElementsByTagName("item").Item(index1).Attributes.GetNamedItem("media-type").NodeValue;
                    if (mediaType.ToString().Equals("application/x-dtbncx+xml") && id.ToString().Equals("ncx"))
                    {
                        var ncxHref = doc.GetElementsByTagName("item").Item(index1).Attributes.GetNamedItem("href").NodeValue;
                        String[] ncxPath_tmp = Routines.parseString(ncxHref.ToString());
                        String ncxFolder = opfFileFolder + "/" + ncxPath_tmp[0];
                        Windows.Data.Xml.Dom.XmlDocument ncx = await Routines.GetFile(ncxFolder, ncxPath_tmp[1]);

                        var navPointCount = ncx.GetElementsByTagName("navPoint").Count;
                        XmlNodeList navlist = ncx.GetElementsByTagName("navPoint");
                        int count = navlist.Count;
                        foreach (XmlElement innerNode in navlist)
                        {
                            String chapName = innerNode.GetElementsByTagName("text").Item(0).InnerText;
                            String chapPath = innerNode.GetElementsByTagName("content").Item(0).Attributes.GetNamedItem("src").NodeValue.ToString();
                            String[] chapPath_tmp = Routines.parseString(chapPath.ToString());
                            String chapFolder = currentBookFolder + "/" + chapPath_tmp[0];
                            ncxDict.Add(chapPath_tmp[1].Replace(" ", ""), chapName);
                        }
                        break;
                    }
                }
            }
            catch { }

            return ncxDict;
        }

        public async Task<Dictionary<string, string>> getReferences(Book book)
        {
            Dictionary<String, String> ncxDict = new Dictionary<string, string>();
            try
            {
                String currentBookFolder = book.Location;
                Windows.Data.Xml.Dom.XmlDocument doc = await Routines.GetFile(book.Location + "\\META-INF", "container.xml");
                var opfPathinit = doc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
                String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
                String opfPath = opfPath_tmp[0];
                String opfFile = opfPath_tmp[1];
                String opfFileFolder = currentBookFolder + "/" + opfPath;

                doc = await Routines.GetFile(opfFileFolder, opfFile);
                var itemcount = doc.GetElementsByTagName("reference").Count;
                for (uint index1 = 0; index1 < itemcount; index1++)
                {
                    var title = doc.GetElementsByTagName("reference").Item(index1).Attributes.GetNamedItem("title").NodeValue.ToString();
                    var href = doc.GetElementsByTagName("reference").Item(index1).Attributes.GetNamedItem("href").NodeValue.ToString();
                    ncxDict.Add(title.ToLower(), href);
                }
            }
            catch { }

            return ncxDict;
        }

        public async Task<Dictionary<string, string>> getItems(Book book)
        {
            Dictionary<String, String> itemDict = new Dictionary<string, string>();
            try
            {
                String currentBookFolder = book.Location;
                Windows.Data.Xml.Dom.XmlDocument doc = await Routines.GetFile(book.Location + "\\META-INF", "container.xml");
                var opfPathinit = doc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
                String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
                String opfPath = opfPath_tmp[0];
                String opfFile = opfPath_tmp[1];
                String opfFileFolder = currentBookFolder + "/" + opfPath;

                doc = await Routines.GetFile(opfFileFolder, opfFile);
                var itemcount = doc.GetElementsByTagName("item").Count;
                for (uint index1 = 0; index1 < itemcount; index1++)
                {
                    if (doc.GetElementsByTagName("item").Item(index1).Attributes.GetNamedItem("media-type").NodeValue.ToString().Equals("application/xhtml+xml"))
                    {
                        itemDict.Add(doc.GetElementsByTagName("item").Item(index1).Attributes.GetNamedItem("id").NodeValue.ToString().ToLower(), doc.GetElementsByTagName("item").Item(index1).Attributes.GetNamedItem("href").NodeValue.ToString());

                    }
                }
            }
            catch { }

            return itemDict;
        }

        public async Task getBookContent(Book book)
        {
            try
            {
                BookChapters.Chapters.Clear();
                Dictionary<String, String> ncxDict = await parseNcx(book);
                Dictionary<String, String> refDict = await getReferences(book);
                Dictionary<String, String> itemDict = await getItems(book);
                String currentBookFolder = book.Location;
                Windows.Data.Xml.Dom.XmlDocument doc = await Routines.GetFile(book.Location + "\\META-INF", "container.xml");
                var opfPathinit = doc.GetElementsByTagName("rootfile").Item(0).Attributes.GetNamedItem("full-path").NodeValue;
                String[] opfPath_tmp = Routines.parseString(opfPathinit.ToString());
                String opfPath = opfPath_tmp[0];
                String opfFile = opfPath_tmp[1];
                currentBookFolder = currentBookFolder + "/" + opfPath;

                doc = await Routines.GetFile(currentBookFolder, opfFile);
                var itemrefcount = doc.GetElementsByTagName("itemref").Count;
                int chapNameCount = 0;
                for (uint index = 0; index < itemrefcount; index++)
                {
                    //this.updatenotifier.Text = "Getting chapter " + index + " of " + itemrefcount + " ....";
                    var itemref = doc.GetElementsByTagName("itemref").Item(index).Attributes.GetNamedItem("idref").NodeValue;
                    try
                    {
                        //if (mediaType.ToString().Equals("application/xhtml+xml") && id.ToString().Equals(itemref.ToString()))
                        //{
                        //var chapPath = doc.GetElementsByTagName("item").Item(index).Attributes.GetNamedItem("href").NodeValue;
                        var chapPath = itemDict[itemref.ToString().ToLower()];
                        String[] filesPath = Routines.parseString(chapPath.ToString());
                        String chapContent = "";
                        if (chapContent == null || chapContent.Trim(' ') == " ")
                        {
                            continue;
                        }
                        chapNameCount++;
                        String chapName = "Chapter " + chapNameCount;
                        if (ncxDict.ContainsKey(itemref.ToString()))
                            chapName = ncxDict[itemref.ToString()];
                        else if (refDict.ContainsKey(itemref.ToString()))
                        {
                            if (ncxDict.ContainsKey(refDict[itemref.ToString()])) {
                                chapName = ncxDict[refDict[itemref.ToString()]];
                        }
                        else
                            chapName = "Chapter " + chapNameCount;
                        }
                        else
                            chapName = "Chapter " + chapNameCount;
                        chapContent = chapContent.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);

                        BookChapters.Chapters.Add(new BookChapter(chapNameCount.ToString(), chapName.ToString().ToUpper(), (currentBookFolder + filesPath[0]), filesPath[1]));
                        //break;
                        //}
                        //}
                    }
                    catch { }
                }

            }
            catch
            {
                //Routines.DisplayMsg("Epub files incorrectly formatted");
            }

        }

        private async void MasterListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lv = sender as ListView;
            BookChapter chapter = (BookChapter)lv.SelectedItem;
            if (chapter == null)
                return;

            if (lv.SelectedIndex != -1)
                returnIndex = lv.SelectedIndex;

            StorageFolder localFolder = await this.storageFolder.GetFolderAsync(chapter.Content.Replace("/", "\\"));
            StorageFile storageFile = await localFolder.GetFileAsync(chapter.FileName);
            Uri _baseUri = new Uri("ms-appdata:///local/");
            try
            {
                String fileContent = await FileIO.ReadTextAsync(storageFile);
                Paragraph chapContent = Routines.HTMLtoRTF(fileContent, localFolder);

                this.contentBlock.Blocks.Clear();
                this.contentBlock.Blocks.Add(chapContent);
                this.contentBlock1.Blocks.Clear();
                this.contentBlock1.Blocks.Add(chapContent);
            }
            catch { }
            this.SamplesSplitView.IsPaneOpen = false;
            this.currChapter.Text = returnIndex.ToString();
        }


        private void setColor(String color)
        {
            switch (color)
            {
                case "White":
                    this.mainGrid.Background = new SolidColorBrush(Windows.UI.Colors.White);
                    this.contentBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                    this.contentBlock1.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                    break;
                case "Black":
                    this.mainGrid.Background = new SolidColorBrush(Windows.UI.Colors.Black);
                    this.contentBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                    this.contentBlock1.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                    break;
                case "Paper":
                    this.mainGrid.Background = new SolidColorBrush(Windows.UI.Colors.Beige);
                    this.contentBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                    this.contentBlock1.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                    break;
            }
        }

        private void setFont(String fontfamily)
        {
            switch (fontfamily)
            {
                case "Georgia":
                    this.contentBlock.FontFamily = new FontFamily("Georgia");
                    this.contentBlock1.FontFamily = new FontFamily("Georgia");
                    break;
                case "Verdana":
                    this.contentBlock.FontFamily = new FontFamily("Verdana");
                    this.contentBlock1.FontFamily = new FontFamily("Verdana");
                    break;
                case "Segoe":
                    this.contentBlock.FontFamily = new FontFamily("Segoe UI");
                    this.contentBlock1.FontFamily = new FontFamily("Segoe UI");
                    break;
            }
        }
        private void setFontWeight(String fontweight)
        {
            switch (fontweight)
            {
                case "Semilight":
                    this.contentBlock.FontWeight = Windows.UI.Text.FontWeights.Light;
                    this.contentBlock1.FontWeight = Windows.UI.Text.FontWeights.Light;
                    break;
                case "Normal":
                    this.contentBlock.FontWeight = Windows.UI.Text.FontWeights.Normal;
                    this.contentBlock1.FontWeight = Windows.UI.Text.FontWeights.Normal;
                    break;
                case "Bold":
                    this.contentBlock.FontWeight = Windows.UI.Text.FontWeights.Bold;
                    this.contentBlock1.FontWeight = Windows.UI.Text.FontWeights.Bold;
                    break;
            }
        }
        private void setColumns(String cols)
        {
            switch (cols)
            {
                case "One":
                    contentBlock1.Width = App.fullWidth - 240;
                    App.readWidth = App.fullWidth;
                    break;
                case "Two":
                    contentBlock1.Width = App.halfWidth - 240;
                    App.readWidth = App.halfWidth;
                    break;
            }
        }

        private void setReadmode(String panorama)
        {
            if (panorama == "pano")
            {
                panoView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                columnView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                panoOn = true;
            }
            if (panorama == "cols")
            {
                panoView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                columnView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                panoOn = false;
            }
        }

        private void color_checked(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            setColor(button.Name);
            localSettings.Values["color"] = button.Name;
        }

        private void color_Loaded(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (localSettings.Values.ContainsKey("color"))
            {
                if (button.Name == localSettings.Values["color"].ToString())
                {
                    button.IsChecked = true;
                }
            }
        }

        private void font_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            setFont(button.Name);
            localSettings.Values["fontfamily"] = button.Name;
        }


        private void fontfamily_Loaded(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (localSettings.Values.ContainsKey("fontfamily"))
            {
                if (button.Name == localSettings.Values["fontfamily"].ToString())
                {
                    button.IsChecked = true;
                }
            }
        }

        private void fontweight_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            setFontWeight(button.Name);
            localSettings.Values["fontweight"] = button.Name;
        }

        private void fontweight_Loaded(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (localSettings.Values.ContainsKey("fontweight"))
            {
                if (button.Name == localSettings.Values["fontweight"].ToString())
                {
                    button.IsChecked = true;
                }
            }
        }

        private void column_checked(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            setColumns(button.Name);
            localSettings.Values["numcols"] = button.Name;
        }


        private void cols_Loaded(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (localSettings.Values.ContainsKey("numcols"))
            {
                if (button.Name == localSettings.Values["numcols"].ToString())
                {
                    button.IsChecked = true;
                }
            }
        }

        private void pano_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    panoView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    columnView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    localSettings.Values["readmode"] = "pano";
                    panoOn = true;
                }
                else
                {
                    panoView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    columnView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    localSettings.Values["readmode"] = "cols";
                    panoOn = false;
                }
            }
        }

        private void pano_Loaded(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (localSettings.Values.ContainsKey("readmode"))
            {
                if (localSettings.Values["readmode"].ToString() == "pano")
                {
                    toggleSwitch.IsOn = true;
                    panoOn = true;
                }
            }
        }
        
        private void mainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            //this.SamplesSplitView.IsPaneOpen = true;
            this.contentBlock.Width = Window.Current.Bounds.Width - 160;
            if (localSettings.Values.ContainsKey("color"))
            {
                setColor(localSettings.Values["color"].ToString());
            }
            if (localSettings.Values.ContainsKey("fontfamily"))
            {
                setFont(localSettings.Values["fontfamily"].ToString());
            }
            if (localSettings.Values.ContainsKey("fontweight"))
            {
                setFontWeight(localSettings.Values["fontweight"].ToString());
            }
            if (localSettings.Values.ContainsKey("numcols"))
            {
                setColumns(localSettings.Values["numcols"].ToString());
            }
            if (localSettings.Values.ContainsKey("readmode"))
            {
                setReadmode(localSettings.Values["readmode"].ToString());
            }
        }

        private void cols1_Loaded(object sender, RoutedEventArgs e)
        {
            contentBlock1.Width = App.readWidth - 200;
            this.columnView.ChangeView(offset, null, null);
        }

        private void previous_page(object sender, RoutedEventArgs e)
        {
            double off = this.panoView.HorizontalOffset - Window.Current.Bounds.Width;
            this.panoView.ChangeView(off, null, null);
        }

        private void next_page(object sender, RoutedEventArgs e)
        {
            double off = this.panoView.HorizontalOffset + Window.Current.Bounds.Width;
            this.panoView.ChangeView(off, null, null);
        }

        private void previous_Chapter(object sender, RoutedEventArgs e)
        {
            if (this.MasterListView.SelectedIndex > 1)
            {
                this.MasterListView.SelectedIndex = this.MasterListView.SelectedIndex - 1;
                this.panoView.ChangeView(0, null, null);
            }
        }

        private void next_Chapter(object sender, RoutedEventArgs e)
        {
            if (this.MasterListView.SelectedIndex < this.MasterListView.Items.Count - 1)
            {
                this.MasterListView.SelectedIndex = this.MasterListView.SelectedIndex + 1;
                this.panoView.ChangeView(0, null, null);
            }
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            this.SamplesSplitView.IsPaneOpen = !this.SamplesSplitView.IsPaneOpen;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.settingsPopup.IsPaneOpen = !this.settingsPopup.IsPaneOpen;
        }

        private void mainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.contentBlock.Width = Window.Current.Bounds.Width - 160;
            this.cols.ResetLayout();
        }

        private void panoView_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate == true)
            {
                return;
            }
            if ((this.panoView.HorizontalOffset % Window.Current.Bounds.Width) != 0)
            {
                int val = (int)(this.panoView.HorizontalOffset / Window.Current.Bounds.Width);
                this.panoView.ChangeView((val * Window.Current.Bounds.Width), null, null);
            }
        }
    }
}
