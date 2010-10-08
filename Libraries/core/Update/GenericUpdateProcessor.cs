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

#if !NO_STORAGE

using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Construct;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Storage;
using VDS.RDF.Update.Commands;

namespace VDS.RDF.Update
{
    /// <summary>
    /// SPARQL Update Processor which processes commands against a generic underlying store represented by an <see cref="IGenericIOManager">IGenericIOManager</see> implementation
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the provided manager also implements the <see cref="IUpdateableGenericIOManager">IUpdateableGenericIOManager</see> interface then the managers native SPARQL Update implementation will be used for the non-type specific calls i.e. <see cref="GenericUpdateProcessor.ProcessCommand">ProcessCommand()</see> and <see cref="GenericUpdateProcessor.ProcessCommandSet">ProcessCommandSet()</see>.  At all other times the SPARQL Update commands will be processed by approximating their behaviour through calls to <see cref="IGenericIOManager.SaveGraph">SaveGraph()</see>, <see cref="IGenericIOManager.LoadGraph">LoadGraph()</see> and <see cref="IGenericIOManager.UpdateGraph">UpdateGraph()</see> in addition to local in-memory manipulation of the data.  Some commands such as INSERT and DELETE can only be processed when the manager is also a <see cref="IQueryableGenericIOManager">IQueryableGenericIOManager</see> since they rely on making a query and performing actions based on the results of that query.
    /// </para>
    /// <para>
    /// The performance of this processor is somewhat dependent on the underlying <see cref="IGenericIOManager">IGenericIOManager</see>.  If the underlying manager supports triple level updates as indicated by the <see cref="IGenericIOManager.UpdateSupported">UpdateSupported</see> property then operations can be performed quite efficiently, if this is not the case then any operation which modifies a Graph will need to load the existing Graph from the store, make the modifications locally in-memory and then save the resulting Graph back to the Store
    /// </para>
    /// </remarks>
    public class GenericUpdateProcessor : ISparqlUpdateProcessor
    {
        private IGenericIOManager _manager;

        /// <summary>
        /// Creates a new Generic Update Processor
        /// </summary>
        /// <param name="manager">Generic IO Manager</param>
        public GenericUpdateProcessor(IGenericIOManager manager)
        {
            if (manager.IsReadOnly) throw new ArgumentException("Cannot create a GenericUpdateProcessor for a store which is read-only", "manager");
            this._manager = manager;
        }

        /// <summary>
        /// Flushes any outstanding changes to the underlying store
        /// </summary>
        public virtual void Flush()
        {
            //Does Nothing
        }

        /// <summary>
        /// Processes a CLEAR command
        /// </summary>
        /// <param name="cmd">Clear Command</param>
        /// <remarks>
        /// Implemented by replacing the Graph with an empty Graph
        /// </remarks>
        public void ProcessClearCommand(ClearCommand cmd)
        {
            Graph g = new Graph();
            g.BaseUri = cmd.TargetUri;
            this._manager.SaveGraph(g);
        }

        /// <summary>
        /// Processes a CREATE command
        /// </summary>
        /// <param name="cmd">Create Command</param>
        /// <remarks>
        /// <para>
        /// Implemented by adding an empty Graph to the Store
        /// </para>
        /// <para>
        /// <strong>Warning:</strong> As the <see cref="IGenericIOManager">IGenericIOManager</see> interface does not allow checking whether a Graph exists processing CREATE commands can result in overwriting existing Graphs
        /// </para>
        /// </remarks>
        public void ProcessCreateCommand(CreateCommand cmd)
        {
            Graph g = new Graph();
            g.BaseUri = cmd.TargetUri;
            try
            {
                this._manager.SaveGraph(g);
            }
            catch
            {
                if (!cmd.Silent) throw;
            }
        }

