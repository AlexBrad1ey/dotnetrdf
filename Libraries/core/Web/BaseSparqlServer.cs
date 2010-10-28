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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.UI;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Update;
using VDS.RDF.Update.Protocol;
using VDS.RDF.Web.Configuration.Server;
using VDS.RDF.Writing;

namespace VDS.RDF.Web
{
    /// <summary>
    /// Abstract Base class for SPARQL Servers which provide combined SPARQL Query, Update and Uniform HTTP Protocol endpoints
    /// </summary>
    public abstract class BaseSparqlServer : IHttpHandler
    {
        /// <summary>
        /// Handler Configuration
        /// </summary>
        protected BaseSparqlServerConfiguration _config;

        /// <summary>
        /// Returns that the Handler is reusable
        /// </summary>
        public bool IsReusable
        {
            get 
            {
                return true; 
            }
        }

        /// <summary>
        /// Processes requests
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public void ProcessRequest(HttpContext context)
        {
            String basePath;
            this._config = this.LoadConfig(context, out basePath);

            //Add our Standard Headers
            HandlerHelper.AddStandardHeaders(context);

            String path = context.Request.Path;
            if (path.StartsWith(basePath))
            {
                path = path.Substring(basePath.Length);
            }

            switch (path)
            {
                case "query":
                    this.ProcessQueryRequest(context);
                    break;
                case "update":
                    this.ProcessUpdateRequest(context);
                    break;
                case "description":
                    context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    break;
                default:
                    this.ProcessProtocolRequest(context);
                    break;
            }
        }

