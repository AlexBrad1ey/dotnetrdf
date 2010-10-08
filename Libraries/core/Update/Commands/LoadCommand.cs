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

using System;
using VDS.RDF.Parsing;

namespace VDS.RDF.Update.Commands
{
    /// <summary>
    /// Represents the SPARQL Update LOAD command
    /// </summary>
    public class LoadCommand : SparqlUpdateCommand
    {
        private Uri _sourceUri, _graphUri;

        /// <summary>
        /// Creates a new LOAD command
        /// </summary>
        /// <param name="sourceUri">Source URI to load data from</param>
        /// <param name="graphUri">Target URI for the Graph to store data in</param>
        public LoadCommand(Uri sourceUri, Uri graphUri)
            : base(SparqlUpdateCommandType.Load) 
        {
            if (sourceUri == null) throw new ArgumentNullException("sourceUri");
            this._sourceUri = sourceUri;
            this._graphUri = graphUri;
        }

        /// <summary>
        /// Creates a new LOAD command which operates on the Default Graph
        /// </summary>
        /// <param name="sourceUri">Source URI to load data from</param>
        public LoadCommand(Uri sourceUri)
            : this(sourceUri, null) { }

        /// <summary>
        /// Gets the URI that data is loaded from
        /// </summary>
        public Uri SourceUri
        {
            get
            {
                return this._sourceUri;
            }
        }

        /// <summary>
        /// Gets the URI of the Graph to load data into
        /// </summary>
        public Uri TargetUri
        {
            get
            {
                return this._graphUri;
            }
        }

        /// <summary>
        /// Evaluates the Command in the given Context
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        public override void Evaluate(SparqlUpdateEvaluationContext context)
        {
            //Q: Does LOAD into a named Graph require that Graph to be pre-existing?
            //if (this._graphUri != null)
            //{
            //    //When adding to specific Graph need to ensure that Graph exists
            //    //In the case when we're adding to the default graph we'll create it if it doesn't exist
            //    if (!context.Data.HasGraph(this._graphUri))
            //    {
            //        throw new RdfUpdateException("Cannot LOAD into a Graph that does not exist in the Store");
            //    }
            //}

            //Load from the URI
            Graph g = new Graph();
            UriLoader.Load(g, this._sourceUri);

            //Now merge into the appropriate Graph
            //Set the Base URI to the URI of the destination Graph
            g.BaseUri = this._graphUri;
            context.Data.AddGraph(g);
        }

        /// <summary>
        /// Processes the Command using the given Update Processor
        /// </summary>
        /// <param name="processor">SPARQL Update Processor</param>
        public override void Process(ISparqlUpdateProcessor processor)
        {
            processor.ProcessLoadCommand(this);
        }

        /// <summary>
        /// Gets the String representation of the Command
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this._graphUri == null)
            {
                return "LOAD <" + this._sourceUri.ToString().Replace(">", "\\>") + ">";
            }
            else
            {
                return "LOAD <" + this._sourceUri.ToString().Replace(">", "\\>") + "> INTO <" + this._graphUri.ToString().Replace(">", "\\>") + ">";
            }
        }
    }
}