        /// <summary>
        /// Processes a command
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <remarks>
        /// <para>
        /// If the provided manager also implements the <see cref="IUpdateableGenericIOManager">IUpdateableGenericIOManager</see> interface then the managers native SPARQL Update implementation will be used.
        /// </para>
        /// </remarks>
        public virtual void ProcessCommand(SparqlUpdateCommand cmd)
        {
            if (this._manager is IUpdateableGenericIOManager)
            {
                ((IUpdateableGenericIOManager)this._manager).Update(cmd.ToString());
            }
            else
            {
                switch (cmd.CommandType)
                {
                    case SparqlUpdateCommandType.Clear:
                        this.ProcessClearCommand((ClearCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.Create:
                        this.ProcessCreateCommand((CreateCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.Delete:
                        this.ProcessDeleteCommand((DeleteCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.DeleteData:
                        this.ProcessDeleteDataCommand((DeleteDataCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.Drop:
                        this.ProcessDropCommand((DropCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.Insert:
                        this.ProcessInsertCommand((InsertCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.InsertData:
                        this.ProcessInsertDataCommand((InsertDataCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.Load:
                        this.ProcessLoadCommand((LoadCommand)cmd);
                        break;
                    case SparqlUpdateCommandType.Modify:
                        this.ProcessModifyCommand((ModifyCommand)cmd);
                        break;
                    default:
                        throw new SparqlUpdateException("Unknown Update Commands cannot be processed by the Leviathan Update Processor");
                }
            }
        }

        /// <summary>
        /// Processes a command set
        /// </summary>
        /// <param name="commands">Command Set</param>
        /// <remarks>
        /// <para>
        /// If the provided manager also implements the <see cref="IUpdateableGenericIOManager">IUpdateableGenericIOManager</see> interface then the managers native SPARQL Update implementation will be used.
        /// </para>
        /// </remarks>
        public virtual void ProcessCommandSet(SparqlUpdateCommandSet commands)
        {
            for (int i = 0; i < commands.CommandCount; i++)
            {
                this.ProcessCommand(commands[i]);
            }
        }

        /// <summary>
        /// Processes a DELETE command
        /// </summary>
        /// <param name="cmd">Delete Command</param>
        /// <remarks>
        /// <para>
        /// <strong>Note:</strong> The underlying manager must implement the <see cref="IQueryableGenericIOManager">IQueryableGenericIOManager</see> interface in order for DELETE commands to be processed
        /// </para>
        /// </remarks>
        public void ProcessDeleteCommand(DeleteCommand cmd)
        {
            if (this._manager is IQueryableGenericIOManager)
            {
                //First build and make the query to get a Result Set
                String queryText = "SELECT * WHERE " + cmd.WherePattern.ToString();
                SparqlQueryParser parser = new SparqlQueryParser();
                SparqlQuery query = parser.ParseFromString(queryText);
                foreach (Uri u in cmd.UsingUris)
                {
                    query.AddDefaultGraph(u);
                }

                Object results = ((IQueryableGenericIOManager)this._manager).Query(query.ToString());
                if (results is SparqlResultSet)
                {
                    //Now need to transform the Result Set back to a Multiset
                    Multiset mset = new Multiset((SparqlResultSet)results);

                    //Generate the Triples for each Solution
                    List<Triple> deletedTriples = new List<Triple>();
                    Dictionary<String, List<Triple>> deletedGraphTriples = new Dictionary<string, List<Triple>>();
                    foreach (Set s in mset.Sets)
                    {
                        List<Triple> tempDeletedTriples = new List<Triple>();
                        try
                        {

                            ConstructContext context = new ConstructContext(null, s, true);
                            foreach (ITriplePattern p in cmd.DeletePattern.TriplePatterns)
                            {
                                tempDeletedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                            }
                            deletedTriples.AddRange(tempDeletedTriples);
                        }
                        catch (RdfQueryException)
                        {
                            //If we throw an error this means we couldn't construct for this solution so the
                            //solution is discarded
                            continue;
                        }

                        //Triples from GRAPH clauses
                        foreach (GraphPattern gp in cmd.DeletePattern.ChildGraphPatterns)
                        {
                            tempDeletedTriples.Clear();
                            try
                            {
                                String graphUri;
                                switch (gp.GraphSpecifier.TokenType)
                                {
                                    case Token.URI:
                                        graphUri = gp.GraphSpecifier.Value;
                                        break;
                                    case Token.VARIABLE:
                                        if (s.ContainsVariable(gp.GraphSpecifier.Value))
                                        {
                                            INode temp = s[gp.GraphSpecifier.Value.Substring(1)];
                                            if (temp.NodeType == NodeType.Uri)
                                            {
                                                graphUri = temp.ToSafeString();
                                            }
                                            else
                                            {
                                                //If the Variable is not bound to a URI then skip
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            //If the Variable is not bound for this solution then skip
                                            continue;
                                        }
                                        break;
                                    default:
                                        //Any other Graph Specifier we have to ignore this solution
                                        continue;
                                }
                                if (!deletedGraphTriples.ContainsKey(graphUri)) deletedGraphTriples.Add(graphUri, new List<Triple>());
                                ConstructContext context = new ConstructContext(null, s, true);
                                foreach (ITriplePattern p in gp.TriplePatterns)
                                {
                                    tempDeletedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                                }
                                deletedGraphTriples[graphUri].AddRange(tempDeletedTriples);
                            }
                            catch (RdfQueryException)
                            {
                                //If we throw an error this means we couldn't construct for this solution so the
                                //solution is discarded
                                continue;
                            }
                        }
                    }

                    //Now decide how to apply the update
                    if (this._manager.UpdateSupported)
                    {
                        this._manager.UpdateGraph(cmd.GraphUri, Enumerable.Empty<Triple>(), deletedTriples);
                        foreach (KeyValuePair<String, List<Triple>> graphDeletion in deletedGraphTriples)
                        {
                            this._manager.UpdateGraph(graphDeletion.Key, Enumerable.Empty<Triple>(), graphDeletion.Value);
                        }
                    }
                    else
                    {
                        Graph g = new Graph();
                        this._manager.LoadGraph(g, cmd.GraphUri);
                        g.Retract(deletedTriples);
                        this._manager.SaveGraph(g);

                        foreach (KeyValuePair<String, List<Triple>> graphDeletion in deletedGraphTriples)
                        {
                            g = new Graph();
                            this._manager.LoadGraph(g, graphDeletion.Key);
                            g.Retract(graphDeletion.Value);
                            this._manager.SaveGraph(g);
                        }
                    }
                }
                else
                {
                    throw new SparqlUpdateException("Cannot evaluate an DELETE Command as the underlying Store failed to answer the query for the WHERE portion of the command as expected");
                }
            }
            else
            {
                throw new NotSupportedException("DELETE commands are not supported by this Update Processor as the manager for the underlying Store does not provide Query capabilities which are necessary to process this command");
            }
        }

        /// <summary>
        /// Processes a DELETE DATA command
        /// </summary>
        /// <param name="cmd">DELETE Data Command</param>
        public void ProcessDeleteDataCommand(DeleteDataCommand cmd)
        {
            if (!cmd.DataPattern.TriplePatterns.All(p => p is IConstructTriplePattern && ((IConstructTriplePattern)p).HasNoExplicitVariables)) throw new SparqlUpdateException("Cannot evaluate a DELETE DATA command where any of the Triple Patterns are not concrete triples - variables are not permitted");

            Uri graphUri = null;
            if (cmd.DataPattern.IsGraph)
            {
                switch (cmd.DataPattern.GraphSpecifier.TokenType)
                {
                    case Token.QNAME:
                        throw new NotImplementedException("Graph Specifiers as QNames for DELETE DATA Commands are not supported - please specify an absolute URI instead");
                    case Token.URI:
                        graphUri = new Uri(cmd.DataPattern.GraphSpecifier.Value);
                        break;
                    default:
                        throw new SparqlUpdateException("Cannot evaluate an DELETE DATA Command as the Graph Specifier is not a QName/URI");
                }
            }

            Graph g = new Graph();
            if (!this._manager.UpdateSupported) this._manager.LoadGraph(g, graphUri);

            //Delete the actual Triples
            INode subj, pred, obj;
            ConstructContext context = new ConstructContext(g, null, true);
            foreach (IConstructTriplePattern p in cmd.DataPattern.TriplePatterns)
            {
                subj = p.Subject.Construct(context);//((NodeMatchPattern)tp.Subject).Node.CopyNode(g);
                pred = p.Predicate.Construct(context);//((NodeMatchPattern)tp.Predicate).Node.CopyNode(g);
                obj = p.Object.Construct(context);//((NodeMatchPattern)tp.Object).Node.CopyNode(g);

                g.Retract(new Triple(subj, pred, obj));
            }

            if (this._manager.UpdateSupported)
            {
                this._manager.UpdateGraph(graphUri, Enumerable.Empty<Triple>(), g.Triples);
            }
            else
            {
                this._manager.SaveGraph(g);
            }
        }

        /// <summary>
        /// Processes a DROP command
        /// </summary>
        /// <param name="cmd">Drop Command</param>
        public void ProcessDropCommand(DropCommand cmd)
        {
            Graph g = new Graph();
            g.BaseUri = cmd.TargetUri;
            try
            {
                this._manager.SaveGraph(g);
            }
            catch
            {
                if (!cmd.Silent) throw;
            }
        }

        /// <summary>
        /// Processes an INSERT command
        /// </summary>
        /// <param name="cmd">Insert Command</param>
        /// <remarks>
        /// <para>
        /// <strong>Note:</strong> The underlying manager must implement the <see cref="IQueryableGenericIOManager">IQueryableGenericIOManager</see> interface in order for INSERT commands to be processed
        /// </para>
        /// </remarks>
        public void ProcessInsertCommand(InsertCommand cmd)
        {
            if (this._manager is IQueryableGenericIOManager)
            {
                //First build and make the query to get a Result Set
                String queryText = "SELECT * WHERE " + cmd.WherePattern.ToString();
                SparqlQueryParser parser = new SparqlQueryParser();
                SparqlQuery query = parser.ParseFromString(queryText);
                foreach (Uri u in cmd.UsingUris)
                {
                    query.AddDefaultGraph(u);
                }

                Object results = ((IQueryableGenericIOManager)this._manager).Query(query.ToString());
                if (results is SparqlResultSet)
                {
                    //Now need to transform the Result Set back to a Multiset
                    Multiset mset = new Multiset((SparqlResultSet)results);

                    //Generate the Triples for each Solution
                    List<Triple> insertedTriples = new List<Triple>();
                    Dictionary<String, List<Triple>> insertedGraphTriples = new Dictionary<string, List<Triple>>();
                    foreach (Set s in mset.Sets)
                    {
                        List<Triple> tempInsertedTriples = new List<Triple>();
                        try
                        {
                            ConstructContext context = new ConstructContext(null, s, true);
                            foreach (ITriplePattern p in cmd.InsertPattern.TriplePatterns)
                            {
                                tempInsertedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                            }
                            insertedTriples.AddRange(tempInsertedTriples);
                        }
                        catch (RdfQueryException)
                        {
                            //If we throw an error this means we couldn't construct for this solution so the
                            //solution is discarded
                            continue;
                        }

                        //Triples from GRAPH clauses
                        foreach (GraphPattern gp in cmd.InsertPattern.ChildGraphPatterns)
                        {
                            tempInsertedTriples.Clear();
                            try
                            {
                                String graphUri;
                                switch (gp.GraphSpecifier.TokenType)
                                {
                                    case Token.URI:
                                        graphUri = gp.GraphSpecifier.Value;
                                        break;
                                    case Token.VARIABLE:
                                        if (s.ContainsVariable(gp.GraphSpecifier.Value))
                                        {
                                            INode temp = s[gp.GraphSpecifier.Value.Substring(1)];
                                            if (temp.NodeType == NodeType.Uri)
                                            {
                                                graphUri = temp.ToSafeString();
                                            }
                                            else
                                            {
                                                //If the Variable is not bound to a URI then skip
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            //If the Variable is not bound for this solution then skip
                                            continue;
                                        }
                                        break;
                                    default:
                                        //Any other Graph Specifier we have to ignore this solution
                                        continue;
                                }
                                if (!insertedGraphTriples.ContainsKey(graphUri)) insertedGraphTriples.Add(graphUri, new List<Triple>());
                                ConstructContext context = new ConstructContext(null, s, true);
                                foreach (ITriplePattern p in gp.TriplePatterns)
                                {
                                    tempInsertedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                                }
                                insertedGraphTriples[graphUri].AddRange(tempInsertedTriples);
                            }
                            catch (RdfQueryException)
                            {
                                //If we throw an error this means we couldn't construct for this solution so the
                                //solution is discarded
                                continue;
                            }
                        }
                    }

                    //Now decide how to apply the update
                    if (this._manager.UpdateSupported)
                    {
                        this._manager.UpdateGraph(cmd.GraphUri, insertedTriples, Enumerable.Empty<Triple>());
                        foreach (KeyValuePair<String, List<Triple>> graphInsertion in insertedGraphTriples)
                        {
                            this._manager.UpdateGraph(graphInsertion.Key, graphInsertion.Value, Enumerable.Empty<Triple>());
                        }
                    }
                    else
                    {
                        Graph g = new Graph();
                        this._manager.LoadGraph(g, cmd.GraphUri);
                        g.Assert(insertedTriples);
                        this._manager.SaveGraph(g);

                        foreach (KeyValuePair<String, List<Triple>> graphInsertion in insertedGraphTriples)
                        {
                            g = new Graph();
                            this._manager.LoadGraph(g, graphInsertion.Key);
                            g.Assert(graphInsertion.Value);
                            this._manager.SaveGraph(g);
                        }
                    }
                }
                else
                {
                    throw new SparqlUpdateException("Cannot evaluate an INSERT Command as the underlying Store failed to answer the query for the WHERE portion of the command as expected");
                }
            }
            else
            {
                throw new NotSupportedException("INSERT commands are not supported by this Update Processor as the manager for the underlying Store does not provide Query capabilities which are necessary to process this command");
            }
        }

        /// <summary>
        /// Processes an INSERT DATA command
        /// </summary>
        /// <param name="cmd">Insert Data Command</param>
        public void ProcessInsertDataCommand(InsertDataCommand cmd)
        {
            if (!cmd.DataPattern.TriplePatterns.All(p => p is IConstructTriplePattern && ((IConstructTriplePattern)p).HasNoExplicitVariables)) throw new SparqlUpdateException("Cannot evaluate a INSERT DATA command where any of the Triple Patterns are not concrete triples - variables are not permitted");

            Uri graphUri = null;
            if (cmd.DataPattern.IsGraph)
            {
                switch (cmd.DataPattern.GraphSpecifier.TokenType)
                {
                    case Token.QNAME:
                        throw new NotImplementedException("Graph Specifiers as QNames for INSERT DATA Commands are not supported - please specify an absolute URI instead");
                    case Token.URI:
                        graphUri = new Uri(cmd.DataPattern.GraphSpecifier.Value);
                        break;
                    default:
                        throw new SparqlUpdateException("Cannot evaluate an INSERT DATA Command as the Graph Specifier is not a QName/URI");
                }
            }

            Graph g = new Graph();
            if (!this._manager.UpdateSupported) this._manager.LoadGraph(g, graphUri);

            //Insert the actual Triples
            INode subj, pred, obj;
            ConstructContext context = new ConstructContext(g, null, true);
            foreach (IConstructTriplePattern p in cmd.DataPattern.TriplePatterns.OfType<IConstructTriplePattern>())
            {
                subj = p.Subject.Construct(context);//((NodeMatchPattern)tp.Subject).Node.CopyNode(target);
                pred = p.Predicate.Construct(context);//((NodeMatchPattern)tp.Predicate).Node.CopyNode(target);
                obj = p.Object.Construct(context);//((NodeMatchPattern)tp.Object).Node.CopyNode(target);

                g.Assert(new Triple(subj, pred, obj));
            }

            if (this._manager.UpdateSupported)
            {
                this._manager.UpdateGraph(graphUri, g.Triples, Enumerable.Empty<Triple>());
            }
            else
            {
                this._manager.SaveGraph(g);
            }
        }

        /// <summary>
        /// Processes a LOAD command
        /// </summary>
        /// <param name="cmd">Load Command</param>
        public void ProcessLoadCommand(LoadCommand cmd)
        {
            Graph g = new Graph();
            if (!this._manager.UpdateSupported) this._manager.LoadGraph(g, cmd.TargetUri);
            UriLoader.Load(g, cmd.SourceUri);
            g.BaseUri = cmd.TargetUri;
            if (this._manager.UpdateSupported)
            {
                this._manager.UpdateGraph(cmd.TargetUri, g.Triples, Enumerable.Empty<Triple>());
            }
            else
            {
                this._manager.SaveGraph(g);
            }

        }

        /// <summary>
        /// Processes an INSERT/DELETE command
        /// </summary>
        /// <param name="cmd">Insert/Delete Command</param>
        public void ProcessModifyCommand(ModifyCommand cmd)
        {
            if (this._manager is IQueryableGenericIOManager)
            {
                //First build and make the query to get a Result Set
                String queryText = "SELECT * WHERE " + cmd.WherePattern.ToString();
                SparqlQueryParser parser = new SparqlQueryParser();
                SparqlQuery query = parser.ParseFromString(queryText);
                foreach (Uri u in cmd.UsingUris)
                {
                    query.AddDefaultGraph(u);
                }

                Object results = ((IQueryableGenericIOManager)this._manager).Query(query.ToString());
                if (results is SparqlResultSet)
                {
                    //Now need to transform the Result Set back to a Multiset
                    Multiset mset = new Multiset((SparqlResultSet)results);

                    //Generate the Triples for each Solution
                    List<Triple> deletedTriples = new List<Triple>();
                    Dictionary<String, List<Triple>> deletedGraphTriples = new Dictionary<string, List<Triple>>();
                    foreach (Set s in mset.Sets)
                    {
                        List<Triple> tempDeletedTriples = new List<Triple>();
                        try
                        {
                            ConstructContext context = new ConstructContext(null, s, true);
                            foreach (ITriplePattern p in cmd.DeletePattern.TriplePatterns)
                            {
                                tempDeletedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                            }
                            deletedTriples.AddRange(tempDeletedTriples);
                        }
                        catch (RdfQueryException)
                        {
                            //If we throw an error this means we couldn't construct for this solution so the
                            //solution is discarded
                            continue;
                        }

                        //Triples from GRAPH clauses
                        foreach (GraphPattern gp in cmd.DeletePattern.ChildGraphPatterns)
                        {
                            tempDeletedTriples.Clear();
                            try
                            {
                                String graphUri;
                                switch (gp.GraphSpecifier.TokenType)
                                {
                                    case Token.URI:
                                        graphUri = gp.GraphSpecifier.Value;
                                        break;
                                    case Token.VARIABLE:
                                        if (s.ContainsVariable(gp.GraphSpecifier.Value))
                                        {
                                            INode temp = s[gp.GraphSpecifier.Value.Substring(1)];
                                            if (temp.NodeType == NodeType.Uri)
                                            {
                                                graphUri = temp.ToSafeString();
                                            }
                                            else
                                            {
                                                //If the Variable is not bound to a URI then skip
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            //If the Variable is not bound for this solution then skip
                                            continue;
                                        }
                                        break;
                                    default:
                                        //Any other Graph Specifier we have to ignore this solution
                                        continue;
                                }
                                if (!deletedGraphTriples.ContainsKey(graphUri)) deletedGraphTriples.Add(graphUri, new List<Triple>());
                                ConstructContext context = new ConstructContext(null, s, true);
                                foreach (ITriplePattern p in gp.TriplePatterns)
                                {
                                    tempDeletedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                                }
                                deletedGraphTriples[graphUri].AddRange(tempDeletedTriples);
                            }
                            catch (RdfQueryException)
                            {
                                //If we throw an error this means we couldn't construct for this solution so the
                                //solution is discarded
                                continue;
                            }
                        }
                    }

                    //Generate the Triples for each Solution
                    List<Triple> insertedTriples = new List<Triple>();
                    Dictionary<String, List<Triple>> insertedGraphTriples = new Dictionary<string, List<Triple>>();
                    foreach (Set s in mset.Sets)
                    {
                        List<Triple> tempInsertedTriples = new List<Triple>();
                        try
                        {
                            ConstructContext context = new ConstructContext(null, s, true);
                            foreach (ITriplePattern p in cmd.InsertPattern.TriplePatterns)
                            {
                                tempInsertedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                            }
                            insertedTriples.AddRange(tempInsertedTriples);
                        }
                        catch (RdfQueryException)
                        {
                            //If we throw an error this means we couldn't construct for this solution so the
                            //solution is discarded
                            continue;
                        }

                        //Triples from GRAPH clauses
                        foreach (GraphPattern gp in cmd.InsertPattern.ChildGraphPatterns)
                        {
                            tempInsertedTriples.Clear();
                            try
                            {
                                String graphUri;
                                switch (gp.GraphSpecifier.TokenType)
                                {
                                    case Token.URI:
                                        graphUri = gp.GraphSpecifier.Value;
                                        break;
                                    case Token.VARIABLE:
                                        if (s.ContainsVariable(gp.GraphSpecifier.Value))
                                        {
                                            INode temp = s[gp.GraphSpecifier.Value.Substring(1)];
                                            if (temp.NodeType == NodeType.Uri)
                                            {
                                                graphUri = temp.ToSafeString();
                                            }
                                            else
                                            {
                                                //If the Variable is not bound to a URI then skip
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            //If the Variable is not bound for this solution then skip
                                            continue;
                                        }
                                        break;
                                    default:
                                        //Any other Graph Specifier we have to ignore this solution
                                        continue;
                                }
                                if (!insertedGraphTriples.ContainsKey(graphUri)) insertedGraphTriples.Add(graphUri, new List<Triple>());
                                ConstructContext context = new ConstructContext(null, s, true);
                                foreach (ITriplePattern p in gp.TriplePatterns)
                                {
                                    tempInsertedTriples.Add(((IConstructTriplePattern)p).Construct(context));
                                }
                                insertedGraphTriples[graphUri].AddRange(tempInsertedTriples);
                            }
                            catch (RdfQueryException)
                            {
                                //If we throw an error this means we couldn't construct for this solution so the
                                //solution is discarded
                                continue;
                            }
                        }
                    }

                    //Now decide how to apply the update
                    if (this._manager.UpdateSupported)
                    {
                        this._manager.UpdateGraph(cmd.GraphUri, insertedTriples, deletedTriples);
                        //We do these two operations sequentially even if in some cases they could be combined to ensure that the underlying
                        //Manager doesn't do any optimisations which would have the result of our updates not being properly applied
                        //e.g. ignoring Triples which are both asserted and retracted in one update
                        foreach (KeyValuePair<String, List<Triple>> graphDeletion in deletedGraphTriples)
                        {
                            this._manager.UpdateGraph(graphDeletion.Key, Enumerable.Empty<Triple>(), graphDeletion.Value);
                        }
                        foreach (KeyValuePair<String, List<Triple>> graphInsertion in insertedGraphTriples)
                        {
                            this._manager.UpdateGraph(graphInsertion.Key, graphInsertion.Value, Enumerable.Empty<Triple>());
                        }
                    }
                    else
                    {
                        Graph g = new Graph();
                        this._manager.LoadGraph(g, cmd.GraphUri);
                        g.Retract(deletedTriples);
                        this._manager.SaveGraph(g);

                        foreach (String graphUri in deletedGraphTriples.Keys.Concat(insertedGraphTriples.Keys).Distinct())
                        {
                            g = new Graph();
                            this._manager.LoadGraph(g, graphUri);
                            if (deletedGraphTriples.ContainsKey(graphUri)) g.Retract(deletedGraphTriples[graphUri]);
                            if (insertedGraphTriples.ContainsKey(graphUri)) g.Assert(insertedGraphTriples[graphUri]);
                            this._manager.SaveGraph(g);
                        }
                    }
                }
                else
                {
                    throw new SparqlUpdateException("Cannot evaluate an INSERT/DELETE Command as the underlying Store failed to answer the query for the WHERE portion of the command as expected");
                }
            }
            else
            {
                throw new NotSupportedException("INSERT/DELETE commands are not supported by this Update Processor as the manager for the underlying Store does not provide Query capabilities which are necessary to process this command");
            }
        }
    }
}

#endif