        /// <summary>
        /// Processes Query requests
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public void ProcessQueryRequest(HttpContext context)
        {
            if (this._config.QueryProcessor == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                return;
            }

            //See if there has been an query submitted
            String queryText = context.Request.QueryString["query"];
            if (queryText == null || queryText.Equals(String.Empty))
            {
                queryText = context.Request.Form["query"];
            }

            //If no Query sent either show Query Form or give a HTTP 400 response
            if (queryText == null || queryText.Equals(String.Empty))
            {
                if (this._config.ShowQueryForm)
                {
                    this.ShowQueryForm(context);
                    return;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
            }

            //Get Other options associated with this query
            List<String> userDefaultGraphs = new List<String>();
            List<String> userNamedGraphs = new List<String>();
            long timeout = 0;
            bool partialResults = this._config.DefaultPartialResults;

            //Get the Default Graph URIs (if any)
            if (context.Request.QueryString["default-graph-uri"] != null)
            {
                userDefaultGraphs.AddRange(context.Request.QueryString.GetValues("default-graph-uri"));
            }
            else if (context.Request.Form["default-graph-uri"] != null)
            {
                userDefaultGraphs.AddRange(context.Request.Form.GetValues("default-graph-uri"));
            }
            //Get the Named Graph URIs (if any)
            if (context.Request.QueryString["named-graph-uri"] != null)
            {
                userNamedGraphs.AddRange(context.Request.QueryString.GetValues("named-graph-uri"));
            }
            else if (context.Request.Form["named-graph-uri"] != null)
            {
                userNamedGraphs.AddRange(context.Request.Form.GetValues("named-graph-uri"));
            }

            //Get Timeout setting (if any)
            if (context.Request.QueryString["timeout"] != null)
            {
                if (!Int64.TryParse(context.Request.QueryString["timeout"], out timeout))
                {
                    timeout = this._config.DefaultTimeout;
                }
            }
            else if (context.Request.Form["timeout"] != null)
            {
                if (!Int64.TryParse(context.Request.Form["timeout"], out timeout))
                {
                    timeout = this._config.DefaultTimeout;
                }
            }
            //Get Partial Results Setting (if any);
            if (context.Request.QueryString["partialResults"] != null)
            {
                if (!Boolean.TryParse(context.Request.QueryString["partialResults"], out partialResults))
                {
                    partialResults = this._config.DefaultPartialResults;
                }
            }
            else if (context.Request.Form["partialResults"] != null)
            {
                if (!Boolean.TryParse(context.Request.Form["partialResults"], out partialResults))
                {
                    partialResults = this._config.DefaultPartialResults;
                }
            }

            try
            {
                //Now we're going to parse the Query
                SparqlQueryParser parser = new SparqlQueryParser();
                parser.ExpressionFactories = this._config.ExpressionFactories;
                SparqlQuery query = parser.ParseFromString(queryText);

                //Check whether we need to use authentication
                //If there are no user groups then no authentication is in use so we default to authenticated with no per-action authentication needed
                bool isAuth = true, requireActionAuth = false;
                if (this._config.UserGroups.Any())
                {
                    //If we have user
                    isAuth = HandlerHelper.IsAuthenticated(context, this._config.UserGroups);
                    requireActionAuth = true;
                }
                if (!isAuth) return;

                //Is this user allowed to make this kind of query?
                if (requireActionAuth) HandlerHelper.IsAuthenticated(context, this._config.UserGroups, this.GetQueryPermissionAction(query));

                //Set the Default Graph URIs (if any)
                if (userDefaultGraphs.Count > 0)
                {
                    //Default Graph Uri specified by default-graph-uri parameter or Web.config settings
                    foreach (String userDefaultGraph in userDefaultGraphs)
                    {
                        if (!userDefaultGraph.Equals(String.Empty))
                        {
                            query.AddDefaultGraph(new Uri(userDefaultGraph));
                        }
                    }
                }
                else if (!this._config.DefaultGraphURI.Equals(String.Empty))
                {
                    //Only applies if the Query doesn't specify any Default Graph
                    if (!query.DefaultGraphs.Any())
                    {
                        query.AddDefaultGraph(new Uri(this._config.DefaultGraphURI));
                    }
                }

                //Set the Named Graph URIs (if any)
                if (userNamedGraphs.Count > 0)
                {
                    foreach (String userNamedGraph in userNamedGraphs)
                    {
                        if (!userNamedGraph.Equals(String.Empty))
                        {
                            query.AddNamedGraph(new Uri(userNamedGraph));
                        }
                    }
                }

                //Set Timeout setting
                if (timeout > 0)
                {
                    query.Timeout = timeout;
                }
                else
                {
                    query.Timeout = this._config.DefaultTimeout;
                }

                //Set Partial Results Setting                 
                query.PartialResultsOnTimeout = partialResults;

                //Now we can finally make the query and return the results
                Object result = this.ProcessQuery(query);
                this.ProcessQueryResults(context, result);

                //Update the Cache as the request may have changed the endpoint
                this.UpdateConfig(context);
            }
            catch (RdfParseException parseEx)
            {
                HandleQueryErrors(context, "Parsing Error", queryText, parseEx, (int)HttpStatusCode.BadRequest);
            }
            catch (RdfQueryTimeoutException timeoutEx)
            {
                HandleQueryErrors(context, "Query Timeout Error", queryText, timeoutEx);
            }
            catch (RdfQueryException queryEx)
            {
                HandleQueryErrors(context, "Update Error", queryText, queryEx);
            }
            catch (RdfWriterSelectionException writerSelEx)
            {
                HandleQueryErrors(context, "Output Selection Error", queryText, writerSelEx, (int)HttpStatusCode.NotAcceptable);
            }
            catch (RdfException rdfEx)
            {
                HandleQueryErrors(context, "RDF Error", queryText, rdfEx);
            }
            catch (Exception ex)
            {
                HandleQueryErrors(context, "Error", queryText, ex);
            }
        }

        /// <summary>
        /// Processes Update requests
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public void ProcessUpdateRequest(HttpContext context)
        {
            if (this._config.UpdateProcessor == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                return;
            }

            //See if there has been an update submitted
            String updateText = context.Request.QueryString["update"];
            if (updateText == null || updateText.Equals(String.Empty))
            {
                updateText = context.Request.Form["update"];
            }

            //If no Update sent either show Update Form or give a HTTP 400 response
            if (updateText == null || updateText.Equals(String.Empty))
            {
                if (this._config.ShowUpdateForm)
                {
                    this.ShowUpdateForm(context);
                    return;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
            }

            //TODO: Does Update need to support default and named graphs URI parameters?

            try
            {
                //Now we're going to parse the Updates
                SparqlUpdateParser parser = new SparqlUpdateParser();
                parser.ExpressionFactories = this._config.ExpressionFactories;
                SparqlUpdateCommandSet commands = parser.ParseFromString(updateText);

                //Check whether we need to use authentication
                //If there are no user groups then no authentication is in use so we default to authenticated with no per-action authentication needed
                bool isAuth = true, requireActionAuth = false;
                if (this._config.UserGroups.Any())
                {
                    //If we have user
                    isAuth = HandlerHelper.IsAuthenticated(context, this._config.UserGroups);
                    requireActionAuth = true;
                }
                if (!isAuth) return;

                //Then process each action
                foreach (SparqlUpdateCommand cmd in commands.Commands)
                {
                    //Authenticate each action
                    bool actionAuth = true;
                    if (requireActionAuth) actionAuth = HandlerHelper.IsAuthenticated(context, this._config.UserGroups, this.GetUpdatePermissionAction(cmd));
                    if (actionAuth)
                    {
                        //Action is permitted so we go ahead and execute it
                        try
                        {
                            this.ProcessUpdate(cmd);
                        }
                        catch
                        {
                            //If halting on errors (default behaviour) throw the error otherwise ignore and continue
                            if (this._config.HaltOnError) throw;
                        }
                    }
                }

                //Flush outstanding changes
                this._config.UpdateProcessor.Flush();

                //Update the Cache as the request may have changed the endpoint
                this.UpdateConfig(context);
            }
            catch (RdfParseException parseEx)
            {
                HandleUpdateErrors(context, "Parsing Error", updateText, parseEx);
            }
            catch (SparqlUpdateException updateEx)
            {
                HandleUpdateErrors(context, "Update Error", updateText, updateEx);
            }
            catch (RdfException rdfEx)
            {
                HandleUpdateErrors(context, "RDF Error", updateText, rdfEx);
            }
            catch (Exception ex)
            {
                HandleUpdateErrors(context, "Error", updateText, ex);
            }
        }

        /// <summary>
        /// Processes Protocol requests
        /// </summary>
        /// <param name="context">HTTP Context</param>
        public void ProcessProtocolRequest(HttpContext context)
        {
            if (this._config.ProtocolProcessor == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                return;
            }

            //Check whether we need to use authentication
            if (!HandlerHelper.IsAuthenticated(context, this._config.UserGroups, context.Request.HttpMethod)) return;

            try
            {
                //Invoke the appropriate method on our protocol processor
                switch (context.Request.HttpMethod)
                {
                    case "GET":
                        this._config.ProtocolProcessor.ProcessGet(context);
                        break;
                    case "PUT":
                        this._config.ProtocolProcessor.ProcessPut(context);
                        break;
                    case "POST":
                        this._config.ProtocolProcessor.ProcessPost(context);
                        break;
                    case "DELETE":
                        this._config.ProtocolProcessor.ProcessDelete(context);
                        break;
                    default:
                        //For any other HTTP Verb we send a 405 Method Not Allowed
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        break;
                }

                //Update the Cache as the request may have changed the endpoint
                this.UpdateConfig(context);
            }
            catch (SparqlHttpProtocolUriResolutionException)
            {
                //If URI Resolution fails we send a 400 Bad Request
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            catch (SparqlHttpProtocolUriInvalidException)
            {
                //If URI is invalid we send a 400 Bad Request
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            catch (NotSupportedException)
            {
                //If Not Supported we send a 405 Method Not Allowed
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            catch (NotImplementedException)
            {
                //If Not Implemented we send a 501 Not Implemented
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            }
            catch (RdfWriterSelectionException)
            {
                //If we can't select a valid Writer when returning content we send a 406 Not Acceptable
                context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
            }
            catch (RdfParserSelectionException)
            {
                //If we can't select a valid Parser when receiving content we send a 415 Unsupported Media Type
                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
            }
            catch (RdfParseException)
            {
                //If we can't parse the received content successfully we send a 400 Bad Request
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            catch (Exception)
            {
                //For any other error we'll send a 500 Internal Server Error
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        #region Configuration Loading

        /// <summary>
        /// Loads the Handler Configuration
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <param name="basePath">Base Path of the Handler</param>
        /// <returns></returns>
        protected abstract BaseSparqlServerConfiguration LoadConfig(HttpContext context, out String basePath);

        /// <summary>
        /// Updates the Handler Configuration
        /// </summary>
        /// <param name="context">HTTP Context</param>
        protected virtual void UpdateConfig(HttpContext context)
        {

        }

        #endregion

        #region Operation Processing

        /// <summary>
        /// Processes SPARQL Queries
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// Implementations should override this method if their behaviour requires more than just invoking the configured Query processor
        /// </para>
        /// </remarks>
        protected virtual Object ProcessQuery(SparqlQuery query)
        {
            return this._config.QueryProcessor.ProcessQuery(query);
        }

        /// <summary>
        /// Processes SPARQL Updates
        /// </summary>
        /// <param name="cmd">Update Command</param>
        /// <remarks>
        /// <para>
        /// Implementations should override this method if their behaviour requires more than just invoking the configured Update processor
        /// </para>
        /// </remarks>
        protected virtual void ProcessUpdate(SparqlUpdateCommand cmd)
        {
            this._config.UpdateProcessor.ProcessCommand(cmd);
        }

        /// <summary>
        /// Internal Helper function which returns the Results back to the Client in one of their accepted formats
        /// </summary>
        /// <param name="context">Context of the HTTP Request</param>
        /// <param name="result">Results of the Sparql Query</param>
        protected void ProcessQueryResults(HttpContext context, Object result)
        {
            //Return the Results
            String ctype;
            if (result is SparqlResultSet)
            {
                //Get the appropriate Writer and set the Content Type
                ISparqlResultsWriter sparqlwriter;
                if (context.Request.AcceptTypes != null)
                {
                    sparqlwriter = MimeTypesHelper.GetSparqlWriter(context.Request.AcceptTypes, out ctype);
                }
                else
                {
                    //Default to SPARQL XML Results Format if no accept header
                    sparqlwriter = new SparqlXmlWriter();
                    ctype = MimeTypesHelper.Sparql[0];
                }
                context.Response.ContentType = ctype;
                if (sparqlwriter is IHtmlWriter)
                {
                    ((IHtmlWriter)sparqlwriter).Stylesheet = this._config.Stylesheet;
                }

                //Clear any existing Response
                context.Response.Clear();

                //Send Result Set to Client
                sparqlwriter.Save((SparqlResultSet)result, new StreamWriter(context.Response.OutputStream));
            }
            else if (result is Graph)
            {
                //Get the appropriate Writer and set the Content Type
                IRdfWriter rdfwriter = MimeTypesHelper.GetWriter(context.Request.AcceptTypes, out ctype);
                context.Response.ContentType = ctype;
                if (rdfwriter is IHtmlWriter)
                {
                    ((IHtmlWriter)rdfwriter).Stylesheet = this._config.Stylesheet;
                }

                //Clear any existing Response
                context.Response.Clear();

                //Send Graph to Client
                rdfwriter.Save((Graph)result, new StreamWriter(context.Response.OutputStream));
            }
            else
            {
                throw new RdfQueryException("Unexpected Query Result Object of Type '" + result.GetType().ToString() + "' returned");
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Handles errors in processing SPARQL Query Requests
        /// </summary>
        /// <param name="context">Context of the HTTP Request</param>
        /// <param name="title">Error title</param>
        /// <param name="query">Sparql Query</param>
        /// <param name="ex">Error</param>
        protected virtual void HandleQueryErrors(HttpContext context, String title, String query, Exception ex)
        {
            HandlerHelper.HandleQueryErrors(context, this._config, title, query, ex);
        }

        /// <summary>
        /// Handles errors in processing SPARQL Query Requests
        /// </summary>
        /// <param name="context">Context of the HTTP Request</param>
        /// <param name="title">Error title</param>
        /// <param name="query">Sparql Query</param>
        /// <param name="ex">Error</param>
        /// <param name="statusCode">HTTP Status Code to return</param>
        protected virtual void HandleQueryErrors(HttpContext context, String title, String query, Exception ex, int statusCode)
        {
            HandlerHelper.HandleQueryErrors(context, this._config, title, query, ex, statusCode);
        }

        /// <summary>
        /// Handles errors in processing SPARQL Update Requests
        /// </summary>
        /// <param name="context">Context of the HTTP Request</param>
        /// <param name="title">Error title</param>
        /// <param name="update">SPARQL Update</param>
        /// <param name="ex">Error</param>
        protected virtual void HandleUpdateErrors(HttpContext context, String title, String update, Exception ex)
        {
            HandlerHelper.HandleUpdateErrors(context, this._config, title, update, ex);
        }

        #endregion

        #region Forms

        /// <summary>
        /// Generates a SPARQL Query Form
        /// </summary>
        /// <param name="context">HTTP Context</param>
        protected virtual void ShowQueryForm(HttpContext context)
        {
            //Set Content Type
            context.Response.Clear();
            context.Response.ContentType = "text/html";

            //Get a HTML Text Writer
            HtmlTextWriter output = new HtmlTextWriter(new StreamWriter(context.Response.OutputStream));

            //Page Header
            output.Write("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            output.RenderBeginTag(HtmlTextWriterTag.Html);
            output.RenderBeginTag(HtmlTextWriterTag.Head);
            output.RenderBeginTag(HtmlTextWriterTag.Title);
            output.WriteEncodedText("SPARQL Query Interface");
            output.RenderEndTag();
            //Add Stylesheet
            if (!this._config.Stylesheet.Equals(String.Empty))
            {
                output.AddAttribute(HtmlTextWriterAttribute.Href, this._config.Stylesheet);
                output.AddAttribute(HtmlTextWriterAttribute.Type, "text/css");
                output.AddAttribute(HtmlTextWriterAttribute.Rel, "stylesheet");
                output.RenderBeginTag(HtmlTextWriterTag.Link);
                output.RenderEndTag();
            }
            output.RenderEndTag();


            //Header Text
            output.RenderBeginTag(HtmlTextWriterTag.Body);
            output.RenderBeginTag(HtmlTextWriterTag.H3);
            output.WriteEncodedText("SPARQL Query Interface");
            output.RenderEndTag();

            //Query Form
            output.AddAttribute(HtmlTextWriterAttribute.Name, "sparqlQuery");
            output.AddAttribute("method", "get");
            output.AddAttribute("action", context.Request.Path);
            output.RenderBeginTag(HtmlTextWriterTag.Form);

            if (!this._config.IntroductionText.Equals(String.Empty))
            {
                output.RenderBeginTag(HtmlTextWriterTag.P);
                output.Write(this._config.IntroductionText);
                output.RenderEndTag();
            }

            output.WriteEncodedText("Query");
            output.WriteBreak();
            output.AddAttribute(HtmlTextWriterAttribute.Name, "query");
            output.AddAttribute(HtmlTextWriterAttribute.Rows, "15");
            output.AddAttribute(HtmlTextWriterAttribute.Cols, "100");
            output.RenderBeginTag(HtmlTextWriterTag.Textarea);
            output.WriteEncodedText(this._config.DefaultQuery);
            output.RenderEndTag();
            output.WriteBreak();

            output.WriteEncodedText("Default Graph URI: ");
            output.AddAttribute(HtmlTextWriterAttribute.Name, "default-graph-uri");
            output.AddAttribute(HtmlTextWriterAttribute.Type, "text");
            output.AddAttribute(HtmlTextWriterAttribute.Size, "100");
            output.AddAttribute(HtmlTextWriterAttribute.Value, this._config.DefaultGraphURI);
            output.RenderBeginTag(HtmlTextWriterTag.Input);
            output.RenderEndTag();
            output.WriteBreak();

            if (this._config.SupportsTimeout)
            {
                output.WriteEncodedText("Timeout: ");
                output.AddAttribute(HtmlTextWriterAttribute.Name, "timeout");
                output.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                output.AddAttribute(HtmlTextWriterAttribute.Value, this._config.DefaultTimeout.ToString());
                output.RenderBeginTag(HtmlTextWriterTag.Input);
                output.RenderEndTag();
                output.WriteEncodedText(" Milliseconds");
                output.WriteBreak();
            }

            if (this._config.SupportsPartialResults)
            {
                output.AddAttribute(HtmlTextWriterAttribute.Name, "partialResults");
                output.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");
                if (this._config.DefaultPartialResults) output.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
                output.AddAttribute(HtmlTextWriterAttribute.Value, "true");
                output.RenderBeginTag(HtmlTextWriterTag.Input);
                output.RenderEndTag();
                output.WriteEncodedText(" Partial Results on Timeout?");
                output.WriteBreak();
            }

            output.AddAttribute(HtmlTextWriterAttribute.Type, "submit");
            output.AddAttribute(HtmlTextWriterAttribute.Value, "Make Query");
            output.RenderBeginTag(HtmlTextWriterTag.Input);
            output.RenderEndTag();

            output.RenderEndTag(); //End Form

            //End of Page
            output.RenderEndTag(); //End Body
            output.RenderEndTag(); //End Html

            output.Flush();
        }

        /// <summary>
        /// Generates a SPARQL Update Form
        /// </summary>
        /// <param name="context">HTTP Context</param>
        protected virtual void ShowUpdateForm(HttpContext context)
        {
            //Set Content Type
            context.Response.Clear();
            context.Response.ContentType = "text/html";

            //Get a HTML Text Writer
            HtmlTextWriter output = new HtmlTextWriter(new StreamWriter(context.Response.OutputStream));

            //Page Header
            output.Write("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            output.RenderBeginTag(HtmlTextWriterTag.Html);
            output.RenderBeginTag(HtmlTextWriterTag.Head);
            output.RenderBeginTag(HtmlTextWriterTag.Title);
            output.WriteEncodedText("SPARQL Update Interface");
            output.RenderEndTag();
            //Add Stylesheet
            if (!this._config.Stylesheet.Equals(String.Empty))
            {
                output.AddAttribute(HtmlTextWriterAttribute.Href, this._config.Stylesheet);
                output.AddAttribute(HtmlTextWriterAttribute.Type, "text/css");
                output.AddAttribute(HtmlTextWriterAttribute.Rel, "stylesheet");
                output.RenderBeginTag(HtmlTextWriterTag.Link);
                output.RenderEndTag();
            }
            output.RenderEndTag();


            //Header Text
            output.RenderBeginTag(HtmlTextWriterTag.Body);
            output.RenderBeginTag(HtmlTextWriterTag.H3);
            output.WriteEncodedText("SPARQL Update Interface");
            output.RenderEndTag();

            //Query Form
            output.AddAttribute(HtmlTextWriterAttribute.Name, "sparqlUpdate");
            output.AddAttribute("method", "get");
            output.AddAttribute("action", context.Request.Path);
            output.RenderBeginTag(HtmlTextWriterTag.Form);

            if (!this._config.IntroductionText.Equals(String.Empty))
            {
                output.RenderBeginTag(HtmlTextWriterTag.P);
                output.Write(this._config.IntroductionText);
                output.RenderEndTag();
            }

            output.WriteEncodedText("Update");
            output.WriteBreak();
            output.AddAttribute(HtmlTextWriterAttribute.Name, "update");
            output.AddAttribute(HtmlTextWriterAttribute.Rows, "15");
            output.AddAttribute(HtmlTextWriterAttribute.Cols, "100");
            output.RenderBeginTag(HtmlTextWriterTag.Textarea);
            output.WriteEncodedText(this._config.DefaultUpdate);
            output.RenderEndTag();
            output.WriteBreak();

            //output.WriteEncodedText("Default Graph URI: ");
            //output.AddAttribute(HtmlTextWriterAttribute.Name, "default-graph-uri");
            //output.AddAttribute(HtmlTextWriterAttribute.Type, "text");
            //output.AddAttribute(HtmlTextWriterAttribute.Size, "100");
            //output.AddAttribute(HtmlTextWriterAttribute.Value, this._config.DefaultGraphURI);
            //output.RenderBeginTag(HtmlTextWriterTag.Input);
            //output.RenderEndTag();
            //output.WriteBreak();

            output.AddAttribute(HtmlTextWriterAttribute.Type, "submit");
            output.AddAttribute(HtmlTextWriterAttribute.Value, "Perform Update");
            output.RenderBeginTag(HtmlTextWriterTag.Input);
            output.RenderEndTag();

            output.RenderEndTag(); //End Form

            //End of Page
            output.RenderEndTag(); //End Body
            output.RenderEndTag(); //End Html

            output.Flush();
        }

        #endregion

        #region Permission Detection

        /// <summary>
        /// Gets the Permission action for a SPARQL Query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns></returns>
        private String GetQueryPermissionAction(SparqlQuery query)
        {
            switch (query.QueryType)
            {
                case SparqlQueryType.Ask:
                    return "ASK";
                case SparqlQueryType.Construct:
                    return "CONSTRUCT";
                case SparqlQueryType.Describe:
                case SparqlQueryType.DescribeAll:
                    return "DESCRIBE";
                case SparqlQueryType.Select:
                case SparqlQueryType.SelectAll:
                case SparqlQueryType.SelectAllDistinct:
                case SparqlQueryType.SelectAllReduced:
                case SparqlQueryType.SelectDistinct:
                case SparqlQueryType.SelectReduced:
                    return "SELECT";
                default:
                    return String.Empty;
            }
        }

        /// <summary>
        /// Gets the Permission action for a SPARQL Update Command
        /// </summary>
        /// <param name="cmd">Update Command</param>
        /// <returns></returns>
        private String GetUpdatePermissionAction(SparqlUpdateCommand cmd)
        {
            switch (cmd.CommandType)
            {
                case SparqlUpdateCommandType.InsertData:
                    return "INSERT DATA";
                case SparqlUpdateCommandType.DeleteData:
                    return "DELETE DATA";
                case SparqlUpdateCommandType.Insert:
                    return "INSERT";
                case SparqlUpdateCommandType.Delete:
                    return "DELETE";
                case SparqlUpdateCommandType.Modify:
                    return "MODIFY";
                case SparqlUpdateCommandType.Load:
                    return "LOAD";
                case SparqlUpdateCommandType.Clear:
                    return "CLEAR";
                case SparqlUpdateCommandType.Create:
                    return "CREATE";
                case SparqlUpdateCommandType.Drop:
                    return "DROP";
                default:
                    return String.Empty;
            }
        }

        #endregion

    }
}

#endif