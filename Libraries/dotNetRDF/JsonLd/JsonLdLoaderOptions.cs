﻿/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2020 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

namespace VDS.RDF.JsonLd
{
    public class JsonLdLoaderOptions
    {
        /// <summary>
        /// If set to true, when extracting JSON-LD script elements from HTML, unless a specific fragment identifier is targeted,
        /// extracts all encountered JSON-LD script elements using an array form, if necessary.
        /// </summary>
        public bool ExtractAllScripts;

        /// <summary>
        /// When the resulting contentType is text/html or application/xhtml+xml, this option determines the profile to use for selecting JSON-LD script elements.
        /// </summary>
        public string Profile;

        /// <summary>
        /// One or more IRIs to use in the request as a profile parameter. (See IANA Considerations in [JSON-LD11]).
        /// </summary>
        public string RequestProfile;
    }
}