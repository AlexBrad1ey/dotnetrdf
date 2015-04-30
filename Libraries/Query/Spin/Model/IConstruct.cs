/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved. 
 *******************************************************************************/
using System.Collections.Generic;
namespace VDS.RDF.Query.Spin.Model
{
    /**
     * A CONSTRUCT Query.
     * 
     * @author Holger Knublauch
     */
    public interface IConstruct : ISolutionModifierQuery
    {

        /**
         * Gets the list of TripleTemplates in the head of the query.
         * @return the templates
         */
        List<ITripleTemplate> getTemplates();
    }
}