// Taken from https://github.com/Vodurden/Http-Multipart-Data-Parser by Jake Woods
using System.IO;
using System.Linq;

namespace RHttpServer.Request.MultiPartFormParsing
{
    internal class FilePart
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="FilePart" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the input field used for the upload.
        /// </param>
        /// <param name="fileName">
        ///     The name of the file.
        /// </param>
        /// <param name="data">
        ///     The file data.
        /// </param>
        public FilePart(string name, string fileName, Stream data) :
            this(name, fileName, data, "text/plain", "form-data")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FilePart" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the input field used for the upload.
        /// </param>
        /// <param name="fileName">
        ///     The name of the file.
        /// </param>
        /// <param name="data">
        ///     The file data.
        /// </param>
        /// <param name="contentType">
        ///     The content type.
        /// </param>
        /// <param name="contentDisposition">
        ///     The content disposition.
        /// </param>
        public FilePart(string name, string fileName, Stream data, string contentType, string contentDisposition)
        {
            Name = name;
            FileName = fileName.Split(Path.GetInvalidFileNameChars()).Last();
            Data = data;
            ContentType = contentType;
            ContentDisposition = contentDisposition;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the data.
        /// </summary>
        public Stream Data { get; private set; }

        /// <summary>
        ///     Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the content-type. Defaults to text/plain if unspecified.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        ///     Gets or sets the content-disposition. Defaults to form-data if unspecified.
        /// </summary>
        public string ContentDisposition { get; set; }

        #endregion
    }
}