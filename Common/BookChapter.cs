using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterDetailApp.Common
{
    public class BookChapter
    {
        public BookChapter(String uniqueId, String title, String content, String filename)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Content = content;
            this.FileName = filename;
        }

        public string UniqueId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string FileName { get; set; }
    }

    public sealed class BookChapters
    {
        private static ObservableCollection<BookChapter> _chapter = new ObservableCollection<BookChapter>();
        public static ObservableCollection<BookChapter> Chapters
        {
            get { return _chapter; }
        }
    }
    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class Book
    {
        private static Uri _baseUri = new Uri("ms-appdata:///local/");
        public Book(String uniqueId, String title, String author, String imagePath, String location)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Author = author;
            this.ImagePath = new Uri("ms-appdata:///local/" + imagePath.Replace("\\", "/"));
            this.Location = location;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Author { get; private set; }
        public Uri ImagePath { get; private set; }
        public string Location { get; private set; }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class BookSource
    {
        private static BookSource _bookDataSource = new BookSource();

        private ObservableCollection<Book> _books = new ObservableCollection<Book>();
        public ObservableCollection<Book> Books
        {
            get { return this._books; }
        }

        public static void AddBookAsync(Book book)
        {
            _bookDataSource.Books.Add(book);
        }


        public static void ClearBooks()
        {
            _bookDataSource.Books.Clear();
        }

        public static IEnumerable<Book> GetGroupsAsync()
        {
            return _bookDataSource.Books;
        }

        public static async Task<Book> GetGroupAsync(string uniqueId)
        {
            //await _bookDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _bookDataSource.Books.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        //public static async Task<BookChapter> GetItemAsync(string uniqueId)
        //{
        //  await _bookDataSource.GetSampleDataAsync();
        // Simple linear search is acceptable for small data sets
        //   var matches = _bookDataSource.Books.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
        //   if (matches.Count() == 1) return matches.First();
        //   return null;
        // }


    }

    public class PhotoItem
    {
        public string Loc { get; set; }
        public string Name { get; set; }
        public Uri PictureUri { get; set; }
    }

    public class BookPhotos
    {
        private static ObservableCollection<PhotoItem> photos = new ObservableCollection<PhotoItem>();
        public static ObservableCollection<PhotoItem> Photos
        {
            get { return photos; }
        }
    }

    public class imageList
    {

        public imageList(String imageLoc, String imageName)
        {
            this._imagePath = imageLoc + "/" + imageName;
            this._imageName = imageName;
            this._imageLoc = imageLoc;
        }
        private Uri _baseUri = new Uri("ms-appdata:///local/");
        private Windows.UI.Xaml.Media.ImageSource _image = null;
        public String _imagePath = null;
        public String _imageName = null;
        public String _imageLoc = null;

        public Windows.UI.Xaml.Media.ImageSource Image
        {
            get
            {
                if (this._image == null && this._imagePath != null)
                {
                    this._image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(this._baseUri, this._imagePath));
                }
                return this._image;
            }

            set
            {
                this._imagePath = null;
            }
        }

        public void SetImage(String path)
        {
            this._image = null;
            this._imagePath = path;
        }
    }
}