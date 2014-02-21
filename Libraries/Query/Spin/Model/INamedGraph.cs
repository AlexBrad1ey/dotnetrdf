/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved. 
 *******************************************************************************/
using VDS.RDF;
namespace VDS.RDF.Query.Spin.Model
{

    /**
     * A named graph element (GRAPH keyword in SPARQL).
     * 
     * @author Holger Knublauch
     */
    public interface INamedGraph : IElementGroup
    {

        /**
         * Gets the URI INode or Variable that holds the name of this
         * named graph.  If it's a Variable, then this method will typecast
         * it into an instance of Variable.
         * @return a INode or Variable
         */
        IResource getNameNode();
    }
}