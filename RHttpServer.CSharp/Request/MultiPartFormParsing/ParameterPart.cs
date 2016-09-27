// Taken from https://github.com/Vodurden/Http-Multipart-Data-Parser by Jake Woods
namespace RHttpServer.Request.MultiPartFormParsing
{
    internal class ParameterPart
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterPart" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="data">
        ///     The data.
        /// </param>
        public ParameterPart(string name, string data)
        {
            Name = name;
            Data = data;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the data.
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        #endregion
    }
}