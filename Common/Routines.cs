using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace MasterDetailApp.Common
{
    class Routines
    {
        public static async Task<Windows.Data.Xml.Dom.XmlDocument> GetFile(String folder, String file)
        {
            Windows.Storage.StorageFolder storFolder;
            Windows.Storage.StorageFile storageFile;
            try
            {
                storFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folder.Replace("/", "\\"));
                storageFile = await storFolder.GetFileAsync(file);
                Windows.Data.Xml.Dom.XmlLoadSettings loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
                loadSettings.ProhibitDtd = false;
                loadSettings.ResolveExternals = false;
                Windows.Data.Xml.Dom.XmlDocument xmldoc = await Windows.Data.Xml.Dom.XmlDocument.LoadFromFileAsync(storageFile, loadSettings);
                return xmldoc;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static String[] parseString(String path)
        {
            String[] finalPath = new String[2];
            String[] tempPath = null;
            int i;
            if (path.ToString().Contains("/"))
            {
                tempPath = path.Split('/');
                for (i = 0; i < tempPath.Length - 1; i++)
                {
                    finalPath[0] = finalPath[0] + tempPath[i] + "/";
                }
                finalPath[1] = tempPath[i];
            }
            else
            {
                finalPath[0] = "";
                finalPath[1] = path;
            }
            return finalPath;
        }

        public static async void DisplayMsg(String title, String content)
        {
            // Create the message dialog and set its content; it will get a default "Close" button since there aren't any other buttons being added
            Windows.UI.Popups.MessageDialog msg = new Windows.UI.Popups.MessageDialog("Welcome");
            msg.Title = title;
            msg.Content = content;
            try
            {
                Windows.UI.Popups.IUICommand result = await msg.ShowAsync();
            }
            catch
            { }
        }

        public static async Task resetFolders()
        {
            Windows.Storage.StorageFolder localfolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile firstfile = await localfolder.GetFileAsync("firsttime");
            await firstfile.DeleteAsync();
            var folders = await localfolder.GetFoldersAsync();
            foreach (Windows.Storage.StorageFolder folder in folders)
            {
                await folder.DeleteAsync();
            }
            Windows.Storage.StorageFolder stor = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("thumbs", Windows.Storage.CreationCollisionOption.FailIfExists);
            Uri uri = new Uri("ms-appx:///Assets/bookImage.jpg");
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            Windows.Storage.StorageFolder imageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync("thumbs");
            await file.CopyAsync(imageFolder, "default.jpg");
        }

        public static async Task copysample()
        {
            Uri uri = new Uri("ms-appx:///Assets/ab.png");
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            Windows.Storage.StorageFile copiedfile = await file.CopyAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "ab.png");
            await Routines.extractFiles(copiedfile);
            uri = new Uri("ms-appdata:///local/ab/OPS/images/cover.png");
            file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            Windows.Storage.StorageFolder imageFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync("thumbs");
            await file.CopyAsync(imageFolder, "book0.jpg");
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = new Windows.Storage.ApplicationDataCompositeValue();
            composite["uniqueid"] = "book0";
            composite["title"] = "The Arabian Nights";
            composite["author"] = "Andrew Lang";
            composite["image"] = "thumbs\\book0.jpg"; ;
            composite["location"] = "ab";
            var samplebook = new Book(composite["uniqueid"].ToString(), composite["title"].ToString(), composite["author"].ToString(),
                                                                composite["image"].ToString(), composite["location"].ToString());
            BookSource.AddBookAsync(samplebook);
            int count = (int)localSettings.Values["bookCount"];
            localSettings.Values["bookCount"] = ++count;
            localSettings.Containers["booksContainer"].Values["book0"] = composite;
        }


        public static async Task<Windows.Storage.StorageFolder> extractFiles(Windows.Storage.StorageFile zipfilename)
        {
            Windows.Storage.StorageFolder storfolder = null;
            try
            {
                // Create stream for compressed files in memory
                using (MemoryStream zipMemoryStream = new MemoryStream())
                {
                    using (Windows.Storage.Streams.IRandomAccessStream zipStream = await zipfilename.OpenAsync(Windows.Storage.FileAccessMode.Read))
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
                    storfolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(zipfilename.DisplayName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
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
                                    Windows.Storage.StorageFile uncompressedFile = await storfolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);

                                    // Store the contents
                                    using (Windows.Storage.Streams.IRandomAccessStream uncompressedFileStream = await uncompressedFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
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
                if (storfolder != null) await storfolder.DeleteAsync();
                return null;
            }
            finally
            {
                zipfilename.DeleteAsync();
            }
            return storfolder;
        }

        public static async Task LoadBooks()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            bool config = true;
            try {
                Windows.Storage.StorageFile firstfile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("firsttime");
            }
            catch (System.IO.FileNotFoundException)
            {
                await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("firsttime");
                await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("thumbs", CreationCollisionOption.OpenIfExists);
                localSettings.Values.Clear();
                localSettings.Values["bookCount"] = 0;
                localSettings.Values["thumbFolder"] = "thumbs";
                try
                {
                    localSettings.CreateContainer("booksContainer", Windows.Storage.ApplicationDataCreateDisposition.Existing);
                }
                catch
                {
                    localSettings.CreateContainer("booksContainer", Windows.Storage.ApplicationDataCreateDisposition.Always);
                }
                try
                {
                    await resetFolders();
                }
                catch { }
                await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("firsttime");
                await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("thumbs", CreationCollisionOption.OpenIfExists);
                Windows.Storage.ApplicationDataCompositeValue composite = new Windows.Storage.ApplicationDataCompositeValue();
                composite["uniqueid"] = "";
                composite["location"] = "";
                composite["title"] = "";
                composite["author"] = "";
                composite["image"] = "";
                composite["chapter"] = "0";
                composite["offset"] = "0";
                composite["count"] = "0";
                localSettings.Values["nowReading"] = composite;
                localSettings.Values["color"] = "White";
                localSettings.Values["fontfamily"] = "Segoe";
                localSettings.Values["fontweight"] = "Normal";
                localSettings.Values["readmode"] = "pano";
                localSettings.Values["numcols"] = "One";
                await copysample();
                DisplayMsg("Happy Reading", "No books here. Add books by Right Clicking with mouse or swiping from bottom of your tablet.");
                config = false;
            }

            if (config == true)
            {
                if ((int)localSettings.Values["bookCount"] == 0)
                {
                    DisplayMsg("Happy Reading", "No books here. Add books by Right Clicking with mouse or swiping from bottom of your tablet.");
                }

                if (localSettings.Containers.ContainsKey("booksContainer"))
                {
                    foreach (KeyValuePair<String, object> book in localSettings.Containers["booksContainer"].Values)
                    {
                        Windows.Storage.ApplicationDataCompositeValue currentBook = (Windows.Storage.ApplicationDataCompositeValue)book.Value;
                        var newbook = new Book(currentBook["uniqueid"].ToString(), currentBook["title"].ToString(), currentBook["author"].ToString(),
                                                                    currentBook["image"].ToString(), currentBook["location"].ToString());
                        BookSource.AddBookAsync(newbook);
                    }
                }
            }
        }

        public static int GetIndex(string[] Table, string Code)
        {
            int I = 0, getindex = 0;
            bool bFound = false;
            try
            {

                //Get the index 
                if (Table.Length != 0)
                {
                    for (I = 0; I < Table.Length; I++)
                    {
                        if (Code == Table[I])
                        {
                            bFound = true;
                            break;
                        }
                    }
                }
                else
                    bFound = false;

                //return it 
                if (bFound == false)
                    getindex = 0;
                else
                    getindex = I;

            }
            catch (Exception)
            {
                //Debug.WriteLine("" + e);
            }
            return getindex;
        }

        public static string cleanupSTYLE(string sHTML)
        {
            string sText = "";
            string sToken = "";
            int lLen = 0, lCurrentToken = 0;
            int lStart = 0, lEnd = 0;

            sHTML.Trim();

            lLen = sHTML.Length;
            lStart = 1;
            lEnd = 1;

            for (lCurrentToken = 1; lCurrentToken < lLen; lCurrentToken++)
            {
                lStart = sHTML.IndexOf("<", lEnd);
                if (lStart < 0)
                    goto Completed;
                lEnd = sHTML.IndexOf(">", lStart);
                sToken = sHTML.Substring(lStart, lEnd - lStart + 1).ToUpper();
                if (sToken.Contains("<STYLE"))
                {
                    sText = sHTML.Substring(0, lStart);
                    lStart = sHTML.IndexOf("<", lEnd);
                    if (lStart < 0)
                        goto Completed;
                    lEnd = sHTML.IndexOf(">", lStart);
                    sToken = sHTML.Substring(lStart, lEnd - lStart + 1).ToUpper();
                    sText = sText + sHTML.Substring(lEnd + 1, sHTML.Length - lEnd - 1);
                    break;
                }
            }

            Completed:
            if (sText.Equals(""))
            {
                return sHTML;
            }
            else
            {
                return sText;
            }

        }
        public static Paragraph HTMLtoRTF(string sHTML, StorageFolder storageFolder)
        {
            string[] ColorTable = new string[0];
            string[] FontTable = new string[0];
            int lStart = 0, lEnd = 0, imgStart = 0, imgEnd = 0;
            string sRTF = "", sText = "", imgText = "";
            int lLen = 0, lCurrentToken = 0;
            string sToken = "", sTemp = "";
            bool bUseDefaultFace = false;
            Uri _baseUri = new Uri(storageFolder.Path.ToString() + "\\");

            Paragraph paragraph = new Paragraph();
            //var inlines = paragraph.Inlines;

            sHTML = cleanupSTYLE(sHTML);
            //Fix the HTML 
            sHTML.Trim();
            sHTML = sHTML.Replace("<STRONG>", "<B>");
            sHTML = sHTML.Replace("</STRONG>", "</B>");
            sHTML = sHTML.Replace("<EM>", "<I>");
            sHTML = sHTML.Replace("</EM>", "</I>");
            sHTML = sHTML.Replace("\n", "");
            sHTML = sHTML.Replace("&nbsp;", "\\~");
            //sHTML = sHTML.Replace("<img", "<IMG>");
            //Initialize 
            lLen = sHTML.Length;
            lStart = 1;
            lEnd = 1;
            //Parse the HTML 
            for (lCurrentToken = 1; lCurrentToken < lLen; lCurrentToken++)
            {
                lStart = sHTML.IndexOf("<", lEnd);
                if (lStart < 0)
                    goto Completed;
                lEnd = sHTML.IndexOf(">", lStart);
                sToken = sHTML.Substring(lStart, lEnd - lStart + 1).ToUpper();
                //Take action 
                switch (sToken)
                {
                    case "<B>":
                        sRTF = sRTF + "\\b1";
                        break;
                    case "</B>":
                        sRTF = sRTF + "\\b0";
                        break;
                    case "<I>":
                        sRTF = sRTF + "\\i1";
                        break;
                    case "</I>":
                        sRTF = sRTF + "\\i0";
                        break;
                    case "<U>":
                        sRTF = sRTF + "\\ul1";
                        break;
                    case "</U>":
                        sRTF = sRTF + "\\ul0";
                        break;
                    case "<TR>":
                        sRTF = sRTF + "\\intbl";
                        break;
                    case "</TR>":
                        sRTF = sRTF + "\\row";
                        break;
                    case "<TD>":
                    case "</TD>":
                        sRTF = sRTF + "\\cell ";
                        break;
                    case "<BR/>":
                    case "</P>":
                        break;
                    case "</SPAN>":
                        bUseDefaultFace = true;
                        break;
                    case "<IMG>":
                        //Get the text 
                        lEnd = sHTML.IndexOf(">", lEnd + 1);
                        
                        sText = sHTML.Substring(lStart, (lEnd - lStart) + 1);
                        imgStart = sText.IndexOf("\"", 0);
                        imgEnd = sText.IndexOf("\"", imgStart + 1);
                        imgText = (sText.Substring(imgStart, (imgEnd - imgStart) + 1)).Replace("\"", "");
                        Windows.UI.Xaml.Documents.Run run = new Windows.UI.Xaml.Documents.Run();
                        run.Text = "\n";
                        paragraph.Inlines.Add(run);
                        Image im = new Image();
                        im.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(_baseUri, imgText));
                        //im.Width = 240;
                        //im.Height = 200;
                        InlineUIContainer a = new InlineUIContainer();
                        a.Child = im;
                        paragraph.Inlines.Add(a);
                        run = new Windows.UI.Xaml.Documents.Run();
                        run.Text = "\n";
                        paragraph.Inlines.Add(run);
                        
                        break;
                    default:
                        break;
                }
                //Get the text 
                lStart = sHTML.IndexOf(">", lEnd);
                //Debug.WriteLine("lStart: " + lStart); 
                if (lStart < 0)
                    goto Completed;
                lEnd = sHTML.IndexOf("<", lStart);
                //Debug.WriteLine("lEnd: " + lEnd); 
                if (lEnd < 0)
                    goto Completed;
                sText = sHTML.Substring(lStart, (lEnd - lStart) + 1);
                sText = sText.Trim();
                sText = sText.Replace("<", "");
                sText = sText.Replace(">", "");
                if ((sText).Length >= 1)
                {
                    sText = sText.Substring(0, (sText).Length);
                    //check out for special characters 
                    sText = sText.Replace("\\", "\\\\");
                    sText = sText.Replace("{", "\\{");
                    sText = sText.Replace("}", "\\}");
                    Windows.UI.Xaml.Documents.Run run = new Windows.UI.Xaml.Documents.Run();
                    run.Text = sText + "\n";
                    paragraph.Inlines.Add(run);
                    sRTF = sRTF + sText + "\n";
                }
            }
            Completed:
            
            return paragraph;
        }

    }
}
