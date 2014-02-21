using System.Collections.Generic;
/*******************************************************************************
 * Copyright (c) 2009 TopQuadrant, Inc.
 * All rights reserved. 
 *******************************************************************************/
using System.Linq;
using VDS.RDF.Query.Spin.LibraryOntology;
using VDS.RDF.Query.Spin.SparqlUtil;

namespace VDS.RDF.Query.Spin.Model
{

    public class ModifyImpl : UpdateImpl, IModify
    {

        public ModifyImpl(INode node, SpinProcessor graph)
            : base(node, graph)
        {
        }


        public IEnumerable<INode> getUsing()
        {
            return listProperties(SP.PropertyUsing).Select(t => t.Object);
        }


        public IEnumerable<INode> getUsingNamed()
        {
            return listProperties(SP.PropertyUsingNamed).Select(t => t.Object);
        }


        public override void printSPINRDF(ISparqlPrinter p)
        {
            printComment(p);
            printPrefixes(p);

            IResource iri = getResource(SP.PropertyGraphIRI);

            IResource with = getResource(SP.PropertyWith);
            if (with != null)
            {
                p.printIndentation(p.getIndentation());
                p.printKeyword("WITH");
                p.print(" ");
                p.printURIResource(with);
                p.println();
            }

            // TODO add a INSERT/CONSTRUCT pattern before the delete is effective
            if (printTemplates(p, SP.PropertyDeletePattern, "DELETE", hasProperty(SP.PropertyDeletePattern), iri))
            {
                p.print("\n");
            }
            if (printTemplates(p, SP.PropertyInsertPattern, "INSERT", hasProperty(SP.PropertyInsertPattern), iri))
            {
                p.print("\n");
            }

            IEnumerable<INode> usings = getUsing();
            if (usings.Count() == 0)
            {
                usings = p.Dataset.DefaultGraphs;
            }
            foreach (IResource _using in usings)
            {
                p.printKeyword("USING");
                p.print(" ");
                p.printURIResource(_using);
                p.println();
            }

            usings = getUsingNamed();
            if (usings.Count() == 0)
            {
                usings = p.Dataset.ActiveGraphs;
            }
            foreach (IResource usingNamed in usings)
            {
                p.printKeyword("USING");
                p.print(" ");
                p.printKeyword("NAMED");
                p.print(" ");
                p.printURIResource(usingNamed);
                p.println();
            }
            p.CurrentSparqlContext = SparqlContext.QueryContext;
            printWhere(p);
        }

        override public void PrintEnhancedSPARQL(ISparqlPrinter p)
        {
            p.PrintEnhancedSPARQL(this);
        }

    }
}