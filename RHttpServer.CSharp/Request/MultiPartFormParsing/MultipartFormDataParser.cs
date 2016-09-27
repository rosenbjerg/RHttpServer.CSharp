// Taken from https://github.com/Vodurden/Http-Multipart-Data-Parser by Jake Woods
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RHttpServer.Request.MultiPartFormParsing
{
    internal class MultipartFormDataParser
    {
        #region Constants

        /// <summary>
        ///     The default buffer size.
        /// </summary>
        private const int DefaultBufferSize = 4096;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartFormDataParser" /> class
        ///     with an input stream. Boundary will be automatically detected based on the
        ///     first line of input.
        /// </summary>
        /// <param name="stream">
        ///     The stream containing the multipart data
        /// </param>
        public MultipartFormDataParser(Stream stream)
            : this(stream, null, Encoding.UTF8, DefaultBufferSize)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartFormDataParser" /> class
        ///     with the boundary and input stream.
        /// </summary>
        /// <param name="stream">
        ///     The stream containing the multipart data
        /// </param>
        /// <param name="boundary">
        ///     The multipart/form-data boundary. This should be the value
        ///     returned by the request header.
        /// </param>
        public MultipartFormDataParser(Stream stream, string boundary)
            : this(stream, boundary, Encoding.UTF8, DefaultBufferSize)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartFormDataParser" /> class
        ///     with the input stream and stream encoding. Boundary is automatically
        ///     detected.
        /// </summary>
        /// <param name="stream">
        ///     The stream containing the multipart data
        /// </param>
        /// <param name="encoding">
        ///     The encoding of the multipart data
        /// </param>
        public MultipartFormDataParser(Stream stream, Encoding encoding)
            : this(stream, null, encoding, DefaultBufferSize)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartFormDataParser" /> class
        ///     with the boundary, input stream and stream encoding.
        /// </summary>
        /// <param name="stream">
        ///     The stream containing the multipart data
        /// </param>
        /// <param name="boundary">
        ///     The multipart/form-data boundary. This should be the value
        ///     returned by the request header.
        /// </param>
        /// <param name="encoding">
        ///     The encoding of the multipart data
        /// </param>
        public MultipartFormDataParser(Stream stream, string boundary, Encoding encoding)
            : this(stream, boundary, encoding, DefaultBufferSize)
        {
            // 4096 is the optimal buffer size as it matches the internal buffer of a StreamReader
            // See: http://stackoverflow.com/a/129318/203133
            // See: http://msdn.microsoft.com/en-us/library/9kstw824.aspx (under remarks)
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartFormDataParser" /> class
        ///     with the stream, input encoding and buffer size. Boundary is automatically
        ///     detected.
        /// </summary>
        /// <param name="stream">
        ///     The stream containing the multipart data
        /// </param>
        /// <param name="encoding">
        ///     The encoding of the multipart data
        /// </param>
        /// <param name="binaryBufferSize">
        ///     The size of the buffer to use for parsing the multipart form data. This must be larger
        ///     then (size of boundary + 4 + # bytes in newline).
        /// </param>
        public MultipartFormDataParser(Stream stream, Encoding encoding, int binaryBufferSize)
            : this(stream, null, encoding, binaryBufferSize)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipartFormDataParser" /> class
        ///     with the boundary, stream, input encoding and buffer size.
        /// </summary>
        /// <param name="stream">
        ///     The stream containing the multipart data
        /// </param>
        /// <param name="boundary">
        ///     The multipart/form-data boundary. This should be the value
        ///     returned by the request header.
        /// </param>
        /// <param name="encoding">
        ///     The encoding of the multipart data
        /// </param>
        /// <param name="binaryBufferSize">
        ///     The size of the buffer to use for parsing the multipart form data. This must be larger
        ///     then (size of boundary + 4 + # bytes in newline).
        /// </param>
        public MultipartFormDataParser(Stream stream, string boundary, Encoding encoding, int binaryBufferSize)
        {
            Files = new List<FilePart>();
            Parameters = new List<ParameterPart>();

            var streamingParser = new StreamingMultipartFormDataParser(stream, boundary, encoding, binaryBufferSize);
            streamingParser.ParameterHandler += parameterPart => Parameters.Add(parameterPart);

            streamingParser.FileHandler += (name, fileName, type, disposition, buffer, bytes) =>
            {
                if ((Files.Count == 0) || (name != Files[Files.Count - 1].Name))
                    Files.Add(new FilePart(name, fileName, new MemoryStream(), type, disposition));

                Files[Files.Count - 1].Data.Write(buffer, 0, bytes);
            };

            streamingParser.Run();

            // Reset all the written memory streams so they can be read.
            foreach (var file in Files)
                file.Data.Position = 0;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the mapping of parameters parsed files. The name of a given field
        ///     maps to the parsed file data.
        /// </summary>
        public List<FilePart> Files { get; }

        /// <summary>
        ///     Gets the parameters. Several ParameterParts may share the same name.
        /// </summary>
        public List<ParameterPart> Parameters { get; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Returns true if the parameter has any values. False otherwise
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>True if the parameter exists. False otherwise</returns>
        public bool HasParameter(string name)
        {
            return Parameters.Any(p => p.Name == name);
        }

        /// <summary>
        ///     Returns the value of a parameter or null if it doesn't exist.
        ///     You should only use this method if you're sure the parameter has only one value.
        ///     If you need to support multiple values use GetParameterValues.
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>The value of the parameter</returns>
        public string GetParameterValue(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name == name).Data;
        }

        /// <summary>
        ///     Returns the values of a parameter or an empty enumerable if the parameter doesn't exist.
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>The values of the parameter</returns>
        public IEnumerable<string> GetParameterValues(string name)
        {
            return Parameters
                .Where(p => p.Name == name)
                .Select(p => p.Data);
        }

        #endregion
    }
}