﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

#if !NO_WEB

using System;
using System.IO;
using System.Web;

namespace VDS.RDF.Update.Protocol
{
    /// <summary>
    /// Abstract Base class for SPARQL Uniform HTTP Protocol for Graph Management implementations
    /// </summary>
    public abstract class BaseProtocolProcessor : ISparqlHttpProtocolProcessor
    {
        /// <summary>
        /// Processes a GET operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessGet(HttpContext context);

        /// <summary>
        /// Processes a POST operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessPost(HttpContext context);

        /// <summary>
        /// Processes a PUT operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessPut(HttpContext context);

        /// <summary>
        /// Processes a DELETE operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessDelete(HttpContext context);

        /// <summary>
        /// Processes a HEAD operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessHead(HttpContext context);

        /// <summary>
        /// Processes an OPTIONS operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessOptions(HttpContext context);

        /// <summary>
        /// Processes a PATCH operation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public abstract void ProcessPatch(HttpContext context);

        /// <summary>
        /// Gets the Graph URI that the request should affect
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <returns></returns>
        protected Uri ResolveGraphUri(HttpContext context)
        {
            Uri graphUri;
            try
            {
                if (context.Request.QueryString["graph"] != null)
                {
                    graphUri = new Uri(context.Request.QueryString["graph"], UriKind.RelativeOrAbsolute);
                    if (!graphUri.IsAbsoluteUri)
                    {
                        //Need to resolve this relative URI against the Request URI
                        Uri baseUri = new Uri(context.Request.Url.AbsoluteUri);
                        graphUri = new Uri(Tools.ResolveUri(graphUri, baseUri));
                    }
                }
                else
                {
                    graphUri = new Uri(context.Request.Url.AbsoluteUri);
                }
            }
            catch (UriFormatException)
            {
                throw new SparqlHttpProtocolUriInvalidException();
            }

            return graphUri;
        }

        /// <summary>
        /// Gets the Graph URI that the request should affect
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <param name="g">Graph parsed from the request body</param>
        /// <returns></returns>
        /// <remarks>
        /// The Graph parameter may be null in which case the other overload of this method will be invoked
        /// </remarks>
        protected Uri ResolveGraphUri(HttpContext context, IGraph g)
        {
            if (g == null) return this.ResolveGraphUri(context);

            Uri graphUri;
            try
            {
                if (context.Request.QueryString["graph"] != null)
                {
                    graphUri = new Uri(context.Request.QueryString["graph"], UriKind.RelativeOrAbsolute);
                    if (!graphUri.IsAbsoluteUri)
                    {
                        //Need to resolve this relative URI against the Graph Base URI or Request URI as appropriate
                        Uri baseUri = (g.BaseUri != null) ? g.BaseUri : new Uri(context.Request.Url.AbsoluteUri);
                        graphUri = new Uri(Tools.ResolveUri(graphUri, baseUri));
                    }
                }
                else if (g.BaseUri != null)
                {
                    graphUri = g.BaseUri;
                }
                else
                {
                    graphUri = new Uri(context.Request.Url.AbsoluteUri);
                }
            }
            catch (UriFormatException)
            {
                throw new SparqlHttpProtocolUriInvalidException();
            }

            return graphUri;
        }

        /// <summary>
        /// Gets the Graph which can be parsed from the request body
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <returns></returns>
        /// <remarks>
        /// In the event that there is no request body a null will be returned
        /// </remarks>
        protected IGraph ParsePayload(HttpContext context)
        {
            if (context.Request.ContentLength == 0) return null;

            Graph g = new Graph();
            IRdfReader parser = MimeTypesHelper.GetParser(context.Request.ContentType);
            parser.Load(g, new StreamReader(context.Request.InputStream));
            g.NamespaceMap.Clear();

            return g;
        }
    }
}

#endif