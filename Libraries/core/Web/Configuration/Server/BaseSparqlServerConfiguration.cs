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

#if !NO_WEB && !NO_ASP

using System;
using System.IO;
using System.Web;
using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Update;
using VDS.RDF.Update.Protocol;

namespace VDS.RDF.Web.Configuration.Server
{
    /// <summary>
    /// Abstract Base class for Handler Configuration for SPARQL Servers
    /// </summary>
    public abstract class BaseSparqlServerConfiguration : BaseHandlerConfiguration
    {
        /// <summary>
        /// Query processor
        /// </summary>
        protected ISparqlQueryProcessor _queryProcessor;
        /// <summary>
        /// Update processor
        /// </summary>
        protected ISparqlUpdateProcessor _updateProcessor;
        /// <summary>
        /// Protocol processor
        /// </summary>
        protected ISparqlHttpProtocolProcessor _protocolProcessor;

        #region Query Variables and Properties

        /// <summary>
        /// Default Graph Uri for queries
        /// </summary>
        protected String _defaultGraph = String.Empty;
        /// <summary>
        /// Default Timeout for Queries
        /// </summary>
        protected long _defaultTimeout = 30000;
        /// <summary>
        /// Default Partial Results on Timeout behaviour
        /// </summary>
        protected bool _defaultPartialResults = false;
        /// <summary>
        /// Whether the Handler supports Timeouts
        /// </summary>
        protected bool _supportsTimeout = false;
        /// <summary>
        /// Whether the Handler supports Partial Results on Timeout
        /// </summary>
        protected bool _supportsPartialResults = false;
        /// <summary>
        /// Querystring Field name for the Timeout setting
        /// </summary>
        protected String _timeoutField = "timeout";
        /// <summary>
        /// Querystring Field name for the Partial Results setting
        /// </summary>
        protected String _partialResultsField = "partialResults";

        /// <summary>
        /// Whether a Query Form should be shown to the User
        /// </summary>
        protected bool _showQueryForm = true;
        /// <summary>
        /// Default Sparql Query
        /// </summary>
        protected String _defaultQuery = String.Empty;

        /// <summary>
        /// Gets the Default Graph Uri
        /// </summary>
        public String DefaultGraphURI
        {
            get
            {
                return this._defaultGraph;
            }
        }

        /// <summary>
        /// Whether the Remote Endpoint supports specifying Query Timeout as a querystring parameter
        /// </summary>
        public bool SupportsTimeout
        {
            get
            {
                return this._supportsTimeout;
            }
        }

        /// <summary>
        /// Gets the Default Query Execution Timeout
        /// </summary>
        public long DefaultTimeout
        {
            get
            {
                return this._defaultTimeout;
            }
        }

        /// <summary>
        /// Querystring field name for the Query Timeout for Remote Endpoints which support it
        /// </summary>
        public String TimeoutField
        {
            get
            {
                return this._timeoutField;
            }
        }

        /// <summary>
        /// Whether the Remote Endpoint supports specifying Partial Results on Timeout behaviour as a querystring parameter
        /// </summary>
        public bool SupportsPartialResults
        {
            get
            {
                return this._supportsPartialResults;
            }
        }

        /// <summary>
        /// Gets the Default Partial Results on Timeout behaviour
        /// </summary>
        public bool DefaultPartialResults
        {
            get
            {
                return this._defaultPartialResults;
            }
        }

        /// <summary>
        /// Querystring field name for the Partial Results on Timeout setting for Remote Endpoints which support it
        /// </summary>
        public String PartialResultsField
        {
            get
            {
                return this._partialResultsField;
            }
        }


        /// <summary>
        /// Gets whether the Query Form should be shown to users
        /// </summary>
        public bool ShowQueryForm
        {
            get
            {
                return this._showQueryForm;
            }
        }

        /// <summary>
        /// Gets the Default Query for the Query Form
        /// </summary>
        public String DefaultQuery
        {
            get
            {
                return this._defaultQuery;
            }
        }

        #endregion

        #region Update Variables and Properties

        /// <summary>
        /// Whether Update Form should be shown
        /// </summary>
        protected bool _showUpdateForm = true;
        /// <summary>
        /// Whether the Handler should stop processing commands if a command errors
        /// </summary>
        protected bool _haltOnError = true;
        /// <summary>
        /// Default Update Text for the Update Form
        /// </summary>
        protected String _defaultUpdate = String.Empty;

        /// <summary>
        /// Gets whether to show the Update Form if no update is specified
        /// </summary>
        public bool ShowUpdateForm
        {
            get
            {
                return this._showUpdateForm;
            }
        }

        /// <summary>
        /// Gets whether to Halt on Errors
        /// </summary>
        public bool HaltOnError
        {
            get
            {
                return this._haltOnError;
            }
        }

        /// <summary>
        /// Gets the Default Update for the Update Form
        /// </summary>
        public String DefaultUpdate
        {
            get
            {
                return this._defaultUpdate;
            }
        }

        #endregion

        #region Protocol Variables and Properties

        #endregion

