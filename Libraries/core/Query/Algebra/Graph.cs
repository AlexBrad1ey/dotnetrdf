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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Represents a GRAPH clause
    /// </summary>
    public class Graph : ISparqlAlgebra
    {
        private ISparqlAlgebra _pattern;
        private IToken _graphSpecifier;

        /// <summary>
        /// Creates a new Graph clause
        /// </summary>
        /// <param name="pattern">Pattern</param>
        /// <param name="graphSpecifier">Graph Specifier</param>
        public Graph(ISparqlAlgebra pattern, IToken graphSpecifier)
        {
            this._pattern = pattern;
            this._graphSpecifier = graphSpecifier;
        }

        /// <summary>
        /// Evaluates the Graph Clause by setting up the dataset, applying the pattern and then generating additional bindings if necessary
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            BaseMultiset result;

            if (this._pattern is Bgp && ((Bgp)this._pattern).IsEmpty)
            {
                //Optimise the case where we have GRAPH ?g {} by not setting the Graph and just returning
                //a Null Multiset
                //This is a nice optimisation since otherwise we might merge all the Graphs in the dataset
                //together for no reason which can be computationally very costly
                result = new NullMultiset();
            }
            else
            {
                bool datasetOk = false;
                try
                {
                    //Modify the Active Graph as appropriate
                    if (this._graphSpecifier.TokenType != Token.VARIABLE)
                    {
                        switch (this._graphSpecifier.TokenType)
                        {
                            case Token.URI:
                            case Token.QNAME:
                                context.Data.SetActiveGraph(new Uri(Tools.ResolveUriOrQName(this._graphSpecifier, context.Query.NamespaceMap, context.Query.BaseUri)));
                                break;
                            default:
                                throw new RdfQueryException("Cannot use a '" + this._graphSpecifier.GetType().ToString() + "' Token to specify the Graph for a GRAPH clause");
                        }
                    }
                    else
                    {
                        String gvar = this._graphSpecifier.Value.Substring(1);
                        if (context.InputMultiset.ContainsVariable(gvar))
                        {
                            //If there are already values bound to the Graph variable then we limit the Query to those Graphs
                            List<Uri> graphUris = new List<Uri>();
                            foreach (Set s in context.InputMultiset.Sets)
                            {
                                INode temp = s[gvar];
                                if (temp != null)
                                {
                                    if (temp.NodeType == NodeType.Uri)
                                    {
                                        graphUris.Add(((UriNode)temp).Uri);
                                    }
                                }
                            }

                            //Set Active Graph
                            context.Data.SetActiveGraph(graphUris);
                        }
                        else
                        {
                            //Nothing yet bound to the Graph Variable so the Query is over all the named Graphs
                            if (context.Query.NamedGraphs.Any())
                            {
                                //Query specifies one/more named Graphs
                                context.Data.SetActiveGraph(context.Query.NamedGraphs.ToList());
                            }
                            else
                            {
                                //Query is over entire dataset/default Graph since no named Graphs are explicitly specified
                                context.Data.SetActiveGraph((Uri)null);
                                //context.Data.SetActiveGraph(new VDS.RDF.Graph());
                            }
                        }
                    }
                    datasetOk = true;

                    //Evaluate the inner pattern
                    BaseMultiset initialInput = context.InputMultiset;
                    result = this._pattern.Evaluate(context);

                    if (result is NullMultiset || result is IdentityMultiset)
                    {
                        //Don't do anything
                    }
                    else
                    {
                        //For Graph Variable Patterns where the Variable wasn't already bound add bindings
                        String gvar = this._graphSpecifier.Value.Substring(1);
                        if (!initialInput.ContainsVariable(gvar))
                        {
                            result.AddVariable(gvar);
                            foreach (Set s in result.Sets)
                            {
                                INode temp = s.Values.FirstOrDefault(n => n != null && n.GraphUri != null);
                                if (temp == null)
                                {
                                    s.Add(gvar, null);
                                }
                                else
                                {
                                    s.Add(gvar, new UriNode(null, temp.GraphUri));
                                }
                            }
                        }
                    }

                    //context.OutputMultiset = context.InputMultiset.Join(result);
                    //context.OutputMultiset = initialInput.Join(result);
                    context.OutputMultiset = result;
                }
                finally
                {
                    if (datasetOk) context.Data.ResetActiveGraph();
                }
            }

            return context.OutputMultiset;
        }

        /// <summary>
        /// Gets the Variables used in the Algebra
        /// </summary>
        public IEnumerable<String> Variables
        {
            get
            {
                if (this._graphSpecifier.TokenType == Token.VARIABLE)
                {
                    String graphVar = ((VariableToken)this._graphSpecifier).Value.Substring(1);
                    return this._pattern.Variables.Concat(graphVar.AsEnumerable()).Distinct();
                }
                else
                {
                    return this._pattern.Variables.Distinct();
                }
            }
        }

        /// <summary>
        /// Gets the Graph Specifier
        /// </summary>
        public IToken GraphSpecifier
        {
            get
            {
                return this._graphSpecifier;
            }
        }

        /// <summary>
        /// Gets the Inner Algebra
        /// </summary>
        public ISparqlAlgebra InnerAlgebra
        {
            get
            {
                return this._pattern;
            }
        }

        /// <summary>
        /// Gets the String representation of the Algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Graph(" + this._graphSpecifier.Value + ", " + this._pattern.ToString() + ")";
        }

        /// <summary>
        /// Converts the Algebra back to a SPARQL Query
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            SparqlQuery q = new SparqlQuery();
            q.RootGraphPattern = this.ToGraphPattern();
            q.Optimise();
            return q;
        }

        public GraphPattern ToGraphPattern()
        {
            GraphPattern p = this._pattern.ToGraphPattern();
            if (!p.IsGraph)
            {
                p.IsGraph = true;
                p.GraphSpecifier = this._graphSpecifier;
            }
            return p;
        }
    }
}
