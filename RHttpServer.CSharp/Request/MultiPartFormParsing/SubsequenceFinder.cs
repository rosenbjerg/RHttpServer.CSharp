// Taken from https://github.com/Vodurden/Http-Multipart-Data-Parser by Jake Woods

using System.Collections.Generic;

namespace RHttpServer.Request.MultiPartFormParsing
{
    internal sealed class SubsequenceFinder
    {
        #region Public Methods and Operators

        public static int Search(byte[] haystack, byte[] needle)
        {
            return Search(haystack, needle, haystack.Length);
        }

        /// <summary>
        ///     Finds if a sequence exists within another sequence.
        /// </summary>
        /// <param name="haystack">
        ///     The sequence to search
        /// </param>
        /// <param name="needle">
        ///     The sequence to look for
        /// </param>
        /// <param name="haystackLength">
        ///     The length of the haystack to consider for searching
        /// </param>
        /// <returns>
        ///     The start position of the found sequence or -1 if nothing was found
        /// </returns>
        public static int Search(byte[] haystack, byte[] needle, int haystackLength)
        {
            var charactersInNeedle = new HashSet<byte>(needle);

            var length = needle.Length;
            var index = 0;
            while (index + length <= haystackLength)
            {
                // Worst case scenario: Go back to character-by-character parsing until we find a non-match
                // or we find the needle.
                if (charactersInNeedle.Contains(haystack[index + length - 1]))
                {
                    var needleIndex = 0;
                    while (haystack[index + needleIndex] == needle[needleIndex])
                    {
                        if (needleIndex == needle.Length - 1)
                            return index;

                        needleIndex += 1;
                    }

                    index += 1;
                    index += needleIndex;
                    continue;
                }

                index += length;
            }

            return -1;
        }

        #endregion
    }
}