        /// <summary>
        /// Creates a new Base SPARQL Server Configuration based on information from a Configuration Graph
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <param name="g">Configuration Graph</param>
        /// <param name="objNode">Object Node</param>
        public BaseSparqlServerConfiguration(HttpContext context, IGraph g, INode objNode)
            : base(context, g, objNode)
        {
            //Get the Query Processor to be used
            INode procNode = ConfigurationLoader.GetConfigurationNode(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyQueryProcessor));
            if (procNode != null)
            {
                Object temp = ConfigurationLoader.LoadObject(g, procNode);
                if (temp is ISparqlQueryProcessor)
                {
                    this._queryProcessor = (ISparqlQueryProcessor)temp;
                }
                else
                {
                    throw new DotNetRdfConfigurationException("Unable to load SPARQL Server Configuration as the RDF configuration file specifies a value for the Handlers dnr:queryProcessor property which cannot be loaded as an object which implements the ISparqlQueryProcessor interface");
                }
            }

            //SPARQL Query Default Config
            this._defaultGraph = ConfigurationLoader.GetConfigurationValue(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyDefaultGraphUri)).ToSafeString();
            this._defaultTimeout = ConfigurationLoader.GetConfigurationInt64(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyTimeout), this._defaultTimeout);
            this._defaultPartialResults = ConfigurationLoader.GetConfigurationBoolean(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyPartialResults), this._defaultPartialResults);

            //Handler Configuration
            this._showQueryForm = ConfigurationLoader.GetConfigurationBoolean(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyShowQueryForm), this._showQueryForm);
            String defQueryFile = ConfigurationLoader.GetConfigurationString(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyDefaultQueryFile));
            if (defQueryFile != null)
            {
                defQueryFile = context.Server.MapPath(defQueryFile);
                if (File.Exists(defQueryFile))
                {
                    using (StreamReader reader = new StreamReader(defQueryFile))
                    {
                        this._defaultQuery = reader.ReadToEnd();
                        reader.Close();
                    }
                }
            }

            //Then get the Update Processor to be used
            procNode = ConfigurationLoader.GetConfigurationNode(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyUpdateProcessor));
            if (procNode != null)
            {
                Object temp = ConfigurationLoader.LoadObject(g, procNode);
                if (temp is ISparqlUpdateProcessor)
                {
                    this._updateProcessor = (ISparqlUpdateProcessor)temp;
                }
                else
                {
                    throw new DotNetRdfConfigurationException("Unable to load SPARQL Server Configuration as the RDF configuration file specifies a value for the Handlers dnr:updateProcessor property which cannot be loaded as an object which implements the ISparqlUpdateProcessor interface");
                }
            }

            //Handler Settings
            this._haltOnError = ConfigurationLoader.GetConfigurationBoolean(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyHaltOnError), this._haltOnError);
            this._showUpdateForm = ConfigurationLoader.GetConfigurationBoolean(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyShowUpdateForm), this._showUpdateForm);
            String defUpdateFile = ConfigurationLoader.GetConfigurationString(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyDefaultUpdateFile));
            if (defUpdateFile != null)
            {
                defUpdateFile = context.Server.MapPath(defUpdateFile);
                if (File.Exists(defUpdateFile))
                {
                    using (StreamReader reader = new StreamReader(defUpdateFile))
                    {
                        this._defaultUpdate = reader.ReadToEnd();
                        reader.Close();
                    }
                }
            }

            //Then get the Protocol Processor to be used
            procNode = ConfigurationLoader.GetConfigurationNode(g, objNode, ConfigurationLoader.CreateConfigurationNode(g, ConfigurationLoader.PropertyProtocolProcessor));
            if (procNode != null)
            {
                Object temp = ConfigurationLoader.LoadObject(g, procNode);
                if (temp is ISparqlHttpProtocolProcessor)
                {
                    this._protocolProcessor = (ISparqlHttpProtocolProcessor)temp;
                }
                else
                {
                    throw new DotNetRdfConfigurationException("Unable to load SPARQL Server Configuration as the RDF configuration file specifies a value for the Handlers dnr:protocolProcessor property which cannot be loaded as an object which implements the ISparqlHttpProtocolProcessor interface");
                }
            }
        }

        /// <summary>
        /// Gets the SPARQL Query Processor
        /// </summary>
        public ISparqlQueryProcessor QueryProcessor
        {
            get
            {
                return this._queryProcessor;
            }
        }

        /// <summary>
        /// Gets the SPARQL Update Processor
        /// </summary>
        public ISparqlUpdateProcessor UpdateProcessor
        {
            get
            {
                return this._updateProcessor;
            }
        }

        /// <summary>
        /// Gets the SPARQL Uniform HTTP Protocol Processor
        /// </summary>
        public ISparqlHttpProtocolProcessor ProtocolProcessor
        {
            get
            {
                return this._protocolProcessor;
            }
        }
    }

    /// <summary>
    /// Concrete implementation of a Handler Configuration for SPARQL Servers
    /// </summary>
    public class SparqlServerConfiguration : BaseSparqlServerConfiguration
    {
        /// <summary>
        /// Creates a new SPARQL Server Configuration from information in a Configuration Graph
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <param name="g">Configuration Graph</param>
        /// <param name="objNode">Object Node</param>
        public SparqlServerConfiguration(HttpContext context, IGraph g, INode objNode)
            : base(context, g, objNode)
        {

        }
    }
}

#endif